using System;
namespace grasslang
{
    public class InfoHandler
    {
        public static void PrintRawError(string info = "")
        {
            Console.Error.WriteLine("Error: " + info);
            Environment.Exit(1);
        }
        public class ErrorType
        {
            // translate by deepl translator
            public static string FUNCTION_NAME_INVALID = "The function name must be an identifier";
            public static string FUNCTION_ARGUMENT_INVALID = "There is a syntax error in the function arguments";
            public static string FUNCTION_TYPE_INVALID = "There is an error in the return value type of the function";
            public static string FUNCTION_BODY_INVALID = "There is an error in the body of the function";

            public static string EXPRESSION_INVALID = "There is an error in an expression";
        }
        public static void PrintFormatedError(string type, params string[] info)
        {
            PrintRawError(string.Format(type, info));
        }

        
    }
}
