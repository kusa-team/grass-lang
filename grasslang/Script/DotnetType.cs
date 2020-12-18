using System.Collections.Generic;
using System.Reflection;
using grasslang.Script.BaseType;
namespace grasslang.Script.DotnetType
{
    public class DotnetObject : Object
    {
        public static Object toScriptObject(Engine engine, object obj)
        {
            if (obj is string string_obj)
            {
                return new String()
                {
                    Value = string_obj,
                    Engine = engine
                };
            } else if (obj is Object script_obj)
            {
                return script_obj;
            }
            return new DotnetObject(obj)
            {
                Engine = engine
            };
        }
        public static object toObject(Object obj)
        {
            if(obj is String str)
            {
                return str.Value;
            } else if (obj is DotnetObject dotnetObject)
            {
                return dotnetObject.Target;
            }
            return obj;
        }
        public object Target;
        public System.Type Type;
        private List<MemberInfo> members;
        public DotnetObject(object target)
        {
            Target = target;
            Type = target.GetType();
            members = new List<MemberInfo>(Type.GetMembers());
        }
        private MemberInfo GetMember(string name)
        {
            foreach(MemberInfo member in members)
            {
                if(member.Name == name)
                {
                    return member;
                }
            }
            return null;
        }
        public override Object findItem(string key)
        {
            if(GetMember(key) is MemberInfo member)
            {
                MemberTypes type = member.MemberType;
                if(type == MemberTypes.Method)
                {
                    return new DotnetMethod(Target, key)
                    {
                        Engine = Engine
                    };
                } else if (type == MemberTypes.Field)
                {
                    object data = Type.GetField(key).GetValue(Target);
                    return toScriptObject(Engine, data);
                }
            }
            return base.findItem(key);
        }
        public override void setItem(string key, Object value)
        {
            if (GetMember(key) is MemberInfo member)
            {
                MemberTypes type = member.MemberType;
                if (type == MemberTypes.Method)
                {
                    throw new System.Exception("You can't assign it to a function.");
                }
                else if (type == MemberTypes.Field)
                {
                    Type.GetField(key).SetValue(Target, DotnetObject.toObject(value));
                }
            }
            else
            {
                base.setItem(key, value);
            }
        }
    }

    public class DotnetMethod : Callable
    {
        public object Target;
        public System.Type Type;
        private string methodName;
        public DotnetMethod(object target, string name)
        {
            Target = target;
            Type = target.GetType();
            methodName = name;
        }
        public override Object Invoke(List<Object> callParams)
        {
            List<System.Type> types = new List<System.Type>();
            List<object> targetParams = new List<object>();
            foreach(Object param in callParams)
            {
                object targetParam = DotnetObject.toObject(param);
                targetParams.Add(targetParam);
                types.Add(targetParam.GetType());
            }
            MethodInfo method = Type.GetMethod(methodName, types.ToArray());
            object result = method.Invoke(Target, targetParams.ToArray());
            if(result is null)
            {
                return null;
            }
            return DotnetObject.toScriptObject(Engine, result);
        }
    }
}
