using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
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

                Functions injectFunction = new Functions();
                context.Scopes.Peek().Children["TestWrite"]
                    = new DotnetMethod(injectFunction, "TestWrite");
                context.Scopes.Peek().Children["Write"]
                    = new DotnetMethod(injectFunction, "Write");
                context.Scopes.Peek().Children["Sleep"]
                    = new DotnetMethod(injectFunction, "Sleep");
                context.Scopes.Peek().Children["Clear"]
                    = new DotnetMethod(injectFunction, "Clear");

                context.Eval(ast);
                context.Eval(new CallExpression()
                {
                    Function = new IdentifierExpression(null, "main"),
                    Parameters = new List<Expression>()
                });
                return;
            }
        }
    }
    class Functions
    {
        public void Write(string text)
        {
            Console.WriteLine(text);
        }
        public void Clear()
        {
            Console.Clear();
        }
        public void Sleep(int time)
        {
            Thread.Sleep(time);
        }
    }
}