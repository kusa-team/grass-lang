using grasslang.CodeModel;
using System.Collections.Generic;

namespace grasslang.Scripting.BaseType
{
    public class Number : Object
    {
        public double Value;
        public Number(double value)
        {
            Value = value;
        }
    }
    public class String : Object
    {
        public string Value;
        public String(string value)
        {
            Value = value;
        }
    }
    public class Bool : Object
    {
        public bool Value;
        public Bool(bool value)
        {
            Value = value;
        }
    }
    public class Callable : Object
    {
        public virtual Object Invoke(List<Object> callParams) => null;
    }
    public class Prototype : Object
    {
        public virtual Object Create(List<Object> ctorParams) => null;
    }
    public class Function : Callable
    {
        public BlockStatement Block;
        public List<DefinitionExpression> Parameters;
        public override Object Invoke(List<Object> callParams)
        {
            Scope functionScope = new Scope()
            {
                Engine = Engine,
                Parent = Parent
            };
            int index = 0;
            
            foreach (DefinitionExpression definition in Parameters)
            {
                Object param;
                if (callParams.Count <= index)
                {
                    param = null;
                } else
                {
                    param = callParams[index];
                }
                functionScope[definition.Name.Literal] = param;
                index++;
            }
            Engine.ExecutionContext.Push(functionScope);
            Object result = Engine.Eval(functionScope, Block);
            Engine.ExecutionContext.Pop();
            return result;
        }
    }
}
