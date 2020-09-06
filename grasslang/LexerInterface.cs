namespace grasslang
{
    public interface LexerInterface
    {
        Token GetNextToken();
        Token PeekToken();
        Token CurrentToken();
    }
}