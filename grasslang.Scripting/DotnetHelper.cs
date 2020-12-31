using System.Collections.Generic;
using grasslang.Scripting.BaseType;
namespace grasslang.Scripting.DotnetType
{
    public class ObjectPrototype : Prototype
    {
        public override Object Create(List<Object> ctorParams)
        {
            return new Object();
        }
    }
}
