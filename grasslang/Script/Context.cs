using System.Collections.Generic;
namespace grasslang.Script
{
    public class Object
    {
        public Dictionary<Literal, Object> Children = new Dictionary<Literal, Object>();
        public Runtime Runtime;
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
    public class Literal : Object { }

    public class Identifier : Literal
    {
        public string Value = "";
        public override object toDotnetObject()
        {
            return Value;
        }
    }
    public class Function : Callable
    {
        public BlockStatement Block;
        public override Object Invoke(List<Object> param)
        {
            return base.Invoke(param);
        }
    }
}
