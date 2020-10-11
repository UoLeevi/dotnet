using System;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace DotNetApp.Expressions
{
    public partial class JsonPathAst
    {
        public class PropertySelector : ExpressionNode
        {
            public override NodeType NodeType => JsonPathAst.NodeType.PropertySelector;

            internal static PropertySelector Parse(ref ReadOnlySpan<char> expr)
            {
                PropertySelector node = new PropertySelector();

                if (expr[0] == '.')
                {
                    expr = expr.Slice(1);

                    if (expr.IsEmpty) throw new JsonPathInvalidSyntaxException(expr);

                    if (!char.IsLetter(expr[0]) && expr[0] != '_') throw new JsonPathInvalidSyntaxException(expr);

                    int i = 1;
                    for (; i < expr.Length; ++i)
                    {
                        if (!char.IsLetterOrDigit(expr[i]) && expr[i] != '_') break;
                    }

                    node.PropertyName = expr.Slice(0, i).ToString();
                    expr = expr.Slice(i);
                    return node;
                }
                else
                {
                    expr = expr.Slice(2);
                    int i = expr.IndexOf("']".AsSpan());

                    if (i == -1) throw new JsonPathInvalidSyntaxException(expr);

                    node.PropertyName = expr.Slice(0, i).ToString();
                    expr = expr.Slice(i + 2);
                    return node;
                }
            }

            internal static bool Peek(ReadOnlySpan<char> expr)
                => expr.Length > 2
                && expr[1] != '*'
                && (
                    (expr[0] == '.' && expr[1] != '.') ||
                    (expr[0] == '[' && expr[1] == '\'')
                );                

            public string PropertyName;

            public override Expression ToExpression(Expression expression)
                => Expression.PropertyOrField(expression, PropertyName);
        }
    }
}
