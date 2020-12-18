using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
namespace grasslang
{
    public class Ast
    {
        public List<Node> Root = new List<Node>();
    }
    public class Node : ICloneable
    {
        public string Type = "";
        public object Clone()
        {
            return MemberwiseClone();
        }
    }

    public class Statement : Node
    {

    }
    public class BlockStatement : Statement
    {
        public List<Node> Body = new List<Node>();
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
        public Expression Left = null;
        public Expression Right = null;
        public InfixExpression(Token token, Expression leftExpression, Expression rightExpression)
        {
            Operator = token;
            Left = leftExpression;
            Right = rightExpression;
        }

        public InfixExpression()
        {
            
        }
    }

    public class TextExpression : Expression
    {
        public string Literal = "";
    }
    [DebuggerDisplay("StringLiteral = \"{Value}\"")]
    public class StringLiteral : Expression
    {
        public Token Token = null;
        public string Value = null;

        public StringLiteral(Token token, string value)
        {
            Token = token;
            Value = value;
        }
    }
    [DebuggerDisplay("PathExpression = \"{Literal}\"")]
    public class PathExpression : TextExpression
    {
        public List<Expression> Path = new List<Expression>();

        // 生成新的PathExpression并剪裁其Path属性
        public PathExpression SubPath(int start, int length = -1)
        {
            PathExpression nextPathExpression = Clone() as PathExpression;
            List<Expression> nextPath = nextPathExpression.Path;
            if(length == -1)
            {
                length = nextPath.Count - start;
            }
            nextPath = nextPath.GetRange(start, length);
            nextPathExpression.Path = nextPath;
            return nextPathExpression;
        }
        public int Length
        {
            get
            {
                return Path.Count;
            }
        }
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
        public IdentifierExpression Function;
        public List<Expression> Parameters;
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
    public class IfExpression : Expression
    {
        public Expression Condition;
        public BlockStatement Consequence;
        public BlockStatement Alternative;
    }
    public class WhileExpression : Expression
    {
        public Expression Condition;
        public BlockStatement Consequence;
    }
    public class LoopExpression : Expression
    {
        public BlockStatement Process;
    }
    [DebuggerDisplay("NumberLiteral = {Value}")]
    public class NumberLiteral : Expression
    {
        public Token Token;
        public string Value;

        public NumberLiteral(Token token, string value)
        {
            Token = token;
            Value = value;
        }
    }
}