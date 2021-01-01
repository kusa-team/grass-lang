using System.Collections.Generic;
using System.IO;
using System;
using System.Reflection;
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
    public class SystemClass
    {
        public static void LoadLibrary(string path)
        {
            string[] possiblePaths =
            {
                path,
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Build/Services", path)
            };
            bool success = false;
            foreach(string currentPath in possiblePaths)
            {
                if(File.Exists(currentPath))
                {
                    AppDomain.CurrentDomain.Load(File.ReadAllBytes(currentPath));
                    success = true;
                    break;
                }
            }
            if(!success)
            {
                throw new Exception("Library not found.");
            }
        }
    }
}
