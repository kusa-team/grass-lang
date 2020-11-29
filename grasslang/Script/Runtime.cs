using System.Collections.Generic;
namespace grasslang.Script
{
    public class Runtime
    {
        public Object Root = null;
        public Stack<Object> Context = new Stack<Object>();
        public Runtime()
        {
            Root = new Object() { Runtime = this };
            Context.Push(Root);
        }
    }
}
