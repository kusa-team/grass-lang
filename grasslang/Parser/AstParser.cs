using System;
using System.Collections.Generic;

namespace grasslang
{
    public class AstParser
    {
        public enum Priority
        {
            Lowest = 0,
            Assign = 1, // =
            Equals = 2, // ==, !=
            LessGreater = 3, // < ,>

            Index = 4, // array[0], map[0]
            Sum = 5, //+,-
            Product = 6,//*,/
            Prefix = 7, // !,-,+
            Call = 8, // func() 
        }
        public static Dictionary<Token.TokenType, Priority> PriorityMap = new Dictionary<Token.TokenType, Priority>
        {
            {Token.TokenType.Plus, Priority.Sum},
            {Token.TokenType.Minus, Priority.Sum},
            {Token.TokenType.Asterisk, Priority.Product},
            {Token.TokenType.Slash, Priority.Product},

            {Token.TokenType.LeftParen, Priority.Call},
            {Token.TokenType.LeftBrack, Priority.Index},
            {Token.TokenType.Identifier, Priority.Prefix },
            {Token.TokenType.LeftBrace, Priority.Lowest }
        };

        public static Priority QueryPriority(Token.TokenType type)
        {
            if (PriorityMap.ContainsKey(type))
            {
                return PriorityMap[type];
            }
            return Priority.Lowest;
        }
        public LexerInterface Lexer = null;

        private Token current
        {
            get
            {
                return Lexer.CurrentToken();
            }
        }
        private Token peek
        {
            get
            {
                return Lexer.PeekToken();
            }
        }
        private Token NextToken()
        {
            return Lexer.GetNextToken();
        }
        public Ast BuildAst()
        {
            Ast result = new Ast();
            NextToken();
            while (Lexer.PeekToken().Type != Token.TokenType.Eof)
            {
                Statement statement = ParseStatement();
                if (statement != null)
                {
                    result.Root.Add(statement);
                }
            }
            return result;
        }

        private Dictionary<Token.TokenType, Func<Expression>> prefixParserMap = new Dictionary<Token.TokenType, Func<Expression>>();
        private Func<Expression> getPrefixParserFunction(Token.TokenType type)
        {
            try
            {
                return prefixParserMap[type];
            } catch
            {
                return null;
            }
        }
        private Dictionary<Token.TokenType, Func<Expression, Expression>> infixParserMap = new Dictionary<Token.TokenType, Func<Expression, Expression>>();
        private Func<Expression, Expression> getInfixParserFunction(Token.TokenType type)
        {
            try
            {
                return infixParserMap[type];
            }
            catch
            {
                return null;
            }
        }
        public void InitParser()
        {
            // prefix
            prefixParserMap[Token.TokenType.Function] = parseFunctionLiteral;
            prefixParserMap[Token.TokenType.Identifier] = parseIdentifierExpression;

            // infix
            infixParserMap[Token.TokenType.Colon] = parseDefinitionExpression;
            infixParserMap[Token.TokenType.Dot] = parsePathExpression;
        }


        private Statement ParseStatement()
        {
            switch (current.Type)
            {
                case Token.TokenType.Let:
                    {
                        return null;
                    }
                case Token.TokenType.Return:
                    {
                        return null;
                    }
            }
            return ParseExpressionStatement();
        }


        private Expression parseFunctionLiteral()
        {
            FunctionLiteral function = new FunctionLiteral();
            NextToken();


            // parse function name
            if (parseIdentifierExpression() is IdentifierExpression functionName)
            {
                function.FunctionName = functionName;
                NextToken();
            } else
            {
                function.Anonymous = true;
            }

            // parse function parameters
            if(current.Type != Token.TokenType.LeftParen)
            {
                return null;
            }
            function.Parameters = parseFunctionParameters();

            // parse function return type
            if (peek.Type == Token.TokenType.LeftBrace)
            {
                function.ReturnType = Expression.Void;
                NextToken();
            } else if (peek.Type == Token.TokenType.Colon)
            {
                NextToken();
                NextToken();
                if (ParseExpression(Priority.Lowest) is TextExpression type)
                {
                    function.ReturnType = type;
                }
            }
            NextToken();

            // parse function body
            if (parseBlockStatement() is BlockStatement block)
            {
                function.Body = block;
            }
            return function;
        }
        private List<DefinitionExpression> parseFunctionParameters()
        {
            List<DefinitionExpression> result = new List<DefinitionExpression>();
            if (peek.Type == Token.TokenType.RightParen)
            {
                NextToken();
                return result;
            }
            
            do
            {
                NextToken();
                if (ParseExpression(Priority.Lowest) is DefinitionExpression param)
                {
                    result.Add(param);
                    NextToken();
                } else
                {
                    return null;
                }
            } while (current.Type != Token.TokenType.RightParen);
            return result;
        }



        private Statement parseBlockStatement()
        {
            return null;
        }


        private ExpressionStatement ParseExpressionStatement()
        {
            Expression expression = ParseExpression(Priority.Lowest);
            if (peek.Type != Token.TokenType.Semicolon)
            {
                return null;
            }
            return new ExpressionStatement(expression);
        }
        private Expression ParseExpression(Priority priority)
        {
            Func<Expression> prefixFunc = getPrefixParserFunction(current.Type);
            if(prefixFunc == null)
            {
                return null;
            }
            Expression left = prefixFunc();

            while (peek.Type != Token.TokenType.Semicolon
                   && priority <= QueryPriority(peek.Type))
            {
                Func<Expression, Expression> infixFunc = getInfixParserFunction(peek.Type);
                if(infixFunc == null)
                {
                    return left;
                }
                NextToken();
                left = infixFunc(left);
                
            }

            return left;
        }
        private Expression HandleInfixExpression(Expression leftExpression)
        {
            InfixExpression expression = new InfixExpression();
            expression.LeftExpression = leftExpression;
            expression.Operator = current;
            Lexer.GetNextToken();
            expression.RightExpression = ParseExpression(QueryPriority(expression.Operator.Type));
            return expression;
        }
        private IdentifierExpression parseIdentifierExpression()
        {
            if(current.Type != Token.TokenType.Identifier)
            {
                return null;
            }
            return new IdentifierExpression(current, current.Literal);
        }
        private PathExpression parsePathExpression(Expression left)
        {
            PathExpression path;
            if (left is PathExpression leftPath)
            {
                path = leftPath;
            } else
            {
                path = new PathExpression();
                path.Path.Add(left);
            }
            NextToken();
            path.Path.Add(ParseExpression(Priority.Index));
            return path;
        }

        private DefinitionExpression parseDefinitionExpression(Expression left)
        {
            DefinitionExpression definition = new DefinitionExpression();
            if (left is IdentifierExpression Name)
            {
                definition.Name = Name;
            } else
            {
                return null;
            }

            NextToken();
            if (ParseExpression(Priority.Lowest) is TextExpression type)
            {
                definition.ObjType = type;
            }
            else
            {
                return null;
            }
            // parse value
            if (peek.Type == Token.TokenType.Assign)
            {
                NextToken();
                NextToken();
                Expression value = ParseExpression(Priority.Lowest);
                if (value == null)
                {
                    return null;
                }
                else
                {
                    definition.Value = value;
                }
            }
            return definition;
        }
        private TextExpression parseTextExpression(Expression leftExpression = null)
        {
            TextExpression text = null;
            Expression left = leftExpression != null ? leftExpression : ParseExpression(Priority.Index);
            if (left == null)
            {
                return null;
            }
            if (peek.Type == Token.TokenType.Dot)
            {
                PathExpression path;
                if (left is PathExpression leftPath)
                {
                    path = leftPath;
                }
                else
                {
                    path = new PathExpression();
                    path.Path.Add(left);
                }
                while (peek.Type == Token.TokenType.Dot)
                {
                    NextToken();
                    NextToken();
                    left = ParseExpression(Priority.Index);
                    path.Path.Add(left);
                }
                text = path;
            } else if (left is IdentifierExpression identifier)
            {
                text = identifier;
            }
            return text;
        }
        private Expression[] ParseCallArgs()
        {
            Token peek;
            List<Expression> expressions = new List<Expression>();
            peek = Lexer.PeekToken(); // now is '('
            while (peek.Type != Token.TokenType.RightParen)
            {
                Lexer.GetNextToken();
                expressions.Add(ParseExpression(Priority.Lowest));
                peek = Lexer.PeekToken();
                if (peek.Type == Token.TokenType.Comma)
                {
                    Lexer.GetNextToken();
                }
            }
            return expressions.ToArray();
        }
        private Expression ParsePrefixDefault()
        {
            PrefixExpression prefixExpression = new PrefixExpression();
            prefixExpression.Token = current;
            Lexer.GetNextToken();
            prefixExpression.Expression = ParseExpression(Priority.Prefix);
            return prefixExpression;
        }

    }
}
