using System;

namespace DotNetApp.Expressions
{
    public class JsonPathFilterWildcardNode : JsonPathNode
    {
        public JsonPathFilterWildcardNode() : base() {}

        protected override Parser[] NodeParsers { get; } = new Parser[] {
        };

        internal static bool TryParse(ReadOnlySpan<char> expr, out int advance, out JsonPathNode node)
        {
            advance = default;
            node = default;

            if (expr.Length != 1 || expr[0] != '*') return false;

            advance = expr.Length;
            node = new JsonPathFilterWildcardNode();
            return true;
        }
    }
}
