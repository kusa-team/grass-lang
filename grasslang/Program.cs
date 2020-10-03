﻿using System;
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
            Console.WriteLine("Lexer耗时：{0}ms，Parser耗时：{1}ms", sw1.ElapsedMilliseconds, sw2.ElapsedMilliseconds);
        }
    }
}