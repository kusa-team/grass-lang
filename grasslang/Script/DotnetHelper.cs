using grasslang.Script.BaseType;
namespace grasslang.Script
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
    }
}
