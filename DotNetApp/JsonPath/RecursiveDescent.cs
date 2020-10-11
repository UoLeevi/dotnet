using System;
using System.Linq.Expressions;

namespace DotNetApp.Expressions
{
    public partial class JsonPathAst
    {
        public class RecursiveDescent : ExpressionNode
        {
            public override NodeType NodeType => JsonPathAst.NodeType.RecursiveDescent;

            internal static RecursiveDescent Parse(ref ReadOnlySpan<char> expr)
            {
                expr = expr.Slice(2);

                return new RecursiveDescent
                {
                    PropertyAccessor = PropertySelector.Parse(ref expr)
                };
            }

            internal static bool Peek(ReadOnlySpan<char> expr)
                => expr.Length > 2
                && expr[0] == '.'
                && expr[1] == '.';

            public PropertySelector PropertyAccessor;
            
            public override Expression ToExpression(Expression expression) => throw new NotImplementedException();
        }
    }
}
