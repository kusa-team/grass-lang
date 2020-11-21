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
            {Token.TokenType.Identifier, Priority.Prefix }
        };

        public static Priority QueryPriority(Token.TokenType type)
        {
            if (PriorityMap.ContainsKey(type))
            {
                return PriorityMap[type];
            }
            return Priority.Lowest;
        }
        private LexerInterface lexer = null;

        private Token current
        {
            get
            {
                return lexer.CurrentToken();
            }
        }
        public AstParser(LexerInterface _lexer)
        {
            lexer = _lexer;
        }
        private Token PeekToken()
        {
            return lexer.PeekToken();
        }
        private Token NextToken()
        {
            return lexer.GetNextToken();
        }

        private Node GetNextNode()
        {
            return ParseStatement();
        }
        public Ast BuildAst()
        {
            Ast result = new Ast();
            NextToken();
            while (lexer.PeekToken().Type != Token.TokenType.Eof)
            {
                result.Root.Add(GetNextNode());
            }
            return result;
        }
        private Statement ParseStatement()
        {
            switch (current.Type)
            {
                case Token.TokenType.Let:
                    {
                        LetStatement letStatement = new LetStatement();
                        lexer.GetNextToken(); // eat 'let'
                        if (ParseExpression(Priority.Lowest) is DefinitionExpression definitionExpression)
                        {
                            letStatement.Definition = definitionExpression;
                        }
                        return letStatement;
                    }
                case Token.TokenType.Return:
                    {
                        ReturnStatement returnStatement = new ReturnStatement();
                        if (lexer.PeekToken().Type != Token.TokenType.Semicolon)
                        {
                            lexer.GetNextToken();
                            returnStatement.Value = ParseExpression(Priority.Lowest);
                        }
                        return returnStatement;
                    }
                case Token.TokenType.Function:
                    {
                        return parseFunctionLiteral();
                    }
            }
            return ParseExpressionStatement();
        }


        private FunctionStatement parseFunctionLiteral()
        {
            FunctionStatement function = new FunctionStatement();
            NextToken();
            // parse function name
            if (ParsePrefixExpression() is IdentifierExpression functionName)
            {
                function.FunctionName = functionName;
            }
            else
            {
                // handle error
                InfoHandler.PrintFormatedError(InfoHandler.ErrorType.FUNCTION_NAME_INVALID);
            }

            // parse function parameters
            if(NextToken().Type != Token.TokenType.LeftParen)
            {
                return null;
            }
            function.Parameters = parseFunctionParameters();

            // parse function return type
            if (current.Type == Token.TokenType.Colon)
            {
                NextToken();
                function.ReturnType = parseTextExpression();
            } else
            {
                function.ReturnType = Expression.Void;
            }

            if (PeekToken().Type != Token.TokenType.LeftBrace)
            {
                return null;
            }
            NextToken();
            // parse function body
            Block body = parseBlock();
            function.Body = body;
            return function;
        }
        private List<DefinitionExpression> parseFunctionParameters()
        {
            List<DefinitionExpression> result = new List<DefinitionExpression>();
            if (PeekToken().Type == Token.TokenType.RightParen)
            {
                NextToken();
                NextToken();
                return result;
            }
            
            do
            {
                NextToken();
                if (parseDefinitionExpression() is DefinitionExpression param)
                {
                    result.Add(param);
                } else
                {
                    return null;
                }
            } while (current.Type != Token.TokenType.RightParen);
            NextToken();
            return result;
        }



        private Block parseBlock()
        {
            return null;
        }


        private ExpressionStatement ParseExpressionStatement()
        {
            Expression expression = ParseExpression(Priority.Lowest);
            if (PeekToken().Type != Token.TokenType.Semicolon)
            {
                return null;
            }
            return new ExpressionStatement(expression);
        }
        private Expression ParseExpression(Priority priority)
        {
            Expression leftExpression = ParsePrefixExpression();
            if (leftExpression == null)
            {
                return null;
            }

            Token peek = lexer.PeekToken();
            while (peek.Type != Token.TokenType.Semicolon
                   && priority <= QueryPriority(peek.Type))
            {
                lexer.GetNextToken();
                Expression infix = ParseInfixExpression(leftExpression);
                if (infix == null)
                {
                    return leftExpression;
                }
                leftExpression = infix;
                peek = lexer.PeekToken();
            }


            return leftExpression;
        }
        private Expression ParsePrefixExpression()
        {
            switch (current.Type)
            {
                case Token.TokenType.Identifier:
                    {
                        return new IdentifierExpression(current, current.Literal);
                    }
                case Token.TokenType.Plus:
                    {
                        return ParsePrefixDefault();
                    }
                case Token.TokenType.Minus:
                    {
                        return ParsePrefixDefault();
                    }
                case Token.TokenType.String:
                    {
                        return new StringExpression(current, current.Literal);
                    }
                case Token.TokenType.Internal:
                    {
                        return new InternalCode(current, current.Literal);
                    }
                case Token.TokenType.True:
                    {
                        return new IdentifierExpression(current, current.Literal);
                    }
                case Token.TokenType.False:
                    {
                        return new IdentifierExpression(current, current.Literal);
                    }
            }

            return null;
        }
        private Expression HandleInfixExpression(Expression leftExpression)
        {
            InfixExpression expression = new InfixExpression();
            expression.LeftExpression = leftExpression;
            expression.Operator = current;
            lexer.GetNextToken();
            expression.RightExpression = ParseExpression(QueryPriority(expression.Operator.Type));
            return expression;
        }
        private Expression ParseInfixExpression(Expression leftExpression)
        {
            switch (PeekToken().Type)
            {
                case Token.TokenType.Plus:
                    {
                        return HandleInfixExpression(leftExpression);
                    }
                case Token.TokenType.Minus:
                    {
                        return HandleInfixExpression(leftExpression);
                    }
                case Token.TokenType.Asterisk:
                    {
                        return HandleInfixExpression(leftExpression);
                    }
                case Token.TokenType.Slash:
                    {
                        return HandleInfixExpression(leftExpression);
                    }
                case Token.TokenType.LeftParen:
                    {
                        // Parse Call
                        CallExpression callExpression = new CallExpression();
                        if (leftExpression is TextExpression TextExpression)
                        {
                            callExpression.FunctionName = TextExpression;
                        }
                        else
                        {
                            InfoHandler.PrintFormatedError(InfoHandler.ErrorType.FUNCTION_NAME_INVALID);
                            //handle error
                            break;
                        }
                        callExpression.ArgsList = ParseCallArgs(); // now is expression start.
                        lexer.GetNextToken(); // eat ')'
                        return callExpression;
                    }
                case Token.TokenType.Dot:
                    {
                        if (leftExpression is PathExpression lastLayer)
                        {
                            lexer.GetNextToken();
                            if (ParsePrefixExpression() is IdentifierExpression nextLayer)
                            {
                                lastLayer.Path.Add(nextLayer);
                                lastLayer.Literal += "." + nextLayer.Literal;
                                return lastLayer;
                            }
                            else
                            {
                                // handle error
                            }
                        }
                        PathExpression PathExpression = new PathExpression();
                        if (leftExpression is IdentifierExpression identifierExpression)
                        {
                            PathExpression.Path.Add(identifierExpression);
                            lexer.GetNextToken();
                            if (ParsePrefixExpression() is IdentifierExpression nextLayer)
                            {
                                PathExpression.Path.Add(nextLayer);
                                PathExpression.Literal = identifierExpression.Literal
                                    + "." + nextLayer.Literal;
                            }
                            else
                            {
                                // handle error
                            }
                        }
                        else
                        {
                            // handle error
                        }
                        return PathExpression;
                    }
                case Token.TokenType.Colon:
                    {
                        
                        return parseDefinitionExpression();
                    }
                case Token.TokenType.Assign:
                    {
                        AssignExpression assignExpression = new AssignExpression();
                        if (leftExpression is TextExpression TextExpression)
                        {
                            assignExpression.Left = TextExpression;
                        }
                        else
                        {
                            // handle error
                            throw new Exception();
                        }
                        lexer.GetNextToken();
                        assignExpression.Right = ParseExpression(Priority.Assign);
                        return assignExpression;
                    }
                case Token.TokenType.LeftBrack:
                    {
                        SubscriptExpression subscriptExpression = new SubscriptExpression();
                        subscriptExpression.Body = leftExpression;
                        lexer.GetNextToken();
                        subscriptExpression.Subscript = ParseExpression(Priority.Index);
                        if (lexer.GetNextToken().Type != Token.TokenType.RightBrack)
                        {
                            // handle error
                            throw new Exception();
                        }
                        return subscriptExpression;
                    }
            }

            return null;
        }
        private IdentifierExpression parseIdentifierExpression()
        {
            if(current.Type != Token.TokenType.Identifier)
            {
                return null;
            }
            return new IdentifierExpression(current, current.Literal);
        }
        private DefinitionExpression parseDefinitionExpression()
        {
            Expression left = parseIdentifierExpression();
            DefinitionExpression definition = new DefinitionExpression();
            if (left is IdentifierExpression Name)
            {
                definition.Name = Name;
            }
            else
            {
                return null;
            }
            
            if(PeekToken().Type != Token.TokenType.Colon)
            {
                return null;
            }
            NextToken();
            NextToken();
            if (parseTextExpression() is TextExpression TextExpression)
            {
                definition.ObjType = TextExpression;
            }
            else
            {
                return null;
            }
            // parse value
            if (PeekToken().Type == Token.TokenType.Assign)
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
            NextToken();
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
            if (PeekToken().Type == Token.TokenType.Dot)
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
                while (PeekToken().Type == Token.TokenType.Dot)
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
            peek = lexer.PeekToken(); // now is '('
            while (peek.Type != Token.TokenType.RightParen)
            {
                lexer.GetNextToken();
                expressions.Add(ParseExpression(Priority.Lowest));
                peek = lexer.PeekToken();
                if (peek.Type == Token.TokenType.Comma)
                {
                    lexer.GetNextToken();
                }
            }
            return expressions.ToArray();
        }
        private Expression ParsePrefixDefault()
        {
            PrefixExpression prefixExpression = new PrefixExpression();
            prefixExpression.Token = current;
            lexer.GetNextToken();
            prefixExpression.Expression = ParseExpression(Priority.Prefix);
            return prefixExpression;
        }

    }
}
