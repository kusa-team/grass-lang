using System.Collections.Generic;
using grasslang.Scripting.BaseType;
using grasslang.Scripting.DotnetType;
using grasslang.CodeModel;
namespace grasslang.Scripting
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
            return new String(stringLiteral.Value)
            {
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
            if (!functionLiteral.Anonymous)
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
        private Object evalWithScope(Object context, BlockStatement block)
        {
            Scope scope = new Scope()
            {
                Engine = this,
                Parent = context
            };
            ExecutionContext.Push(scope);
            Object result = Eval(scope, block);
            ExecutionContext.Pop();
            return result;
        }
        private Object evalCallExpression(Object context, CallExpression call)
        {
            Object result;
            string name = call.Function.Literal;
            if (context[name] is Callable target)
            {
                List<Object> callParams = new List<Object>();
                foreach (Expression param in call.Parameters)
                {
                    callParams.Add(Eval(ExecutionContext.Peek(), param));
                }
                result = target.Invoke(callParams);
            }
            else
            {
                throw new System.Exception("The object named \'" +
                    name + "\' is not a function.");
            }
            return result;
        }
        private Object evalNewExpression(Object context, NewExpression expression)
        {
            CallExpression ctorCallExp;
            Prototype prototype;
            if (expression.ctorCall is CallExpression ctorCall)
            {
                string typeName = ctorCall.Function.Literal;
                Object prototypeObj = ExecutionContext.Peek()[typeName];
                if (prototypeObj is not Prototype)
                {
                    throw new System.Exception("The object named \"" +
                        typeName + "\" is not a type");
                }
                ctorCallExp = ctorCall;
                prototype = prototypeObj as Prototype;
            } else
            {
                // is PathExpression
                PathExpression typePath = expression.ctorCall as PathExpression;
                List<Expression> path = typePath.Path;
                Expression pathCall = path[path.Count - 1];
                if (pathCall is not CallExpression)
                {
                    throw new System.Exception("Need a type");
                }
                // get the parent of prototype
                PathExpression parentPath = typePath.SubPath(0, -1);
                Object parent = Eval(ExecutionContext.Peek(), parentPath);
                ctorCallExp = pathCall as CallExpression;
                if (parent[ctorCallExp.Function.Literal] is Prototype prototypeTarget)
                {
                    prototype = prototypeTarget;
                } else
                {
                    throw new System.Exception("Need a type");
                }
            }
            Prototype type = prototype;
            List<Object> callParams = new List<Object>();
            foreach (Expression param in ctorCallExp.Parameters)
            {
                callParams.Add(Eval(ExecutionContext.Peek(), param));
            }
            return type.Create(callParams);
        }
        private Object evalAssignExpression(Object context, AssignExpression expression)
        {
            // 取得父对象和键
            TextExpression text = expression.Left;
            Object parentContext = null;
            IdentifierExpression key = null;
            if (text is PathExpression path)
            {
                parentContext = Eval(context, path.SubPath(0, path.Length - 1));
                if (path.SubPath(path.Length - 1).Path[0]
                    is IdentifierExpression identifier)
                {
                    key = identifier;
                }
                else
                {
                    throw new System.Exception("You can only assign it to a key.");
                }
            }
            else if (text is IdentifierExpression identifier)
            {
                parentContext = context;
                key = identifier;
            }
            // 取得右值
            Object value = Eval(context, expression.Right);
            parentContext[key.Literal] = value;
            return value;
        }
        private void evalIfExpression(Object context, IfExpression ifExpression)
        {
            var condition = Eval(context, ifExpression.Condition);
            if(condition is Bool succ)
            {
                if(succ.Value == true)
                {
                    evalWithScope(ExecutionContext.Peek(), ifExpression.Consequence);
                }
            } else
            {
                throw new System.Exception("Required a bool!");
            }
        }
        private Object evalInfixExpression(InfixExpression infixExpression)
        {
            var context = ExecutionContext.Peek();
            var left = Eval(context, infixExpression.Left);
            var right = Eval(context, infixExpression.Right);
            Object result = null;
            switch (infixExpression.Operator.Type)
            {
                case Token.TokenType.Equal:
                    result = new Bool(left.Same(right));
                    break;
                case Token.TokenType.NotEqual:
                    result = new Bool(!left.Same(right));
                    break;
                case Token.TokenType.Plus:
                    result = left.Add(right);
                    break;

                default: break;
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
                if (pathExpression.Path.Count != 0)
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
                }
                else
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
            else if (node is AssignExpression assignExpression)
            {
                result = evalAssignExpression(context, assignExpression);
            }
            else if (node is BooleanLiteral booleanLiteral)
            {
                result = new Bool(booleanLiteral.Value);
            }
            else if (node is NewExpression newExpression)
            {
                result = evalNewExpression(context, newExpression);
            }
            else if (node is IfExpression ifExpression)
            {
                evalIfExpression(context, ifExpression);
            }
            else if (node is InfixExpression infixExpression)
            {
                result = evalInfixExpression(infixExpression);
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
        private void loadGlobal()
        {
            RootContext["Clr"] = new DotnetNamespace();
            RootContext["Object"] = new ObjectPrototype();
            RootContext["System"] = new DotnetObject(new SystemClass());
        }
        public Engine()
        {
            ExecutionContext.Push(RootContext);
            loadGlobal();
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
        public virtual bool Same(Object obj) => obj == this;
        public virtual Object Add(Object obj) => null;
        public virtual String GetString() => null;
    }
    public class Scope : Object
    {
        public override void setItem(string key, Object value)
        {
            if(!Items.ContainsKey(key))
            {
                if(Parent != null)
                {
                    try
                    {
                        // find key in parents
                        Parent.findItem(key);
                        Parent[key] = value;
                    }
                    catch
                    {
                        // not found
                    }
                }
            }
            base.setItem(key, value);
        }
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