using System.IO;
using grasslang.Script;

namespace grasslang
{
    
    class Program
    {
        static void Main(string[] args)
        {
            ArgumentParser argument = new ArgumentParser(args);
            argument.AddValue("project", new string[] { "-p", "--project" }, "");
            argument.Parse();

            if((string)argument["project"] is { Length: >0 })
            {
                return;
            }
        }
    }
}