using System.Collections.Generic;
using System.Reflection;
namespace grasslang.Script
{

    public class Context
    {
        public Object this[string key]
        {
            set
            {
                Scopes.Peek().Items[key] = value;
            }
            get
            {
                return Scopes.Peek().Items[key];
            }
        }
        public Stack<Scope> Scopes = new Stack<Scope>();
        public Context()
        {
            Scopes.Push(new Scope());
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
            if (node is ExpressionStatement expressionStatement)
            {
                Eval(expressionStatement.Expression);
            }
            else if (node is CallExpression call)
            {
                Object target = Eval(call.Function);
                if (target is not Callable)
                {
                    throw new System.Exception("Error");
                }
                List<Object> callParams = new List<Object>();
                foreach (Expression param in call.Parameters)
                {
                    callParams.Add(Eval(param));
                }
                return (target as Callable).Invoke(callParams);
            }
            else if (node is IdentifierExpression identifier)
            {
                return findInScopes(identifier.Literal);
            }
            else if (node is FunctionLiteral functionLiteral)
            {
                Function function = new Function() { Context = this };
                function.Block = functionLiteral.Body;
                Scopes.Peek().Items[functionLiteral.FunctionName.Literal] = function;
            }
            else if (node is StringLiteral stringLiteral)
            {
                return new StringObject()
                {
                    Literal = stringLiteral.Value
                };
            }
            else if (node is NumberLiteral numberLiteral)
            {
                return new NumberObject()
                {
                    Literal = int.Parse(numberLiteral.Value)
                };
            }
            else if (node is PathExpression pathExpression)
            {
                Object top = null;
                foreach (Expression layer in pathExpression.Path)
                {
                    if (layer is IdentifierExpression identifier1)
                    {
                        string name = identifier1.Literal;
                        top = top is null ? findInScopes(name) : top.GetChildren(name);
                    }
                    else if (layer is CallExpression call1)
                    {
                        string name = (call1.Function as IdentifierExpression).Literal;
                        Object target = top.GetChildren(name);
                        List<Object> callParams = new List<Object>();
                        foreach (Expression param in call1.Parameters)
                        {
                            callParams.Add(Eval(param));
                        }
                        top = (target as Callable).Invoke(callParams);
                    }
                }
                return top;
            }
            else if (node is AssignExpression assignExpression)
            {
                if(Eval(assignExpression.Left) is not Variable variable)
                {
                    throw new System.Exception("Error");
                }
                Object value = Eval(assignExpression.Right);
                variable.Set(value);
                return value;
            }
            return null;
        }
        private Object findInScopes(string name)
        {
            foreach(Scope scope in Scopes)
            {
                foreach (var attr in scope.Items)
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

    public class Scope
    {
        public Dictionary<string, Object> Items = new Dictionary<string, Object>();
    }


    public class Object
    {
        public Dictionary<string, Object> Children = new Dictionary<string, Object>();
        public Context Context;
        public virtual object toDotnetObject() { return null; }

        public virtual void SetChildren(string key, Object value) {  }
        public virtual Object GetChildren(string key) { return null; }
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

    public class StringObject : Object
    {
        public string Literal = "";
        public override object toDotnetObject()
        {
            return Literal;
        }
    }
    public class NumberObject : Object
    {
        public int Literal = 0;
        public override object toDotnetObject()
        {
            return Literal;
        }
    }

    public class DotnetObject : Object
    {
        public object RawObject;

        public DotnetObject(object raw)
        {
            RawObject = raw;
        }
        public override Object GetChildren(string key)
        {
            System.Type type = RawObject.GetType();
            FieldInfo field = type.GetField(key);
            if (field is null)
            {
                return new DotnetMethod(RawObject, key);
            }
            return new DotnetField(RawObject, key);
        }
    }
    public class DotnetField : Variable
    {
        private FieldInfo field;
        private object instance;
        public DotnetField(object obj, string name)
        {
            field = obj.GetType().GetField(name);
            instance = obj;
        }

        public override void Set(Object value)
        {
            field.SetValue(instance, value.toDotnetObject());
        }

        public override Object Get()
        {
            return new DotnetObject(field.GetValue(instance));
        }

        public override Object GetChildren(string key)
        {
            return Get().GetChildren(key);
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
        public override Object Invoke(List<Object> callParams)
        {
            List<object> dotnetParams = new List<object>();
            foreach(Object param in callParams)
            {
                dotnetParams.Add(param.toDotnetObject());
            }
            method.Invoke(instance, dotnetParams.ToArray());
            return null;
        }
    }
}
