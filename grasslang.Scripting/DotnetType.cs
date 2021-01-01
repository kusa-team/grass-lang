using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using grasslang.Scripting.BaseType;
using Type = System.Type;
using Activator = System.Activator;
namespace grasslang.Scripting.DotnetType
{
    public class DotnetNamespace : Object
    {
        public string Namespace = "";
        public static IEnumerable<Type> getClass(string name)
        {
            return from assembly in System.AppDomain.CurrentDomain.GetAssemblies()
                    from type in assembly.GetTypes()
                    where type.FullName == name
                    select type;
        }
        public static bool isNamespaceExists(string nsname)
        {
            return (from assembly in System.AppDomain.CurrentDomain.GetAssemblies()
                    from type in assembly.GetTypes()
                    where type.Namespace == nsname
                    select type).Any();
        }
        public override Object findItem(string key)
        {
            string newNamespace = Namespace;
            newNamespace += ((Namespace is { Length: > 0 }) ? "." : "") + key;
            if(isNamespaceExists(newNamespace))
            {
                return new DotnetNamespace
                {
                    Namespace = newNamespace
                };
            } else if (getClass(newNamespace) is var item)
            {
                if(item.Any())
                {
                    return new DotnetClass
                    {
                        ClassType = item.First()
                    };
                }
            }

            throw new System.Exception("Namespace \"" + newNamespace + "\" not found");
        }
    }
    public class DotnetClass : Prototype
    {
        public Type ClassType;
        public override Object Create(List<Object> ctorParams)
        {
            object instance = Activator.CreateInstance(ClassType, DotnetObject.toObjects(ctorParams).ToArray());
            return new DotnetObject(instance);
        }
        private Object getStaticMember(string key)
        {
            var members = ClassType.GetMembers(BindingFlags.Static | BindingFlags.Public);
            var target = (from member in members
                          where ((member.MemberType == MemberTypes.Field
                          || member.MemberType == MemberTypes.Property
                          || member.MemberType == MemberTypes.Method)
                          && member.Name == key)
                          select member);
            Object result = null;
            if (target.Any())
            {
                MemberInfo memberInfo = target.First();
                if (memberInfo.MemberType == MemberTypes.Property)
                {
                    result = new DotnetObject
                        ((memberInfo as PropertyInfo).GetValue(null));
                }
                else if (memberInfo.MemberType == MemberTypes.Field)
                {
                    result = new DotnetObject
                        ((memberInfo as FieldInfo).GetValue(null));
                }
                else
                {
                    result = new DotnetMethod
                    {
                        Target = null,
                        Type = ClassType,
                        methodName = key
                    };
                }
            }
            return result;
        }
        public override Object findItem(string key)
        {
            
            if(getStaticMember(key) is Object result)
            {
                return result;
            }
            return base.findItem(key);
        }
    }
    public class DotnetObject : Object
    {
        public static Object toScriptObject(Engine engine, object obj)
        {
            if (obj is string string_obj)
            {
                return new String(string_obj)
                {
                    Engine = engine
                };
            } else if (obj is Object script_obj)
            {
                return script_obj;
            } else if (obj is bool boolean)
            {
                return new Bool(boolean);
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
            } else if (obj is Bool boolean)
            {
                return boolean.Value;
            }
            return obj;
        }
        public static List<object> toObjects(List<Object> objs)
        {
            List<object> result = new List<object>();
            foreach (Object param in objs)
            {
                object obj = toObject(param);
                result.Add(obj);
            }
            return result;
        }
        public object Target;
        public Type Type;
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
        public Type Type;
        public string methodName;
        public DotnetMethod() { }
        public DotnetMethod(object target, string name)
        {
            Target = target;
            Type = target.GetType();
            methodName = name;
        }
        public override Object Invoke(List<Object> callParams)
        {
            List<Type> types = new List<Type>();
            List<object> targetParams = DotnetObject.toObjects(callParams);
            foreach (var aparam in targetParams)
            {
                types.Add(aparam.GetType());
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
