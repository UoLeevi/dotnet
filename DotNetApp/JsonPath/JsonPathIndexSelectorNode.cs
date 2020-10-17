using System;
using System.Linq.Expressions;

namespace DotNetApp.Expressions
{
    public class JsonPathIndexSelectorNode : JsonPathNode
    {
        internal JsonPathIndexSelectorNode() : base () {}

        public int Index { get; private set; }

        internal static bool TryParse(ReadOnlySpan<char> expr, out int advance, out JsonPathNode node)
        {
            advance = default;
            node = default;

            if (expr.Length <= 3) return false;

            if (expr[0] != '[') return false;

            int i = expr.IndexOf(']');

            if (i == -1) return false;

            if (!int.TryParse(expr.Slice(1, i - 1).ToString(), out int index)) return false;

            advance = i + 1;
            node = new JsonPathIndexSelectorNode()
            {
                Index = index
            };

            return true;
        }

        protected internal override Expression ToExpression(Expression expression)
            => throw new NotImplementedException();
    }
}
