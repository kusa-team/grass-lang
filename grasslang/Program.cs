using System;
using System.IO;
using grasslang.Scripting;
using grasslang.Scripting.DotnetType;
using grasslang.CodeModel;
using grasslang.Compile;
namespace grasslang
{
    
    class Program
    {
        static void Main(string[] args)
        {
            ArgumentParser argument = new ArgumentParser(args);
            argument.AddValue("project", new string[] { "-p", "--project" });
            argument.AddSwitch("version", new string[] { "-v", "--version" });
            argument.Parse();
            if(argument["version"] is true)
            {
                Console.WriteLine("Grasslang debug 0.22.");
            }
            if(argument["project"] is string and { Length: >0 } projectfile)
            {
                parseProject(projectfile);
                return;
            }
        }
        private static void parseProject(string projectfile)
        {
            Engine engine = new Engine();
            Parser parser = new Parser();
            parser.Lexer = new Lexer(File.ReadAllText(projectfile));
            parser.InitParser();
            Ast ast = parser.BuildAst();
            Project project = new Project();
            engine.RootContext["Project"]
                = new DotnetObject(project);
            engine.Eval(ast);
        }
    }
}