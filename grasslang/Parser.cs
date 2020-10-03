using System;
using System.Collections.Generic;

namespace grasslang
{
    /*
     * 语法规定：
     * 1.表达式必须有返回值
     * 2.大语句解析结束后，必须将pos移到分号
     */
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
                        letStatement.VarName = new IdentifierExpression(current, current.Literal);
                        if (lexer.PeekToken().Type == Token.TokenType.ASSIGN)
                        {
                            lexer.GetNextToken(); // eat varname
                            lexer.GetNextToken(); // eat '='
                            letStatement.Value = ParseExpression(Priority.Lowest);
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
                        if(ParseExpression(Priority.Lowest) is IdentifierExpression functionName)
                        {
                            functionStatement.FunctionName = functionName;
                        } else
                        {
                            // error
                        }

                        return null;
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
            Expression leftExpression = ParsePrefixExpression();
            if (leftExpression == null)
            {
                // handle error
            }

            Token peek = lexer.PeekToken();
            while (peek.Type != stopTag
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
        private Expression ParseInfixExpression(Expression leftExpression)
        {
            switch (current.Type)
            {
                case Token.TokenType.PLUS:
                    {
                        // Parse Plus
                        InfixExpression expression = new InfixExpression();
                        expression.LeftExpression = leftExpression;
                        expression.Operator = current;
                        lexer.GetNextToken();
                        expression.RightExpression = ParseExpression(QueryPriority(expression.Operator.Type));
                        return expression;
                    }
                case Token.TokenType.LPAREN:
                    {
                        // Parse Call
                        CallExpression callExpression = new CallExpression();
                        if (leftExpression is IdentifierExpression identifierExpression)
                        {
                            callExpression.FunctionName = identifierExpression;
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
            }

            return null;
        }
    }
}