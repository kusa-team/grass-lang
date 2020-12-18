using System.Collections.Generic;
using grasslang.Script.BaseType;
namespace grasslang.Script
{
    public class Engine
    {
        public Scope RootContext = new Scope();
        public Stack<Scope> ExecutionContext = new Stack<Scope>();
        private void evalLetStatement(Object context, LetStatement statement)
        {
            DefinitionExpression definition = statement.Definition;
            context[definition.Name.Literal] = Eval(context, definition.Value);
        }
        private Object evalStringLiteral(Object context, StringLiteral stringLiteral)
        {
            return new String()
            {
                Value = stringLiteral.Value,
                Parent = context,
                Engine = this
            };
        }
        private Object evalFunctionLiteral(Object context, FunctionLiteral functionLiteral)
        {
            Function function = new Function()
            {
                Engine = this,
                Parent = context,
                Block = functionLiteral.Body,
                Parameters = functionLiteral.Parameters
            };
            if(!functionLiteral.Anonymous)
            {
                string functionName = functionLiteral.FunctionName.Literal;
                context[functionName] = function;
            }
            return function;
        }
        private Object evalBlockStatement(Object context, BlockStatement block)
        {
            Object result = null;
            foreach (Node node in block.Body)
            {
                result = Eval(context, node);
            }
            return result;
        }
        private Object evalCallExpression(Object context, CallExpression call)
        {
            Object result;
            string name = call.Function.Literal;
            if (context[name] is Callable target)
            {
                List<Object> callParams = new List<Object>();
                foreach(Expression param in call.Parameters)
                {
                    callParams.Add(Eval(ExecutionContext.Peek(), param));
                }
                result = target.Invoke(callParams);
            } else
            {
                throw new System.Exception("The object named \'" +
                    name + "\' is not a function.");
            }
            return result;
        }
        public Object Eval(Object context, Node node)
        {
            Object result = null;
            if (node is LetStatement letStatement)
            {
                evalLetStatement(context, letStatement);
            }
            else if (node is StringLiteral stringLiteral)
            {
                result = evalStringLiteral(context, stringLiteral);
            }
            else if (node is FunctionLiteral functionLiteral)
            {
                result = evalFunctionLiteral(context, functionLiteral);
            }
            else if (node is ExpressionStatement expressionStatement)
            {
                result = Eval(context, expressionStatement.Expression);
            }
            else if (node is BlockStatement block)
            {
                result = evalBlockStatement(context, block);
            }
            else if (node is PathExpression pathExpression)
            {
                if(pathExpression.Path.Count != 0)
                {
                    Expression fristPath = pathExpression.Path[0];
                    Object nextContext;
                    if (fristPath is IdentifierExpression fristKey)
                    {
                        nextContext = context[fristKey.Literal];
                    }
                    else
                    {
                        nextContext = Eval(context, fristPath);
                    }
                    result = Eval(nextContext, pathExpression.SubPath(1));
                } else
                {
                    result = context;
                }
            }
            else if (node is CallExpression callExpression)
            {
                result = evalCallExpression(context, callExpression);
            }
            else if (node is IdentifierExpression identifierExpression)
            {
                result = context[identifierExpression.Literal];
            }
            return result;
        }
        public Object Eval(Node node)
        {
            return Eval(RootContext, node);
        }
        public Object Eval(Ast ast)
        {
            Object result = null;
            foreach (Node node in ast.Root)
            {
                result = Eval(node);
            }
            return result;
        }
        public Engine()
        {
            ExecutionContext.Push(RootContext);
        }
    }

    public class Object
    {
        public Engine Engine;
        public Object Parent;
        public Dictionary<string, Object> Items = new Dictionary<string, Object>();
        public virtual Object this[string index]
        {
            get
            {
                return findItem(index);
            }
            set
            {
                setItem(index, value);
            }
        }
        public virtual Object findItem(string key)
        {
            if (Items.ContainsKey(key))
            {
                return Items[key];
            }
            throw new System.Exception("Variable \"" + key + "\" not found.");
        }
        public virtual void setItem(string key, Object value)
        {
            Items[key] = value;
        }
    }
    public class Scope : Object
    {
        
        public override Object findItem(string key)
        {
            if (Items.ContainsKey(key))
            {
                return Items[key];
            }
            if (Parent == null)
            {
                throw new System.Exception("Variable \"" + key + "\" not found.");
            }
            return Parent.findItem(key);
        }
    }
}
