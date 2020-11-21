using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace grasslang
{
    [DebuggerDisplay("Token = \"{Literal}\"")]
    public class Token
    {
        public enum TokenType
        {
            Assign, // =
            Not, // !
            
            Plus, // +
            Minus, // -
            Asterisk, // *
            Slash, // /
            
            LessThen, // <
            GreaterThen, // >
            
            Equal, // ==
            NotEqual, // !=
            
            Comma, // ,
            Semicolon, // ;
            Dot, // .
            Colon, // :

            LeftParen, // (
            RightParen, // )
            LeftBrace, // {
            RightBrace, // }
            LeftBrack, // [
            RightBrack, // ]


            Function,
            Let,
            True,
            False,
            If,
            Else,
            Return,

            NextLine,
            Eof,
            Identifier,
            String,
            Number,
            Internal

        }

        public static Token Create(TokenType type, char ch)
        {
            return CreateWithString(type, "" + ch);
        }

        public static Token CreateWithString(TokenType type, string literal)
        {
            return new Token(type, literal);
        }

        public static Token EOF
        {
            get
            {
                return Token.Create(TokenType.Eof, '\x00');
            }
        }
        public Token(TokenType _type, string _literal)
        {
            Type = _type;
            Literal = _literal;
        }
        
        public TokenType Type;
        public string Literal;
    }
    public class Lexer : LexerInterface
    {
        private string code = null;
        private int pos = -1; 
        private char ch = '\x00';
        public Lexer(string codestr)
        {
            code = codestr;
            NextChar();
        }

        private char NextChar()
        {
            pos++;
            if (pos >= code.Length)
            {
                return ch = '\x00';
            }
            return ch = code[pos];
        }
        private char PeekChar()
        {
            if (pos + 1 >= code.Length)
            {
                return '\x00';
            }
            return code[pos + 1];
        }

        public static bool IsWhitespace(char ch)
        {
            return ch == ' ' || ch == '\r' || ch == '\n' || ch == '\t';
        }

        public static bool IsNumber(char ch)
        {
            return '0' <= ch && '9' >= ch;
        }

        public static bool IsNumberString(string str)
        {
            int type = 0; // 0 = int, 1 = float, 2 = hex
            bool result = true;
            foreach (var ch in str)
            {
                if (!IsNumber(ch) && type == 0)
                {
                    if (ch == 'x' || ch == 'X')
                    {
                        type = 2;
                    }
                    else if(ch == '.')
                    {
                        type = 1;
                    }
                    else
                    {
                        result = false;
                    }
                }
                else
                {
                    result = false;
                }
            }
            return result;
        }
        public static Dictionary<string, Token.TokenType> KeyworDictionary = new Dictionary<string, Token.TokenType>
        {
            {"fn", Token.TokenType.Function},
            {"let", Token.TokenType.Let},
            {"true", Token.TokenType.True},
            {"false", Token.TokenType.False},
            {"if", Token.TokenType.If},
            {"else", Token.TokenType.Else},
            {"return", Token.TokenType.Return},
            {"\n", Token.TokenType.NextLine }
        };
        public static Token.TokenType GetTokenType(string literal)
        {
            Token.TokenType type = Token.TokenType.Identifier;
            if (KeyworDictionary.ContainsKey(literal))
            {
                type = KeyworDictionary[literal];
            } else if (IsNumberString(literal))
            {
                type = Token.TokenType.Number;
            }
            return type;
        }

        public Token PeekToken()
        {
            // 这里...写程序送bug，靠
            int beforePos = pos;
            Token beforeCurrent = current;
            char beforeCh = ch;
            
            Token result = GetNextToken();
            
            pos = beforePos;
            current = beforeCurrent;
            ch = beforeCh;
            return result;
        }

        private Token current = null;
        public Token CurrentToken()
        {
            return current;
        }

        public static bool IsStopFlag(char ch)
        {
            return IsWhitespace(ch) || "=!+-*/<>,;{}()[].".IndexOf(ch) != -1 || ch == '\x00';
        }
        private void SkipWhitespace()
        {
            if (IsWhitespace(ch))
            {
                while (IsWhitespace(ch))
                {
                    NextChar();
                }
            }
        }
        
        public Token GetNextToken()
        {
            SkipWhitespace();
            Token tok = null;
            switch (ch)
            {
                case '=':
                    {
                        if (PeekChar() == '=') // is '=' or '=='
                        {
                            tok = Token.CreateWithString(Token.TokenType.Equal, "==");
                            NextChar(); // eat second '='
                        }
                        else
                        {
                            tok = Token.Create(Token.TokenType.Assign, ch);
                        }
                        break;
                    }
                case '!':
                    {
                        if (PeekChar() == '=') // is '!' or '!='
                        {
                            tok = Token.CreateWithString(Token.TokenType.NotEqual, "!=");
                            NextChar(); // eat second '='
                        }
                        else
                        {
                            tok = Token.Create(Token.TokenType.Not, ch);
                        }
                        break;
                    }


                // + - * /
                case '+':
                    {
                        tok = Token.Create(Token.TokenType.Plus, ch);
                        break;
                    }
                case '-':
                    {
                        tok = Token.Create(Token.TokenType.Minus, ch);
                        break;
                    }
                case '*':
                    {
                        tok = Token.Create(Token.TokenType.Asterisk, ch);
                        break;
                    }
                case '/':
                    {
                        if (PeekChar() == '/')
                        {
                            while(NextChar() != '\n') { }
                            NextChar();
                            tok = GetNextToken();
                        }
                        else
                        {
                            tok = Token.Create(Token.TokenType.Slash, ch);
                        }
                        
                        break;
                    }


                // < >
                case '<':
                    {
                        tok = Token.Create(Token.TokenType.LessThen, ch);
                        break;
                    }
                case '>':
                    {
                        tok = Token.Create(Token.TokenType.GreaterThen, ch);
                        break;
                    }


                // , ; . :
                case ',':
                    {
                        tok = Token.Create(Token.TokenType.Comma, ch);
                        break;
                    }
                case ';':
                    {
                        tok = Token.Create(Token.TokenType.Semicolon, ch);
                        break;
                    }
                case '.':
                    {
                        tok = Token.Create(Token.TokenType.Dot, ch);
                        break;
                    }
                case ':':
                    {
                        tok = Token.Create(Token.TokenType.Colon, ch);
                        break;
                    }

                // ( ) { }
                case '(':
                    {
                        tok = Token.Create(Token.TokenType.LeftParen, ch);
                        break;
                    }
                case ')':
                    {
                        tok = Token.Create(Token.TokenType.RightParen, ch);
                        break;
                    }
                case '{':
                    {
                        tok = Token.Create(Token.TokenType.LeftBrace, ch);
                        break;
                    }
                case '}':
                    {
                        tok = Token.Create(Token.TokenType.RightBrace, ch);
                        break;
                    }
                case '[':
                    {
                        tok = Token.Create(Token.TokenType.LeftBrack, ch);
                        break;
                    }
                case ']':
                    {
                        tok = Token.Create(Token.TokenType.RightBrack, ch);
                        break;
                    }
                // string
                case '"':
                    {
                        tok = Token.CreateWithString(Token.TokenType.String, ReadString());
                        break;
                    }
                case '\'':
                    {
                        tok = Token.CreateWithString(Token.TokenType.String, ReadString());
                        break;
                    }
                // internal code
                case '@':
                    {
                        if (NextChar() == '`')
                        {
                            tok = Token.CreateWithString(Token.TokenType.Internal, ReadString());
                        } else
                        {
                            // handle error
                        }
                        break;
                    }
                // keywords or identifier
                default:
                    {
                        string buffer = "";
                        char peek = ch;
                        while (!IsStopFlag(peek))
                        {
                            buffer += peek;
                            peek = PeekChar();
                            if (IsStopFlag(peek))
                            {
                                break;
                            }
                            NextChar();
                        }

                        if (!string.IsNullOrEmpty(buffer.Trim()))
                        {
                            tok = new Token(GetTokenType(buffer), buffer);
                        }
                        break;
                    }
            }

            NextChar();
            return current = (tok == null ? Token.EOF : tok);
        }

        private string ReadString()
        {
            char flag = ch;
            NextChar();
            string buffer = "";
            while (true)
            {
                if (ch == flag)
                {
                    break;
                }
                buffer += ch == '\\' ? NextChar() : ch;
                NextChar();
            }

            return buffer;
        }
    }
}