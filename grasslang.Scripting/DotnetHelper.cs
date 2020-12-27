using grasslang.Scripting.BaseType;
namespace grasslang.Scripting
{
    public class DotnetHelper
    {
        private Engine engine;
        public DotnetHelper(Engine Engine)
        {
            engine = Engine;
        }

        public Object CreateObject()
        {
            return new Object()
            {
                Engine = engine
            };
        }
        public Scope CreateScope()
        {
            return new Scope()
            {
                Engine = engine
            };
        }
    }
}
