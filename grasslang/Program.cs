using System;
using System.IO;
using grasslang.Script;
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
                Console.WriteLine("Grasslang debug 0.21.");
            }
            if((string)argument["project"] is { Length: >0 } projectfile)
            {
                parseProject(projectfile);
                return;
            }
        }
        private static void parseProject(string projectfile)
        {
            Context context = new Context();
            Parser parser = new Parser();
            parser.Lexer = new Lexer(File.ReadAllText(projectfile));
            parser.InitParser();
            Ast ast = parser.BuildAst();
            Project project = new Project();
            context["Project"] = new DotnetObject(project);
            context["System"] = new DotnetObject(new DotnetHelper());
            context.Eval(ast);
        }
    }
}