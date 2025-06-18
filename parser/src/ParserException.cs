namespace MMLGB
{
    public class ParserException : Exception
    {
        public ParserException(string message) : base(message)
        {
        }

        public ParserException(string message, Lexer.Token token)
            : base($"On line {token.Line} column {token.Pos}: {message}")
        {
        }
    }
}
