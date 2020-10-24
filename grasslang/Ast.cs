using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace grasslang
{
    public class Ast
    {
        public List<Node> Root = new List<Node>();
    }

    public class Node
    {
        public string Type = "";
    }

    public class Statement : Node
    {

    }
    public class Block : Node
    {
        public List<Node> body = new List<Node>();
        public Block()
        {

        }
    }
    public class ExpressionStatement : Statement
    {
        public Expression Expression = null;

        public ExpressionStatement(Expression expression)
        {
            Expression = expression;
        }
    }

    public class Expression : Node
    {
        
    }
    public class PrefixExpression : Expression
    {
        public Token Token = null;
        public Expression Expression = null;

        public PrefixExpression(Token token, Expression expression)
        {
            Token = token;
            Expression = expression;
        }

        public PrefixExpression()
        {
            
        }
    }
    
    public class InfixExpression : Expression
    {
        public Token Operator = null;
        public Expression LeftExpression = null;
        public Expression RightExpression = null;
        public InfixExpression(Token token, Expression leftExpression, Expression rightExpression)
        {
            Operator = token;
            LeftExpression = leftExpression;
            RightExpression = rightExpression;
        }

        public InfixExpression()
        {
            
        }
    }
    [DebuggerDisplay("StringExpression = \"{Value}\"")]
    public class StringExpression : Expression
    {
        public Token Token = null;
        public string Value = null;

        public StringExpression(Token token, string value)
        {
            Token = token;
            Value = value;
        }
    }
    [DebuggerDisplay("IdentifierExpression = \"{Literal}\"")]
    public class IdentifierExpression : Expression
    {
        public Token Token = null;
        public string Literal = null;

        public IdentifierExpression(Token token, string literal)
        {
            Token = token;
            Literal = literal;
        }
        
    }
    [DebuggerDisplay("CallExpression = \"{FunctionName.Literal}\"")]
    public class CallExpression : Expression
    {
        public IdentifierExpression FunctionName;
        public Expression[] ArgsList;

        public CallExpression(IdentifierExpression functionName, Expression[] argsList)
        {
            FunctionName = functionName;
            ArgsList = argsList;
        }

        public CallExpression()
        {
            
        }
    }
    [DebuggerDisplay("ChildrenExpression = \"{Literal}\"")]
    public class ChildrenExpression : Expression
    {
        public string Literal;
        public List<IdentifierExpression> Layers = new List<IdentifierExpression>();
    }

    [DebuggerDisplay("DefinitionExpression = \"{Name.Literal} : {Type.Literal}\"")]
    public class DefinitionExpression : Expression
    {
        public IdentifierExpression Name;
        public Expression Value = null;
        public ChildrenExpression Type;
        public DefinitionExpression()
        {

        }
        public DefinitionExpression(IdentifierExpression Name, ChildrenExpression Type, Expression Value = null)
        {
            this.Name = Name;
            this.Type = Type;
            this.Value = Value;
        }
    }
    public class LetStatement : Statement
    {
        public DefinitionExpression Definition;

        public LetStatement()
        {
            
        }
        public LetStatement(DefinitionExpression Definition)
        {
            this.Definition = Definition;
        }
    }
    
    public class ReturnStatement : Statement
    {
        public Expression Value = null;

        public ReturnStatement()
        {
            
        }
        public ReturnStatement(Expression value)
        {
            this.Value = value;
        }
    }
    public class FunctionStatement : Statement
    {
        public IdentifierExpression FunctionName = null;
        public List<Expression> ArgumentList = new List<Expression>();
        public Block body = null;
        public FunctionStatement()
        {

        }
    }
}