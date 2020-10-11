using System;
using System.Linq.Expressions;

namespace DotNetApp.Expressions
{
    public partial class JsonPathAst
    {
        public class ItemSelector : ExpressionNode
        {
            public override NodeType NodeType => JsonPathAst.NodeType.ItemSelector;

            internal static ItemSelector Parse(ref ReadOnlySpan<char> expr)
            {
                expr = expr.Slice(1);

                int nesting = 1;
                int i = 0;
                for (; i < expr.Length && nesting != 0; ++i)
                {
                    if (expr[i] == '[') ++nesting;
                    else if (expr[i] == ']') --nesting;
                }

                if (nesting != 0) throw new JsonPathInvalidSyntaxException(expr);

                ReadOnlyMemory<char> nodeExprMem = expr.Slice(0, i).ToArray();
                expr = expr.Slice(i);

                return new ItemSelector
                {
                    node = new Lazy<Node>(() =>
                    {
                        var nodeExpr = nodeExprMem.Span;

                        foreach (NodeType nodeType in AllowedChildren)
                        {
                            if (nodeType.Peek(nodeExpr))
                            {
                                return nodeType.Parse(ref nodeExpr);
                            }
                        }

                        throw new JsonPathInvalidSyntaxException(nodeExpr);
                    })
                };
            }

            internal static bool Peek(ReadOnlySpan<char> expr)
                => expr.Length > 2
                && expr[0] == '['
                && expr[1] != '\'';

            private Lazy<Node> node;
            public Node Node => node.Value;

            public override Expression ToExpression(Expression expression) => throw new NotImplementedException();

            private static readonly NodeType[] AllowedChildren = new[] { NodeType.Wildcard, NodeType.NthIndex, NodeType.MultipleIndexes, NodeType.Range, NodeType.FilterExpression, NodeType.ScriptExpression };
        }
    }
}
