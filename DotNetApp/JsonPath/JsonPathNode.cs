using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using DotNetApp.Collections;

namespace DotNetApp.Expressions
{
    public abstract class JsonPathNode
    {
        protected internal delegate bool Parser(ReadOnlySpan<char> expr, out int advance, out JsonPathNode node);

        internal JsonPathNode(ReadOnlySpan<char> expr = default)
        {
            Nodes = ParseNodes(expr.ToArray()).ToCachedEnumerable();
        }

        public IEnumerable<JsonPathNode> Nodes { get; }

        public Expression Expression { get; private set; }

        protected virtual Parser[] NodeParsers => Array.Empty<Parser>();

        private IEnumerable<JsonPathNode> ParseNodes(ReadOnlyMemory<char> exprMem)
        {
            ReadOnlySpan<char> expr;

            while (!exprMem.IsEmpty)
            {
                expr = exprMem.Span;
                JsonPathNode node = Parse(ref expr);
                exprMem = expr.ToArray();
                yield return node;
            }
        }

        protected internal virtual Expression ToExpression(Expression expression)
        {
            Expression = expression;
            return expression;
        }

        private JsonPathNode Parse(ref ReadOnlySpan<char> expr)
        {
            foreach (Parser TryParse in NodeParsers)
            {
                if (TryParse(expr, out int advance, out JsonPathNode node))
                {
                    expr = expr.Slice(advance);
                    return node;
                }
            }

            throw new JsonPathInvalidSyntaxException(expr);
        }
    }
}
