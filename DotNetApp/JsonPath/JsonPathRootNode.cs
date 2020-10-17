using System;
using System.Linq.Expressions;

namespace DotNetApp.Expressions
{
    public class JsonPathRootNode : JsonPathNode
    {
        public JsonPathRootNode(ReadOnlySpan<char> expr) : base(expr) {}

        protected override Parser[] NodeParsers { get; } = new Parser[] {
            JsonPathPropertySelectorNode.TryParse,
            JsonPathIndexSelectorNode.TryParse,
            JsonPathItemsSelectorNode.TryParse
        };

        protected internal override Expression ToExpression(Expression expression)
        {
            foreach (JsonPathNode node in Nodes)
            {
                expression = node.ToExpression(expression);
            }

            return base.ToExpression(expression);
        }
    }
}
