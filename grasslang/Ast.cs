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
    public class BlockStatement : Statement
    {
        public List<Node> Body = new List<Node>();
        public BlockStatement()
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
        public static IdentifierExpression Void
        {
            get
            {
                return new IdentifierExpression(null, "void");
            }
        }
        public static IdentifierExpression Null
        {
            get
            {
                return new IdentifierExpression(null, "null");
            }
        }
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

    public class TextExpression : Expression
    {
        public string Literal = "";
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
    [DebuggerDisplay("PathExpression = \"{Literal}\"")]
    public class PathExpression : TextExpression
    {
        public List<Expression> Path = new List<Expression>();
    }
    [DebuggerDisplay("IdentifierExpression = \"{Literal}\"")]
    public class IdentifierExpression : TextExpression
    {
        public Token Token = null;

        public IdentifierExpression(Token token, string literal)
        {
            Token = token;
            Literal = literal;
        }
        
    }
    [DebuggerDisplay("CallExpression = \"{FunctionName.Literal}\"")]
    public class CallExpression : Expression
    {
        public TextExpression FunctionName;
        public Expression[] ArgsList;
        
        public CallExpression(TextExpression functionName, Expression[] argsList)
        {
            FunctionName = functionName;
            ArgsList = argsList;
        }

        public CallExpression()
        {
            
        }
    }


    [DebuggerDisplay("DefinitionExpression = \"{Name.Literal} : {Type.Literal}\"")]
    public class DefinitionExpression : Expression
    {
        public IdentifierExpression Name;
        public Expression Value = null;
        public TextExpression ObjType;
        public DefinitionExpression()
        {

        }
        public DefinitionExpression(IdentifierExpression Name, TextExpression Type, Expression Value = null)
        {
            this.Name = Name;
            this.ObjType = Type;
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
    public class FunctionLiteral : Expression
    {
        public IdentifierExpression FunctionName = null;
        public List<DefinitionExpression> Parameters = new List<DefinitionExpression>();
        public BlockStatement Body = null;
        public TextExpression ReturnType;
        public bool Anonymous = false;
    }
    public class AssignExpression : Expression
    {
        public TextExpression Left;
        public Expression Right;
    }
    public class SubscriptExpression : Expression
    {
        public Expression Body;
        public Expression Subscript;
    }
    public class InternalCode : Expression
    {
        public Token Token = null;
        public string Value = null;

        public InternalCode(Token token, string value)
        {
            Token = token;
            Value = value;
        }
    }
}