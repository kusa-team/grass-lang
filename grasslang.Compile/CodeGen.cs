using System;
using System.Collections.Generic;
using grasslang.CodeModel;
namespace grasslang.Compile
{
    /// <summary>
    /// translate grasslang code to c++ code
    /// </summary>
    public class CodeGen
    {
        public static string Template = @"
#include <iostream>
using namespace std;
#define gx_string string
#define gx_int int
{{{ type_defines }}}
{{{ object_defines }}}

{{{ body }}}";
        public static string Prefix = "gx_";

        private List<string> typeDefines = new List<string>();
        private List<string> objectDefines = new List<string>();
        private int blockLevel = 0;
        private string buildInternalCode(InternalCode internalCode)
        {
            return internalCode.Value;
        }
        private string buildFunctionLiteral(FunctionLiteral functionLiteral)
        {
            var result = "";
            // build return type
            var returntype = Build(functionLiteral.ReturnType);
            result += returntype + " ";
            // build function name
            var functionname = Prefix + Build(functionLiteral.FunctionName);
            result += functionname + "(";
            // build function arguments
            bool fristArgument = true;
            var parameters = "";
            functionLiteral.Parameters.ForEach(node =>
            {
                parameters += (fristArgument ? "" : ",") + Prefix + Build(node);
                if(fristArgument)
                {
                    fristArgument = false;
                }
            });
            result += parameters;
            result += ") { \n";
            // build function body
            result += Build(functionLiteral.Body);
            result += "}";
            // add to defines list
            if (blockLevel == 0)
            {
                objectDefines.Add(returntype + " " + functionname + "(" + parameters + ");");
            }
            return result;
        }
        private string buildDefinitionExpression(DefinitionExpression definitionExpression)
        {
            var result = "";
            // build type
            result += Build(definitionExpression.ObjType) + " ";
            // build name
            result += Build(definitionExpression.Name);
            // build value
            if(definitionExpression.Value != null)
            {
                result += " = " + Build(definitionExpression.Value);
            }
            return result;
        }
        private string buildClassLiteral(ClassLiteral classLiteral)
        {
            var result = "";
            result += "class ";
            // build class name
            var classname = Prefix + Build(classLiteral.TypeName);
            result += classname + " ";
            // build class extends
            result += ": public " + Prefix + "Object";
            if(classLiteral.Extends.Count != 0)
            {
                var extends = "";
                classLiteral.Extends.ForEach(node =>
                {
                    extends += ", " + Prefix + Build(node);
                });
                result += extends;
            }
            result += "{ \n";
            // build class body
            result += Build(classLiteral.Body);
            result += "};";
            // add to defines
            if(blockLevel == 0)
            {
                typeDefines.Add("class " + classname + ";");
            }
            return result;
        }
        private string buildBlockExpression(BlockStatement blockStatement)
        {
            var result = "";
            blockLevel++;
            blockStatement.Body.ForEach(node =>
            {
                result += Build(node) + "\n";
            });
            blockLevel--;
            return result;
        }
        private string buildIdentifierExpression(IdentifierExpression identifierExpression)
        {
            return identifierExpression.Literal;
        }
        private string buildReturnStatement(ReturnStatement returnStatement)
        {
            return "return " + Build(returnStatement.Value);
        }
        public string Build(Node node)
        {
            if(node is ExpressionStatement expressionStatement)
            {
                return Build(expressionStatement.Expression);
            } else if(node is InternalCode internalCode)
            {
                return buildInternalCode(internalCode);
            } else if (node is FunctionLiteral functionLiteral)
            {
                return buildFunctionLiteral(functionLiteral);
            } else if (node is IdentifierExpression identifierExpression)
            {
                return buildIdentifierExpression(identifierExpression);
            } else if (node is DefinitionExpression definitionExpression)
            {
                return buildDefinitionExpression(definitionExpression);
            } else if (node is BlockStatement block)
            {
                return buildBlockExpression(block);
            } else if (node is ReturnStatement returnStatement)
            {
                return buildReturnStatement(returnStatement);
            } else if (node is ClassLiteral classLiteral)
            {
                return buildClassLiteral(classLiteral);
            } else if (node is LetStatement letStatement)
            {
                return buildLetStatement(letStatement);
            } else if (node is NewExpression newExpression)
            {

            }
            return "";
        }
        private string buildNewExpression(NewExpression newExpression)
        {
            return "";
        }
        private string buildLetStatement(LetStatement letStatement)
        {
            // parse definition
            var definition = Build(letStatement.Definition);
            // add to defines
            if(blockLevel == 0)
            {
                var vartype = Build(letStatement.Definition.ObjType);
                var varname = Build(letStatement.Definition.Name);
                objectDefines.Add(vartype + " " + varname + ";");
            }
            return definition + ";";
        }
        public string Build(Ast ast)
        {
            var result = Template;
            var body = "";
            ast.Root.ForEach(node =>
            {
                body += Build(node) + "\n";
            });
            // replace type defines
            var type_defines = "";
            typeDefines.ForEach(code => type_defines += code + "\n");
            result = result.Replace("{{{ type_defines }}}", type_defines);

            // replace function defines
            var object_defines = "";
            objectDefines.ForEach(code => object_defines += code + "\n");
            result = result.Replace("{{{ object_defines }}}", object_defines);

            // replace body
            result = result.Replace("{{{ body }}}", body);
            return result;
        }
    }
}
