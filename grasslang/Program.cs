using System;
using System.Diagnostics;
using System.IO;

namespace grasslang
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch sw1 = Stopwatch.StartNew();
            Lexer lexer = new Lexer(File.ReadAllText(args[0]));
            sw1.Stop();
            
            Stopwatch sw2 = Stopwatch.StartNew();
            Parser parser = new Parser(lexer);
            Ast ast = parser.BuildAst();
            sw2.Stop();
            //Console.WriteLine("Lexer耗时：{0}ms，Parser耗时：{1}ms", sw1.ElapsedMilliseconds, sw2.ElapsedMilliseconds);
            Stopwatch sw3 = Stopwatch.StartNew();
            ast.Root.ForEach((node) =>
            {
                Expression expression = ((ExpressionStatement) node).Expression;
                if (expression.GetType() == typeof(CallExpression))
                {
                    CallExpression callExpression = (CallExpression) expression;
                    if (callExpression.FunctionName.Literal == "print")
                    {
                        Console.WriteLine(((StringExpression)callExpression.ArgsList[0])
                            .Value);
                    }
                }
            });
            sw3.Stop();
            //Console.WriteLine("临时代码执行器耗时：{0}ms", sw3.ElapsedMilliseconds);
        }
    }
}