using System;
using System.Collections.Generic;

namespace grasslang
{
    public class Parser
    {
        public enum Priority
        {
            Lowest = 0,
            Assign = 1, // =
            Equals = 2, // ==, !=
            LessGreater = 3, // < ,>
            Sum = 4, //+,-
            Product = 5,//*,/
            Prefix = 6, // !,-,+
            Call = 7, // func() 
            Index = 8, // array[0], map[0]
            High = 9
        }
        public static Dictionary<Token.TokenType, Priority> PriorityMap = new Dictionary<Token.TokenType, Priority>
        {
            {Token.TokenType.PLUS, Priority.Sum},
            {Token.TokenType.MINUS, Priority.Sum},
            {Token.TokenType.ASTERISK, Priority.Product},
            {Token.TokenType.SLASH, Priority.Product},

            {Token.TokenType.LPAREN, Priority.Call}
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
        public Parser(LexerInterface _lexer)
        {
            lexer = _lexer;
        }

        private Node GetNextNode()
        {
            return ParseStatement();
        }
        public Ast BuildAst()
        {
            Ast result = new Ast();
            while (lexer.PeekToken().Type != Token.TokenType.EOF)
            {
                lexer.GetNextToken();
                result.Root.Add(GetNextNode());
                lexer.GetNextToken();
            }
            return result;
        }



        private Statement ParseStatement()
        {
            switch (current.Type)
            {
                case Token.TokenType.LET:
                    {
                        LetStatement letStatement = new LetStatement();
                        lexer.GetNextToken(); // eat 'let'
                        if (ParseExpression(Priority.Lowest) is DefinitionExpression definitionExpression)
                        {
                            letStatement.Definition = definitionExpression;
                        }
                        return letStatement;
                    }
                case Token.TokenType.RETURN:
                    {
                        ReturnStatement returnStatement = new ReturnStatement();
                        if (lexer.PeekToken().Type != Token.TokenType.SEMICOLON)
                        {
                            lexer.GetNextToken();
                            returnStatement.Value = ParseExpression(Priority.Lowest);
                        }
                        return returnStatement;
                    }
                case Token.TokenType.FUNCTION:
                    {
                        FunctionStatement functionStatement = new FunctionStatement();
                        lexer.GetNextToken();
                        // parse function name
                        if (ParsePrefixExpression() is IdentifierExpression functionName)
                        {
                            functionStatement.FunctionName = functionName;
                        } else
                        {
                            // handle error
                        }
                        // parse function arguments
                        lexer.GetNextToken();
                        if (lexer.PeekToken().Type == Token.TokenType.IDENTIFER)
                        {
                            while (lexer.PeekToken().Type == Token.TokenType.IDENTIFER)
                            {
                                lexer.GetNextToken();
                                if (ParseExpression(Priority.Lowest, Token.TokenType.COMMA)
                                    is DefinitionExpression definition)
                                {
                                    functionStatement.ArgumentList.Add(definition);
                                    lexer.GetNextToken();
                                }
                                else
                                {
                                    // handle error
                                }
                            }
                        } else
                        {
                            lexer.GetNextToken(); // skip ')'
                        }
                        lexer.GetNextToken();
                        // parse function return type
                        if (current.Type != Token.TokenType.COLON)
                        {
                            functionStatement.ReturnType = Expression.Void;
                        } else
                        {
                            if(lexer.PeekToken().Type != Token.TokenType.IDENTIFER)
                            {
                                // handle error
                            }
                            lexer.GetNextToken();
                            if(ParseExpression(Priority.Lowest, Token.TokenType.LBRACE) is LiteralExpression returnType)
                            {
                                functionStatement.ReturnType = returnType;
                                lexer.GetNextToken();
                            }
                        }
                        // parse function body
                        if (current.Type != Token.TokenType.LBRACE)
                        {
                            // handle error
                        }
                        Block body = new Block();
                        while(lexer.PeekToken().Type != Token.TokenType.RBRACE)
                        {
                            lexer.GetNextToken();
                            body.body.Add(GetNextNode());
                            lexer.GetNextToken();
                        }
                        functionStatement.body = body;
                        return functionStatement;
                    }
            }
            return ParseExpressionStatement();
        }

        private ExpressionStatement ParseExpressionStatement()
        {
            return new ExpressionStatement(ParseExpression(Priority.Lowest));
        }
        private Expression ParseExpression(Priority priority, Token.TokenType stopTag = Token.TokenType.SEMICOLON)
        {
            return ParseExpression(priority, (type) =>
            {
                return type != stopTag;
            });
        }
        private Expression ParseExpression(Priority priority, Func<Token.TokenType, bool> stopFunc)
        {
            Expression leftExpression = ParsePrefixExpression();
            if (leftExpression == null)
            {
                // handle error
            }

            Token peek = lexer.PeekToken();
            while (stopFunc(peek.Type)
                   && peek.Type != Token.TokenType.EOF
                   && peek.Type != Token.TokenType.RPAREN
                   && priority <= QueryPriority(peek.Type))
            {
                // handle infix
                lexer.GetNextToken();
                leftExpression = ParseInfixExpression(leftExpression);

                peek = lexer.PeekToken();
            }


            return leftExpression;
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
            switch (current.Type)
            {
                case Token.TokenType.PLUS:
                    {
                        return HandleInfixExpression(leftExpression);
                    }
                case Token.TokenType.MINUS:
                    {
                        return HandleInfixExpression(leftExpression);
                    }
                case Token.TokenType.ASTERISK:
                    {
                        return HandleInfixExpression(leftExpression);
                    }
                case Token.TokenType.SLASH:
                    {
                        return HandleInfixExpression(leftExpression);
                    }
                case Token.TokenType.LPAREN:
                    {
                        // Parse Call
                        CallExpression callExpression = new CallExpression();
                        if (leftExpression is LiteralExpression literalExpression)
                        {
                            callExpression.FunctionName = literalExpression;
                        }
                        else
                        {
                            //handle error
                            break;
                        }
                        callExpression.ArgsList = ParseCallArgs(); // now is expression start.
                        lexer.GetNextToken(); // eat ')'
                        return callExpression;
                    }
                case Token.TokenType.DOT:
                    {
                        if (leftExpression is ChildrenExpression lastLayer)
                        {
                            lexer.GetNextToken();
                            if(ParsePrefixExpression() is IdentifierExpression nextLayer)
                            {
                                lastLayer.Layers.Add(nextLayer);
                                lastLayer.Literal += "." + nextLayer.Literal;
                                return lastLayer;
                            } else
                            {
                                // handle error
                            }
                        }
                        ChildrenExpression childrenExpression = new ChildrenExpression();
                        if (leftExpression is IdentifierExpression identifierExpression)
                        {
                            childrenExpression.Layers.Add(identifierExpression);
                            lexer.GetNextToken();
                            if (ParsePrefixExpression() is IdentifierExpression nextLayer)
                            {
                                childrenExpression.Layers.Add(nextLayer);
                                childrenExpression.Literal = identifierExpression.Literal
                                    +  "." + nextLayer.Literal;
                            }
                            else
                            {
                                // handle error
                            }
                        } else
                        {
                            // handle error
                        }
                        return childrenExpression;
                    }
                case Token.TokenType.COLON:
                    {
                        DefinitionExpression definition = new DefinitionExpression();
                        if(leftExpression is IdentifierExpression Name)
                        {
                            definition.Name = Name;
                        } else
                        {
                            // handle error
                        }
                        lexer.GetNextToken();
                        // parse type
                        if(ParseExpression(Priority.Lowest, (type) =>
                        {
                            return type == Token.TokenType.IDENTIFER || type == Token.TokenType.DOT;
                        }) is LiteralExpression literalExpression)
                        {
                            definition.ObjType = literalExpression;
                        } else
                        {
                            // handle error
                        }
                        // parse value
                        if(lexer.PeekToken().Type == Token.TokenType.ASSIGN)
                        {
                            lexer.GetNextToken();
                            lexer.GetNextToken();
                            Expression value = ParseExpression(Priority.Lowest);
                            if(value == null)
                            {
                                // handle error
                            } else
                            {
                                definition.Value = value;
                            }
                        }

                        return definition;
                    }
            }

            return null;
        }

        private Expression[] ParseCallArgs()
        {
            Token peek;
            List<Expression> expressions = new List<Expression>();
            peek = lexer.PeekToken(); // now is '('
            while (peek.Type != Token.TokenType.RPAREN)
            {
                lexer.GetNextToken();
                expressions.Add(ParseExpression(Priority.Lowest, Token.TokenType.COMMA));
                peek = lexer.PeekToken();
                if (peek.Type == Token.TokenType.COMMA)
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
        private Expression ParsePrefixExpression()
        {
            switch (current.Type)
            {
                case Token.TokenType.IDENTIFER:
                    {
                        return new IdentifierExpression(current, current.Literal);
                    }
                case Token.TokenType.PLUS:
                    {
                        return ParsePrefixDefault();
                    }
                case Token.TokenType.MINUS:
                    {
                        return ParsePrefixDefault();
                    }
                case Token.TokenType.STRING:
                    {
                        return new StringExpression(current, current.Literal);
                    }
                case Token.TokenType.INTERNAL:
                    {
                        return new InternalCode(current, current.Literal);
                    }
            }

            return null;
        }
    }
}