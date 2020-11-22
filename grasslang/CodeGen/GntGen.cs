/*using System;
using System.Collections.Generic;

namespace grasslang.CodeGens
{
    public class GntGen : CodeGen
    {
        private Ast ast;
        private string code = "";
        private int layer = 0;
        // Gct = Grasslang Node Tree
        public GntGen(Ast ast)
        {
            this.ast = ast;
        }

        //
        private void NextLayer()
        {
            layer++;
        }

        private void LastLayer()
        {
            layer--;
        }

        private void WriteCode(string newcode)
        {
            code += RepeatIndentation(layer) + newcode + ";\n";
        }
        //

        private string RepeatIndentation(int count)
        {
            string result = "";
            for (int i = 0; i < count; i++)
            {
                result += "\t";
            }
            return result;
        }
        public string GetCode()
        {
            return code;
        }
        private void BuildNode(Node node)
        {
            if (node is IdentifierExpression identifierExpression)
            {
                WriteCode("IdentifierExpression " + identifierExpression.Literal);
            }
            else if (node is FunctionStatement functionStatement)
            {
                WriteCode("FunctionStatement");
                NextLayer();
                BuildNode(functionStatement.ReturnType);
                BuildNode(functionStatement.FunctionName);
                WriteCode("List");
                NextLayer();
                bool frist_argument = true;
                functionStatement.ArgumentList.ForEach((argument) =>
                {
                    BuildNode(argument);
                    if (frist_argument)
                    {
                        frist_argument = false;
                    }
                });
                LastLayer();
                WriteCode("End");
                BuildNode(functionStatement.body);
                LastLayer();
            }
            else if (node is Block block)
            {
                WriteCode("Block");
                NextLayer();
                block.body.ForEach((item) =>
                {
                    BuildNode(item);
                });
                LastLayer();
                WriteCode("End");
            }
            else if (node is DefinitionExpression definitionExpression)
            {
                WriteCode("DefinitionExpression");
                NextLayer();
                BuildNode(definitionExpression.ObjType);
                BuildNode(definitionExpression.Name);
                if (definitionExpression.Value != null)
                {
                    BuildNode(definitionExpression.Value);
                }
                LastLayer();
            }
            else if (node is LetStatement letStatement)
            {
                WriteCode("LetStatement");
                NextLayer();
                BuildNode(letStatement.Definition);
                LastLayer();
            }
            else if (node is CallExpression callExpression)
            {
                WriteCode("CallExpression");
                NextLayer();
                BuildNode(callExpression.FunctionName);
                WriteCode("List");
                NextLayer();
                bool frist_argument = true;
                new List<Expression>(callExpression.ArgsList).ForEach((argument) =>
                {
                    BuildNode(argument);
                    if (frist_argument)
                    {
                        frist_argument = false;
                    }
                });
                LastLayer();
                WriteCode("End");
                LastLayer();
            }
            else if (node is ExpressionStatement expressionStatement)
            {
                BuildNode(expressionStatement.Expression);
            }
            else if (node is ReturnStatement returnStatement)
            {
                WriteCode("ReturnStatement");
                NextLayer();
                BuildNode(returnStatement.Value);
                LastLayer();
            }
            else if (node is InfixExpression infixExpression)
            {
                WriteCode("InfixExpression");
                NextLayer();
                BuildNode(infixExpression.LeftExpression);
                WriteCode("Operator " + infixExpression.Operator.Literal);
                BuildNode(infixExpression.RightExpression);
                LastLayer();
            }
            else if (node is StringExpression stringExpression)
            {
                WriteCode("StringExpression \"" + stringExpression.Value + "\"");
            }
            else if (node is ChildrenExpression childrenExpression)
            {
                WriteCode("ChildrenExpression " + childrenExpression.Literal);
            }
        }
        public void Build()
        {
            ast.Root.ForEach((node) =>
            {
                BuildNode(node);
            });
        }
    }
}
*/