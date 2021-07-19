using grasslang.CodeModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace grasslang.OwoVMCompiler
{
    class Node
    {
        public Node Parent = null;
        public virtual List<Node> Children { get; set; } = new List<Node>();
        public string Name;
        public virtual string FullName
        {
            get
            {
                string result = "";
                if (Parent is Node namedParent)
                {
                    result += string.IsNullOrEmpty(namedParent.Name) ? "" 
                        : (namedParent.FullName + ".");
                }
                result += Name;
                return result;
            }
        }
        public virtual Node Find(string identifier)
        {
            foreach (var child in Children)
            {
                if (child.Name == identifier) return child;
            }
            if (Parent != null)
            {
                return Parent.Find(identifier);
            }
            return null;
        }
        public List<IndexItem> ToIndexItem()
        {
            List<IndexItem> result = new List<IndexItem>();
            if(this is ClassScope selfClass)
            {
                result.Add(new IndexItem(FullName, 0x02, 0x00));
            } else if (this is FunctionScope selfFunc)
            {
                result.Add(new IndexItem(FullName, 0x04, selfFunc.Address));
            }
            foreach (var child in Children)
            {
                result.AddRange(child.ToIndexItem());
            }
            return result;
        }
    }
    class Scope : Node
    {
        public override Node Find(string identifier)
        {
            var node = (Node)this;
            while (true)
            {
                if (node is Scope)
                {
                    foreach (var child in node.Children)
                    {
                        if (child.Name == identifier) return child;
                    }
                }
                else if (node == null)
                {
                    return null;
                }
                node = node.Parent;
            }
        }
    }
    class ClassScope : Scope
    {
        public ClassScope(string name)
        {
            this.Name = name;
        }
    }
    interface RegisterScope
    {
        public uint VariableRegister { get; set; }
        public uint TempRegister { get; set; }
        public uint ParameterRegister { get; set; }
    } 
    class FunctionScope : Scope, RegisterScope
    {
        public string ArgumentString
        {
            get => "";
        }
        public override string FullName => base.FullName + "(" + ArgumentString + ")";
        public uint VariableRegister { get; set; } = 0;
        public uint TempRegister { get; set; } = 32;
        public uint ParameterRegister { get; set; } = 48;

        public UInt64 Address = 0;
        public FunctionScope(string name)
        {
            this.Name = name;
        }
    }
    class VariableNode : Node
    {
        public uint Register = 0;
        public bool InStack = false;
        public string Type = "";
        public VariableNode(string name, uint reg)
        {
            this.Name = name;
            this.Register = reg;
        }
    }
    
    public class Compiler
    {
        Stack<Node> scopes = new Stack<Node>();
        
        public bool enableVMCall = false;
        private static Dictionary<string, ImmediateType> immTypes = new Dictionary<string, ImmediateType>();
        private static ImmediateType GetImmediateType(string typeString, string valueString = null)
        {
            if(!immTypes.ContainsKey(typeString))
            {
                if(!string.IsNullOrEmpty(valueString))
                {
                    return valueString.Contains('.') ? ImmediateType.Float : ImmediateType.Int32;
                }
                return ImmediateType.Int32;
            }
            return immTypes[typeString];
        }
        private void moveVariable(byte index, string name, ref CodeBuffer buffer)
        {
            var variableRaw = scopes.Peek().Find(name);
            if(variableRaw == null)
            {
                throw new Exception("Undefined identifier: " + name);
            } else if (variableRaw is not VariableNode)
            {
                throw new Exception("Compiler Internal Error");
            }
            var variable = variableRaw as VariableNode;
            buffer.EmitMov(
                new Register(index),
                new Register((byte)(variable.Register)
                    , GetImmediateType(variable.Type))
                );
        }
        public int CodeSize = 0;
        private CodeBuffer compileNode(CodeModel.Node node)
        {
            CodeBuffer result = new();
            if (node is ExpressionStatement exprStmt)
            {
                result.Emit(compileNode(exprStmt.Expression));
            }
            else if (node is LetStatement letStmt)
            {
                // 在本作用域内声明变量
                var definition = letStmt.Definition;
                var curNode = scopes.Peek() as RegisterScope;
                var reg = curNode.VariableRegister;
                var variable = new VariableNode(definition.Name.Literal, reg);
                ((Scope)curNode).Children.Add(variable);
                curNode.VariableRegister++;
                // 解析语句 构造字节码
                if (definition.Value is NumberLiteral numLiteral)
                {
                    // 数字 直接Move
                    var type = definition.ObjType.Literal;
                    var value = numLiteral.Value;
                    result.EmitMov(
                        new Register((byte)reg),
                        new Immediate(
                            GetImmediateType(type, value),
                            value
                        ));
                }
                else if (node is IdentifierExpression identExpr)
                {
                    moveVariable((byte)reg, identExpr.Literal, ref result);
                }
                else
                {
                    // 先Move到临时寄存器，再Move回来
                    result.Emit(compileNode(definition.Value));
                    result.EmitMov(
                        new Register((byte)reg, ImmediateType.Unknown),
                        new Register((byte)curNode.TempRegister, ImmediateType.Unknown)
                        );
                }
            }
            else if (node is IdentifierExpression identExpr)
            {
                var curNode = scopes.Peek() as RegisterScope;
                moveVariable((byte)curNode.TempRegister, identExpr.Literal, ref result);
            }
            else if (node is StringLiteral strLiteral)
            {
                var curNode = scopes.Peek() as RegisterScope;
                var parsedStr = strLiteral.Value;
                parsedStr = parsedStr.Replace("\\n", "\n");
                result.EmitString(
                    new Register((byte)curNode.TempRegister),
                    parsedStr
                    );
            }
            else if (node is BlockStatement blockStmt)
            {
                foreach (var subNode in blockStmt.Body)
                {
                    result.Emit(compileNode(subNode));
                }
            }
            else if (node is IfExpression ifExpr)
            {
                var curNode = scopes.Peek() as RegisterScope;
                if (ifExpr.Condition is NumberLiteral numLiteral)
                {
                    if (ulong.Parse(numLiteral.Value) == 0)
                    {
                        // it is always 0
                        if (ifExpr.Alternative != null)
                        {
                            result.Emit(compileNode(ifExpr.Alternative));
                        }
                    }
                    else
                    {
                        // always is true
                        result.Emit(compileNode(ifExpr.Consequence));
                    }
                }
                else
                {
                    result.Emit(compileNode(ifExpr.Condition));
                    var conditionIsTrue = compileNode(ifExpr.Consequence);
                    result.EmitBrfalse(new Register((byte)curNode.TempRegister),
                        new Immediate(ImmediateType.Uint64, (CodeSize + 12).ToString())); // 12 = size of opcode + size of immediate
                    result.Emit(conditionIsTrue);
                    if (ifExpr.Alternative != null)
                    {
                        result.Emit(compileNode(ifExpr.Alternative));
                    }
                }
            }
            else if (node is CallExpression callExpr)
            {
                string name = callExpr.Function.Literal;
                bool vmcall = false;
                if(name == "owovm$vmcall")
                {
                    if(enableVMCall)
                    {
                        vmcall = true;
                    } else
                    {
                        throw new Exception("VMCall is not allowed");
                    }
                }
                var curNode = scopes.Peek() as RegisterScope;
                // load argument and push registers here
                byte regIndex = 48;
                foreach(var parameter in callExpr.Parameters)
                {
                    if(parameter is NumberLiteral numLiteral)
                    {
                        var value = numLiteral.Value;
                        result.EmitMov(
                            new Register(regIndex), 
                            new Immediate(GetImmediateType(value), value)
                        );    
                    } else
                    {
                        result.Emit(compileNode(parameter));
                        result.EmitMov(
                            new Register((byte)regIndex, ImmediateType.Unknown),
                            new Register((byte)curNode.TempRegister, ImmediateType.Unknown)
                            );
                    }
                    regIndex++;
                }
                if(vmcall)
                {
                    result.EmitVMCall();
                } else
                {
                    if(callExpr.Function is IdentifierExpression identNameExpr)
                    {
                        var target = (curNode as Node).Find(name);
                        if(target != null)
                        {
                            result.EmitCall(target.FullName);
                        } else
                        {
                            throw new Exception("Undefined identifier: " + name);
                        }
                    } else
                    {

                    }
                }
            }
            CodeSize += result.LengthExcluded;
            return result;
        }
        Stack<IndexItem> itemStack = new Stack<IndexItem>();
        // 仅适用于compile结构代码
        private CodeBuffer compileNodeArray(CodeModel.Node[] nodeArray)
        {
            CodeBuffer result = new CodeBuffer();
            foreach(var subNode in nodeArray)
            {
                if(subNode is ClassLiteral classLiteral)
                {
                    // class
                    var subScope = new ClassScope(classLiteral.TypeName.Literal)
                    {
                        Parent = scopes.Peek()
                    };
                    scopes.Peek().Children.Add(subScope);
                    scopes.Push(subScope);
                    result.Emit(compileNodeArray(classLiteral.Body.Body.ToArray()));
                    scopes.Pop();
                } else if (subNode is FunctionLiteral functionLiteral)
                {
                    // function
                    var subScope = new FunctionScope(functionLiteral.FunctionName.Literal)
                    {
                        Parent = scopes.Peek(),
                        Address = Convert.ToUInt64(CodeSize)
                    };
                    scopes.Peek().Children.Add(subScope);
                    scopes.Push(subScope);
                    var body = compileNode(functionLiteral.Body);
                    body.EmitRet();
                    result.Emit(body);
                    scopes.Pop();
                    CodeSize += 4; // ret
                } else if(subNode is ExpressionStatement exprStmt)
                {
                    result.Emit(compileNodeArray(new[] { exprStmt.Expression }));
                }
            }
            return result;
        }
        /*
            思路： 结构上仅允许class/struct/function
            突然想到还没有class呢，先鸽了(划掉) 草 原来已经有class了，鸽了几个月忘记了
         */
        private CodeBuffer compileAst(Ast ast)
        {
            return compileNodeArray(ast.Root.ToArray());
        }
        private byte[] compileIndexItems(Node root)
        {
            var items = root.ToIndexItem();
            var result = new List<byte>();
            foreach (var item in items)
            {
                result.AddRange(item.CompileToArray());
            }
            return result.ToArray();
        }
        public byte[] Compile(Ast ast)
        {
            var rootScope = new Scope();
            scopes.Push(rootScope);
            List<byte> result = new List<byte>();

            // compile code
            byte[] codeResult = compileAst(ast).Build();
            
            // compile index
            byte[] indexResult = compileIndexItems(rootScope);

            // concat
            result.AddRange(BitConverter.GetBytes(Convert.ToUInt64(indexResult.Length)));
            result.AddRange(indexResult);
            result.AddRange(codeResult);

            // return
            return result.ToArray();
        }
    }
}
