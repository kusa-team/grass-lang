using System.Collections.Generic;
using System.Reflection;
namespace grasslang.Script
{

    public class Context
    {
        public Stack<Object> Scopes = new Stack<Object>();
        public Context()
        {
            Scopes.Push(new Object() { Context = this });
        }
        public Object Eval(Ast ast)
        {
            Object result = null;
            foreach(Node node in ast.Root)
            {
                result = Eval(node);
            }
            return result;
        }
        public Object Eval(BlockStatement block)
        {
            Object result = null;
            foreach (Node node in block.Body)
            {
                result = Eval(node);
            }
            return result;
        }
        public Object Eval(Node node)
        {
            if(node is ExpressionStatement expressionStatement)
            {
                Eval(expressionStatement.Expression);
            } else if(node is CallExpression call)
            {
                Object target = Eval(call.Function);
                if(target is not Callable)
                {
                    throw new System.Exception("Need callable!");
                }
                List<Object> callParams = new List<Object>();
                foreach(Expression param in call.Parameters)
                {
                    callParams.Add(Eval(param));
                }
                return (target as Callable).Invoke(callParams);
            } else if (node is IdentifierExpression identifier)
            {
                return findInScopes(identifier.Literal);
            } else if (node is FunctionLiteral functionLiteral)
            {
                Function function = new Function() { Context = this };
                function.Block = functionLiteral.Body;
                Scopes.Peek().Children[functionLiteral.FunctionName.Literal] = function;
            }
            return null;
        }
        private Object findInScopes(string name)
        {
            foreach(Object scope in Scopes)
            {
                foreach (var attr in scope.Children)
                {
                    if(attr.Key == name)
                    {
                        return attr.Value;
                    }
                }
            }
            return null;
        }
    }

    public class Object
    {
        public Dictionary<string, Object> Children = new Dictionary<string, Object>();
        public Context Context;
        public virtual object toDotnetObject() { return null; }
    }
    public class Variable : Object
    {
        public virtual Object Get() { return null; }
        public virtual void Set(Object value) { }
    }
    public class Callable : Object
    {
        public virtual Object Invoke(List<Object> param) { return null; }
    }
    public class Function : Callable
    {
        public BlockStatement Block;
        public override Object Invoke(List<Object> param)
        {
            return Context.Eval(Block);
        }
    }
    public class DotnetMethod : Callable
    {
        private MethodInfo method;
        private object instance;
        public DotnetMethod(object obj, string methodname)
        {
            method = obj.GetType().GetMethod(methodname);
            instance = obj;
        }
        public override Object Invoke(List<Object> param)
        {
            method.Invoke(instance, param.ToArray());
            return null;
        }
    }
}
