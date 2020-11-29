using System;
using System.IO;
using grasslang.Script;

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
                Context context = new Context();
                Parser parser = new Parser();
                parser.Lexer = new Lexer(File.ReadAllText(projectfile));
                parser.InitParser();
                Ast ast = parser.BuildAst();
                context.Scopes.Peek().Children["TestWrite"]
                    = new DotnetMethod(new Functions(), "TestWrite");
                context.Eval(ast);
                return;
            }
        }
    }
    class Functions
    {
        public void TestWrite()
        {
            Console.WriteLine("Call in Context!");
        }
    }
}