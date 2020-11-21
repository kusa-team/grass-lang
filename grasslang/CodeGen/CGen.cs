/*using System;
using System.Collections.Generic;

namespace grasslang.CodeGens
{
    public class CGen : CodeGen
    {
        private Ast ast;
        public string code = "";
        public CGen(Ast ast)
        {
            this.ast = ast;
        }
        public string GetCode()
        {
            return code;
        }
        private string BuildNode(Node node)
        {
            string result = "";
            if (node is IdentifierExpression identifierExpression)
            {
                return identifierExpression.Literal;
            }
            else if (node is FunctionStatement functionStatement)
            {
                result += BuildNode(functionStatement.ReturnType) + " ";
                result += "glFunc_" + BuildNode(functionStatement.FunctionName) + "(";

                bool frist_argument = true;
                functionStatement.ArgumentList.ForEach((argument) =>
                {
                    result += (frist_argument ? "" : ", ") + BuildNode(argument);
                    if (frist_argument)
                    {
                        frist_argument = false;
                    }
                });
                result += ") { \n";
                result += BuildNode(functionStatement.body);
                result += "} \n \n";
            }
            else if (node is Block block)
            {
                block.body.ForEach((item) =>
                {
                    result += BuildNode(item) + ";\n";
                });
            }
            else if (node is DefinitionExpression definitionExpression)
            {
                result += BuildNode(definitionExpression.ObjType) + " ";
                result += BuildNode(definitionExpression.Name);
                if (definitionExpression.Value != null)
                {
                    result += " = " + BuildNode(definitionExpression.Value);
                }
            }
            else if (node is LetStatement letStatement)
            {
                result += BuildNode(letStatement.Definition);
            }
            else if (node is CallExpression callExpression)
            {
                string prefix = "glFunc_";
                if(callExpression.FunctionName is ChildrenExpression)
                {
                    prefix = "";
                }
                result += prefix + BuildNode(callExpression.FunctionName) + "(";
                bool frist_argument = true;
                new List<Expression>(callExpression.ArgsList).ForEach((argument) =>
                {
                    result += (frist_argument ? "" : ", ") + BuildNode(argument);
                    if (frist_argument)
                    {
                        frist_argument = false;
                    }
                });
                result += ")";
            }
            else if (node is ExpressionStatement expressionStatement)
            {
                result += BuildNode(expressionStatement.Expression);
            }
            else if (node is ReturnStatement ReturnStatement)
            {
                result += "return " + BuildNode(ReturnStatement.Value);
            }
            else if (node is InfixExpression infixExpression)
            {
                result += BuildNode(infixExpression.LeftExpression) + infixExpression.Operator.Literal + BuildNode(infixExpression.RightExpression);
            }
            else if(node is StringExpression stringExpression)
            {
                result += "string(" + "\"" + stringExpression.Value + "\")";
            }
            else if(node is InternalCode internalCode)
            {
                result += internalCode.Value + "\n";
            }
            else if(node is ChildrenExpression childrenExpression)
            {
                result += childrenExpression.Literal;
            }

            return result;
        }
        public void Build()
        {
            // headers and tool functions
            code += @"#include <iostream>
using namespace std;
void glFunc_print(int value) {
    cout << value << endl;
}
void glFunc_print(string value) {
    cout << value << endl;
}

// 
// grasslang body
//
";
            // define functions
            ast.Root.ForEach((node) =>
            {
                if (node is FunctionStatement functionStatement)
                {
                    string result = "";
                    if (functionStatement.ReturnType == null)
                    {
                        result += "void ";
                    }
                    else
                    {
                        result += BuildNode(functionStatement.ReturnType) + " ";
                    }
                    result += "glFunc_" + BuildNode(functionStatement.FunctionName) + "(";

                    bool frist_argument = true;
                    functionStatement.ArgumentList.ForEach((argument) =>
                    {
                        result += (frist_argument ? "" : ", ") + BuildNode(argument);
                        if (frist_argument)
                        {
                            frist_argument = false;
                        }
                    });
                    result += ");\n";
                    code += result + "\n";
                }
            });
            // body
            ast.Root.ForEach((node) =>
            {
                code += BuildNode(node);
            });
            // loader
            code += @"// grasslang loader
int main(int argc, char **argv) {
    glFunc_main();
}";
            
        }
    }
}
*/