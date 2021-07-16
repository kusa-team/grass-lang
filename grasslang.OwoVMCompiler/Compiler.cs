using grasslang.CodeModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace grasslang.OwoVMCompiler
{
    record VariableInfo(string name, string type, uint register);
    public class Compiler
    {
        List<IndexItem> indexItems = new List<IndexItem>();
        List<VariableInfo> variables = new List<VariableInfo>();
        
        public bool enableVMCall = false;
        public CodeBuffer codeBuffer = new CodeBuffer();
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
            var resultVars = (from var in variables where var.name == name select var);
            if (!resultVars.Any())
            {
                throw new Exception("Undefined identifier: " + name);
            }
            var inputVar = resultVars.First();
            buffer.EmitMov(
                new Register(index),
                new Register((byte)inputVar.register, GetImmediateType(inputVar.type))
                );
        }
        private CodeBuffer compileNode(Node node, uint startReg = 0, uint startTempReg = 32)
        {
            uint TopRegister = startReg; // r0-31为变量寄存器（超出则压入栈中），r32-47计算临时寄存器，r48-61为参数寄存器（超出压栈），r62为参数数量，r63为返回值
            uint TempRegister = startTempReg;
            CodeBuffer result = new CodeBuffer();


            if (node is LetStatement letStatement)
            {
                var definition = letStatement.Definition;
                variables.Add(new VariableInfo(definition.Name.Literal, definition.ObjType.Literal, TopRegister));
                if (definition.Value is NumberLiteral numberLiteral)
                {
                    // rvalue is number
                    var rvalue = numberLiteral.Value;
                    var type = definition.ObjType.Literal;

                    result.EmitMov(
                        new Register((byte)TopRegister), 
                        new Immediate(GetImmediateType(type, rvalue), numberLiteral.Value)
                    );
                } else if (definition.Value is IdentifierExpression identifierExpression)
                {
                    moveVariable((byte)TopRegister, identifierExpression.Literal, ref result);
                } else if (definition.Value is StringLiteral stringLiteral)
                {
                    result.Emit(compileNode(stringLiteral, TopRegister, TempRegister));
                    result.EmitMov(new Register((byte)TopRegister), new Register((byte)TempRegister, ImmediateType.Unknown));
                }
                else
                {
                    if (definition.Value != null)
                        throw new NotImplementedException("Unsupport node!");
                }
                TopRegister++;
            }
            else if (node is ExpressionStatement expressionStatement)
            {
                return compileNode(expressionStatement.Expression);
            } else if (node is CallExpression callExpression)
            {
                bool isVMCall = false;
                if(callExpression.Function.Literal == "owovm$vmcall")
                {
                    if(enableVMCall)
                    {
                        isVMCall = true;
                    } else
                    {
                        throw new Exception("VMCall is not allowed");
                    }
                }
                for (int i = 0; i < callExpression.Parameters.Count; i++)
                {
                    if (callExpression.Parameters[i] is IdentifierExpression vmcallArg)
                    {
                        moveVariable((byte)(48 + i), vmcallArg.Literal, ref result);
                    }
                    else if (callExpression.Parameters[i] is NumberLiteral vmcallArgNum)
                    {
                        var value = vmcallArgNum.Value;
                        result.EmitMov(
                            new Register((byte)(48 + i)),
                            new Immediate(GetImmediateType("", value), value)
                            );
                    }
                    else if (callExpression.Parameters[i] is StringLiteral)
                    {
                        result.Emit(compileNode(callExpression.Parameters[i], TopRegister, TempRegister));
                        result.EmitMov(
                            new Register((byte)(48 + i)),
                            new Register((byte)(TempRegister)));
                    }
                }
                if(isVMCall)
                {
                    result.EmitVMCall();
                }
            } else if (node is StringLiteral stringLiteral)
            {
                string parsedString = stringLiteral.Value;
                parsedString = parsedString.Replace("\\n", "\n");
                result.EmitString(
                    new Register((byte)TempRegister),
                    parsedString
                    );
            } else if (node is FunctionLiteral functionLiteral)
            {
                var func = compileNode(functionLiteral.Body);
                func.EmitRet();
                result.Emit(func);
            } else if (node is BlockStatement blockStatement)
            {
                foreach(var subnode in blockStatement.Body)
                {
                    result.Emit(compileNode(subnode));
                }
            }
            return result;
        }
        Stack<IndexItem> itemStack = new Stack<IndexItem>();
        // 仅适用于compile结构代码
        private CodeBuffer compileNodeArray(Node[] nodeArray)
        {
            CodeBuffer result = new CodeBuffer();
            foreach (var node in nodeArray)
            {
                bool compiled = false;
                if (node is ExpressionStatement expressionStatement)
                {
                    if (expressionStatement.Expression is FunctionLiteral functionLiteral)
                    {
                        // TODO: 暂时不支持参数，什么时候支持等我什么时候整完call指令
                        string prefix = itemStack.Count != 0 ? itemStack.Peek().Name + "." : "";
                        indexItems.Add(new IndexItem(prefix + functionLiteral.FunctionName.Literal + "()", 0x04, Convert.ToUInt64(result.Length)));
                        // 0x04 means function/method
                        result.Emit(compileNode(node));
                        compiled = true;
                    }
                    else if (expressionStatement.Expression is ClassLiteral classLiteral)
                    {
                        var item = new IndexItem(classLiteral.TypeName.Literal, 0x02, 0x00);// 长度0x00空类（等后面加入class的field再写）
                        itemStack.Push(item);
                        indexItems.Add(item);
                        result.Emit(compileNodeArray(classLiteral.Body.Body.ToArray()));
                        itemStack.Pop();
                    }
                }
                if (!compiled)
                {
                    // TODO: throw an exception here
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
        private byte[] compileIndexItems()
        {
            List<byte> result = new List<byte>();
            foreach (var item in indexItems)
            {
                result.AddRange(item.CompileToArray());
            }
            return result.ToArray();
        }
        public byte[] Compile(Ast ast)
        {
            List<byte> result = new List<byte>();
            byte[] codeResult = compileAst(ast).Build();
            byte[] indexResult = compileIndexItems();
            result.AddRange(BitConverter.GetBytes(Convert.ToUInt64(indexResult.Length)));
            result.AddRange(indexResult);
            result.AddRange(codeResult);
            return result.ToArray();
        }
    }
}
