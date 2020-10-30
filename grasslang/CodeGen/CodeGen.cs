using System;
namespace grasslang.CodeGens
{
    public interface CodeGen
    {
        void Build();
        string GetCode();
    }
}
