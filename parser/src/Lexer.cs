using System.Text.RegularExpressions;

namespace MMLGB
{
    public static class Lexer
    {
        public enum TokenType
        {
            COMMENT, NUMBER, HEXNUMBER, BINNUMBER,
            CHANNEL, NOTE, SHARP, DASH, COMMAND,
            DOT, COMMA, TIE, MACRO, ASSIGN,
            LCURLY, RCURLY, LBRACKET, RBRACKET,
            NEWLINE, WHITESPACE, EOF
        }

        public class Token
        {
            public TokenType Type { get; }
            public string Data { get; }
            public int Line { get; }
            public int Pos { get; }

            public Token(TokenType type, string data, int line = 0, int pos = 0)
            {
                Type = type;
                Data = data;
                Line = line;
                Pos = pos;
            }

            public Token(Token other) : this(other.Type, other.Data, other.Line, other.Pos) { }

            public override string ToString()
            {
                return $"({Type}, {Data})";
            }
        }

        private static readonly Dictionary<TokenType, string> TokenPatterns = new()
        {
            { TokenType.COMMENT, ";.*" },
            { TokenType.NUMBER, "[0-9]+" },
            { TokenType.HEXNUMBER, "0x[0-9]+" },
            { TokenType.BINNUMBER, "0b[0-1]+" },
            { TokenType.CHANNEL, "[ABCD]" },
            { TokenType.NOTE, "[cdefgab]" },
            { TokenType.SHARP, "[#\\+]" },
            { TokenType.DASH, "-" },
            { TokenType.COMMAND, "[rwo<>lvtysL]" },
            { TokenType.DOT, "\\." },
            { TokenType.COMMA, "," },
            { TokenType.TIE, "\\^" },
            { TokenType.MACRO, "(@@|@po|@p|@ns|@ve|@v|@wave|@wd)" },
            { TokenType.ASSIGN, "=" },
            { TokenType.LCURLY, "\\{" },
            { TokenType.RCURLY, "\\}" },
            { TokenType.LBRACKET, "\\[" },
            { TokenType.RBRACKET, "\\]" },
            { TokenType.NEWLINE, "\n" },
            { TokenType.WHITESPACE, "[ \\t\\f\\r]+" },
            { TokenType.EOF, "" }
        };

        public static List<Token> Lex(string input)
        {
            var tokens = new List<Token>();

            var patternBuffer = new List<string>();
            foreach (var kvp in TokenPatterns)
            {
                patternBuffer.Add($"(?<{kvp.Key}>{kvp.Value})");
            }

            string combinedPattern = string.Join("|", patternBuffer);
            var tokenRegex = new Regex(combinedPattern, RegexOptions.Compiled);

            int line = 1;
            int lineStart = 0;

            var matches = tokenRegex.Matches(input);
            foreach (Match match in matches)
            {
                int pos = match.Index - lineStart + 1;

                foreach (TokenType type in Enum.GetValues(typeof(TokenType)))
                {
                    if (type == TokenType.EOF) continue; // not a real pattern match

                    var group = match.Groups[type.ToString()];
                    if (group.Success)
                    {
                        if (type == TokenType.COMMENT || type == TokenType.WHITESPACE)
                            break;

                        if (type == TokenType.NEWLINE)
                        {
                            tokens.Add(new Token(type, group.Value, line, pos));
                            line++;
                            lineStart = match.Index + 1;
                        }
                        else
                        {
                            tokens.Add(new Token(type, group.Value, line, pos));
                        }
                        break;
                    }
                }
            }

            tokens.Add(new Token(TokenType.EOF, "", line, 0));
            return tokens;
        }
    }
}
