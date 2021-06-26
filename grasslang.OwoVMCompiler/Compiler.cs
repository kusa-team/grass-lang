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
        uint TopRegister = 0; // r0-31为变量寄存器（超出则压入栈中），r32-47计算临时寄存器，r48-61为参数寄存器（超出压栈），r62为参数数量，r63为返回值
        uint TempRegister = 32;
        public bool enableVMCall = false;

        private static byte[] instMovToRegister(byte reg,UInt64 value)
        {
            var result = new byte[] { 0x0D, reg, 0, 0b01010110 }.ToList();
            result.AddRange(BitConverter.GetBytes(value));
            return result.ToArray();
        }
        private static byte[] instMovToRegister(byte reg, byte value)
        {
            return new byte[] { 0x0D, reg, value, 0b01010101 };
        }
        private byte[] compileNode(Node node)
        {
            List<byte> result = new List<byte>();
            if (node is LetStatement letStatement)
            {
                var definition = letStatement.Definition;
                variables.Add(new VariableInfo(definition.Name.Literal, definition.ObjType.Literal, TopRegister));
                if (definition.Value is NumberLiteral numberLiteral)
                {
                    byte type = (byte)(numberLiteral.Value.Contains(".") ? 0b01011010 : 0b01010110);
                    result.AddRange(new byte[] { 0x0D, (byte)TopRegister, 0x00, type });
                    var value = BitConverter.GetBytes(long.Parse(numberLiteral.Value));
                    result.AddRange(value);
                } else if (definition.Value is StringLiteral)
                {
                    result.AddRange(compileNode(definition.Value));
                    result.AddRange(new byte[] { 0x0D, (byte)TopRegister, (byte)TempRegister, 00}); // mov堆上对象不需要type
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
            }
            else if (node is AssignExpression assignExpression)
            {
                if (assignExpression.Left is IdentifierExpression)
                {
                    var varname = assignExpression.Left.Literal;
                    var dsts = (from var in variables where var.name == varname select var);
                    if (!dsts.Any())
                    {
                        throw new Exception("Undefined variable: " + varname);
                    }
                    // 思路：先计算右值，最后将计算结果的临时寄存器mov回去
                    result.AddRange(compileNode(assignExpression.Right));
                    result.AddRange(new byte[] { 0x0D, (byte)dsts.First().register, (byte)TempRegister, 0b01010101 });
                }
            }
            else if (node is InfixExpression infixExpression)
            {
                // 思路：将第一个操作数mov到第一个临时寄存器，第二个操作数mov到第二个临时寄存器，结果为第一个临时寄存器

                // mov t1, op1
                uint lastTempRegister = TempRegister;
                Action<Expression> movIntoTempRegister = (expression) =>
                {
                    result.Add((byte)0x0D);
                    result.Add((byte)TempRegister);
                    if (expression is IdentifierExpression identifierExpressionValue)
                    {
                        // op1 is variable
                        var dsts = (from var in variables where var.name == identifierExpressionValue.Literal select var);
                        if (!dsts.Any())
                        {
                            throw new Exception("Undefined variable: " + identifierExpressionValue.Literal);
                        }
                        result.Add((byte)dsts.First().register);
                        result.Add((byte)0b01010101);
                    }
                    else if (expression is NumberLiteral numberLiteralValue)
                    {
                        // op1 is immediate
                        result.Add((byte)0x00);
                        result.Add((byte)0b01010110);
                        result.AddRange(BitConverter.GetBytes(long.Parse(numberLiteralValue.Value)));
                    }
                    TempRegister++;
                };
                movIntoTempRegister(infixExpression.Left);
                movIntoTempRegister(infixExpression.Right);
                if (infixExpression.Operator.Literal == "+")
                {
                    result.Add((byte)0x01);
                }
                else if (infixExpression.Operator.Literal == "-")
                {
                    result.Add((byte)0x02);
                }
                else if (infixExpression.Operator.Literal == "*")
                {
                    result.Add((byte)0x03);
                }
                else if (infixExpression.Operator.Literal == "/")
                {
                    result.Add((byte)0x04);
                }
                result.AddRange(new byte[] { (byte)lastTempRegister, (byte)(lastTempRegister + 1), 0b01010101 });
                TempRegister = lastTempRegister; // reset
            }
            else if (node is FunctionLiteral functionLiteral)
            {
                foreach (var subnode in functionLiteral.Body.Body)
                {
                    result.AddRange(compileNode(subnode));
                }
            } else if (node is CallExpression callExpression)
            {
                // call指令未完成，先做vmcall吧。
                if(callExpression.Function.Literal == "owovm$vmcall")
                {
                    if(enableVMCall)
                    {
                        // 启用vmcall了
                        if(callExpression.Parameters[0] is NumberLiteral vmcallOpcode)
                        {
                            result.AddRange(instMovToRegister(48, UInt64.Parse(vmcallOpcode.Value)));
                        }else
                        {
                            throw new Exception("The opcode of VMCall is must number");
                        }
                        for (int i = 1; i < callExpression.Parameters.Count; i++)
                        {
                            if(callExpression.Parameters[i] is IdentifierExpression vmcallArg)
                            {
                                var varname = vmcallArg.Literal;
                                var resultVars = (from var in variables where var.name == varname select var);
                                if(!resultVars.Any()) { throw new Exception("Undefined variable: " + varname); }
                                var inputVar = resultVars.First();
                                result.AddRange(instMovToRegister((byte)(48 + i), (byte)inputVar.register));
                            } else if(callExpression.Parameters[i] is NumberLiteral vmcallArgNum)
                            {
                                result.AddRange(instMovToRegister((byte)(48 + i), UInt64.Parse(vmcallArgNum.Value)));
                            } else if(callExpression.Parameters[i] is StringLiteral)
                            {
                                result.AddRange(compileNode(callExpression.Parameters[i]));
                                result.AddRange(new byte[] { 0x0D, (byte)(48 + i), (byte)TempRegister, 00 });
                            }
                        }
                        result.AddRange(new byte[] { 0x0E, 0, 0, 0 });
                    } else
                    {
                        throw new Exception("VMCall is not allowed");
                    }
                }
            } else if (node is StringLiteral stringLiteral)
            {
                string parsedString = stringLiteral.Value;
                parsedString = parsedString.Replace("\\n", "\n");
                result.AddRange(new byte[] { 0x12, (byte)TempRegister, 0, 0 });
                result.AddRange(BitConverter.GetBytes(Convert.ToUInt64(parsedString.Length)));
                result.AddRange(Encoding.Default.GetBytes(parsedString));
            }
            return result.ToArray();
        }
        Stack<IndexItem> itemStack = new Stack<IndexItem>();
        // 仅适用于compile结构代码
        private byte[] compileNodeArray(Node[] nodeArray)
        {
            List<byte> result = new List<byte>();
            foreach (var node in nodeArray)
            {
                bool compiled = false;
                if (node is ExpressionStatement expressionStatement)
                {
                    if (expressionStatement.Expression is FunctionLiteral functionLiteral)
                    {
                        // TODO: 暂时不支持参数，什么时候支持等我什么时候整完call指令
                        string prefix = itemStack.Count != 0 ? itemStack.Peek().Name + "." : "";
                        indexItems.Add(new IndexItem(prefix + functionLiteral.FunctionName.Literal + "()", 0x04, Convert.ToUInt64(result.Count)));
                        // 0x04 means function/method
                        result.AddRange(compileNode(node));
                        compiled = true;
                    }
                    else if (expressionStatement.Expression is ClassLiteral classLiteral)
                    {
                        var item = new IndexItem(classLiteral.TypeName.Literal, 0x02, 0x00);// 长度0x00空类（等后面加入class的field再写）
                        itemStack.Push(item);
                        indexItems.Add(item);
                        result.AddRange(compileNodeArray(classLiteral.Body.Body.ToArray()));
                        itemStack.Pop();
                    }
                }
                if (!compiled)
                {
                    // TODO: throw an exception here
                }
            }
            return result.ToArray();
        }
        /*
            思路： 结构上仅允许class/struct/function
            突然想到还没有class呢，先鸽了(划掉) 草 原来已经有class了，鸽了几个月忘记了
         */
        private byte[] compileAst(Ast ast)
        {
            var result = compileNodeArray(ast.Root.ToArray());
            return result;
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
            byte[] codeResult = compileAst(ast);
            byte[] indexResult = compileIndexItems();
            result.AddRange(BitConverter.GetBytes(Convert.ToUInt64(indexResult.Length)));
            result.AddRange(indexResult);
            result.AddRange(codeResult);
            return result.ToArray();
        }
    }
}
