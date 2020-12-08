using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace DotNetApp.Expressions
{
    public class JsonPathPropertySelectorNode : JsonPathNode
    {
        internal JsonPathPropertySelectorNode() : base () {}

        public string PropertyName { get; private set; }

        internal static bool TryParse(ReadOnlySpan<char> expr, out int advance, out JsonPathNode node)
        {
            advance = default;
            node = default;

            if (expr.Length <= 2) return false;

            if (expr[1] == '*') return false;

            if (expr[1] == '.') return false;

            if (expr[0] == '.')
            {
                Match identifier = CSharpIdentifierRegex.Match(expr.Slice(1).ToString());

                if (!identifier.Success) return false;

                advance = identifier.Length + 1;
                node = new JsonPathPropertySelectorNode()
                {
                    PropertyName = identifier.Value
                };

                return true;
            }

            if (expr[0] == '[' && expr[1] == '\'')
            {
                Match identifier = CSharpIdentifierRegex.Match(expr.Slice(2).ToString());

                if (!identifier.Success) return false;

                if (!expr.Slice(identifier.Length + 2).StartsWith("']".AsSpan())) return false;

                advance = identifier.Length + 4;
                node = new JsonPathPropertySelectorNode()
                {
                    PropertyName = identifier.Value
                };

                return true;
            }

            return false;
        }

        protected internal override Expression ToExpression(Expression expression)
        {
            expression = Expression.PropertyOrField(expression, PropertyName);
            return base.ToExpression(expression);
        }

        private static Regex CSharpIdentifierRegex = new Regex(@"^[\w_]+[\d\w_]*", RegexOptions.Compiled);
    }
}
