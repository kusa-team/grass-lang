using System;
using System.IO;
using System.Linq;
using grasslang.Build;
using grasslang.Compile;
using grasslang.CodeModel;
namespace grasslang
{
    
    class Program
    {
        static void Main(string[] args)
        {
            ArgumentParser arguments = new ArgumentParser(args);
            // build system
            arguments.AddValue("project", new string[] { "-p", "--project" });
            arguments.AddValue("tasks", new string[] { "-t", "--tasks" }, "build");
            // common
            arguments.AddSwitch("version", new string[] { "-v", "--version" });
            arguments.AddSwitch("script", new string[] { "--script" });
            arguments.AddValue("codegen", new string[] { "-c", "--codegen" });
            arguments.Parse();
            if(arguments["version"] is true)
            {
                Console.WriteLine("Grasslang debug 0.24.");
            }
            if(arguments["codegen"] is string and { Length: >0 } genfile)
            {
                CodeGen codeGen = new CodeGen();
                Parser parser = new Parser
                {
                    Lexer = new Lexer(File.ReadAllText(genfile))
                };
                parser.InitParser();
                var ast = parser.BuildAst();
                var code = codeGen.Build(ast);
                Console.WriteLine(code);
            }
            if(arguments["project"] is string and { Length: >0 } project)
            {
                runProject(project, arguments);
                return;
            }
        }
        private static void runProject(string projectfile, ArgumentParser arguments)
        {
            // root project
            Project rootProject = new Project();
            rootProject.LoadProject(projectfile);

            // run tasks
            var tasks = (from task in (arguments["tasks"] as string).Split(',')
                         select task.Trim());
            foreach (string task in tasks)
            {
                rootProject.RunTask(task);
            }
        }
    }
}