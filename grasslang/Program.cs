using System;
using System.Diagnostics;
using System.IO;
using grasslang.CodeGens;
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
            AstParser parser = new AstParser(lexer);
            Ast ast = parser.BuildAst();
            sw2.Stop();
            /*
            Stopwatch sw3 = Stopwatch.StartNew();
            CodeGen codeGen = new GntGen(ast);
            codeGen.Build();
            string code = codeGen.GetCode();
            sw3.Stop();*/

            Console.WriteLine("// Lexer耗时：{0}ms，Parser耗时：{1}ms，CGen耗时：{2}ms", sw1.ElapsedMilliseconds, sw2.ElapsedMilliseconds, 0);
            //Console.WriteLine(code);
        }
    }
}