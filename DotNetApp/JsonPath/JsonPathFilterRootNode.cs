using System;

namespace DotNetApp.Expressions
{
    public class JsonPathFilterRootNode : JsonPathRootNode
    {
        public JsonPathFilterRootNode(ReadOnlySpan<char> expr) : base(expr) {}

        protected override Parser[] NodeParsers { get; } = new Parser[] {
            JsonPathFilterWildcardNode.TryParse,
            JsonPathFilterExpressionNode.TryParse
        };
    }
}
