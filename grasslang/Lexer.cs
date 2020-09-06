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
            ASSIGN, // =
            NOT, // !
            
            PLUS, // +
            MINUS, // -
            ASTERISK, // *
            SLASH, // /
            
            LT, // <
            GT, // >
            
            EQ, // ==
            NOT_EQ, // !=
            
            COMMA, // ,
            SEMICOLON, // ;
            DOT, // .
            
            LPAREN, // (
            RPAREN, // )
            LBRACE, // {
            RBRACE, // }
            
            FUNCTION,
            LET,
            TRUE,
            FALSE,
            IF,
            ELSE,
            RETURN,
            
            EOF,
            IDENTIFER,
            STRING,
            NUMBER
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
                return Token.Create(TokenType.EOF, '\x00');
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
            {"fn", Token.TokenType.FUNCTION},
            {"let", Token.TokenType.LET},
            {"true", Token.TokenType.TRUE},
            {"false", Token.TokenType.FALSE},
            {"if", Token.TokenType.IF},
            {"else", Token.TokenType.ELSE},
            {"return", Token.TokenType.RETURN}
        };
        public static Token.TokenType GetTokenType(string literal)
        {
            Token.TokenType type = Token.TokenType.IDENTIFER;
            if (KeyworDictionary.ContainsKey(literal))
            {
                type = KeyworDictionary[literal];
            } else if (IsNumberString(literal))
            {
                type = Token.TokenType.NUMBER;
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
            return IsWhitespace(ch) || "=!+-*/<>,;{}().".IndexOf(ch) != -1 || ch == '\x00';
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
                        tok = Token.CreateWithString(Token.TokenType.EQ, "==");
                        NextChar(); // eat second '='
                    }
                    else
                    {
                        tok = Token.Create(Token.TokenType.ASSIGN, ch);
                    }
                    break;
                }
                case '!':
                {
                    if (PeekChar() == '=') // is '!' or '!='
                    {
                        tok = Token.CreateWithString(Token.TokenType.NOT_EQ, "!=");
                        NextChar(); // eat second '='
                    }
                    else
                    {
                        tok = Token.Create(Token.TokenType.NOT, ch);
                    }
                    break;
                }

                
                // + - * /
                case '+':
                {
                    tok = Token.Create(Token.TokenType.PLUS, ch);
                    break;
                }
                case '-':
                {
                    tok = Token.Create(Token.TokenType.MINUS, ch);
                    break;
                }
                case '*':
                {
                    tok = Token.Create(Token.TokenType.ASTERISK, ch);
                    break;
                }
                case '/':
                {
                    tok = Token.Create(Token.TokenType.SLASH, ch);
                    break;
                }
                
                
                // < >
                case '<':
                {
                    tok = Token.Create(Token.TokenType.LT, ch);
                    break;
                }
                case '>':
                {
                    tok = Token.Create(Token.TokenType.GT, ch);
                    break;
                }
                
                
                // , ; .
                case ',':
                {
                    tok = Token.Create(Token.TokenType.COMMA, ch);
                    break;
                }
                case ';':
                {
                    tok = Token.Create(Token.TokenType.SEMICOLON, ch);
                    break;
                }
                case '.':
                {
                    tok = Token.Create(Token.TokenType.DOT, ch);
                    break;
                }
                
                
                // ( ) { }
                case '(':
                {
                    tok = Token.Create(Token.TokenType.LPAREN, ch);
                    break;
                }
                case ')':
                {
                    tok = Token.Create(Token.TokenType.RPAREN, ch);
                    break;
                }
                case '{':
                {
                    tok = Token.Create(Token.TokenType.LBRACE, ch);
                    break;
                }
                case '}':
                {
                    tok = Token.Create(Token.TokenType.RBRACE, ch);
                    break;
                }

                // string
                case '"':
                {
                    tok = Token.CreateWithString(Token.TokenType.STRING, ReadString());
                    break;
                }
                case '\'':
                {
                    tok = Token.CreateWithString(Token.TokenType.STRING, ReadString());
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