using grasslang.CodeModel;
using System;
using System.Collections.Generic;
using System.Linq;
namespace grasslang.OwoVMCompiler
{
    record VariableInfo(string name, string type, uint register);
    public class Compiler
    {
        List<VariableInfo> variables = new List<VariableInfo>();
        uint TopRegister = 0; // r0-31为变量寄存器（超出则压入栈中），r32-47计算临时寄存器，r48-61为参数寄存器（超出压栈），r62为参数数量，r63为返回值
        uint TempRegister = 32;
        public byte[] compileNode(Node node)
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
            return result.ToArray();
        }
        public byte[] compileAst(Ast ast)
        {
            List<byte> result = new List<byte>();
            foreach (var node in ast.Root)
            {
                result.AddRange(compileNode(node));
            }
            return result.ToArray();
        }
    }
}
