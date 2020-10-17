using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DotNetApp.Collections.Extensions;

namespace DotNetApp.Expressions
{
    public class JsonPathItemsSelectorNode : JsonPathNode
    {
        private static MethodInfo methodDefinitionSelect;

        static JsonPathItemsSelectorNode()
        {
            methodDefinitionSelect = typeof(Enumerable)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(x => x.Name == nameof(Enumerable.Select))
                .Where(x => x.GetParameters() is var parameters
                    && parameters.Length == 2
                    && parameters[0].ParameterType.IsGenericType
                    && parameters[0].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                    && parameters[1].ParameterType.IsGenericType
                    && parameters[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>))
                .Single();
        }

        internal JsonPathItemsSelectorNode(ReadOnlySpan<char> expr) : base (expr) {}

        public JsonPathNode FilterRoot { get; private set; }

        protected override Parser[] NodeParsers { get; } = new Parser[] {
            JsonPathPropertySelectorNode.TryParse,
            JsonPathIndexSelectorNode.TryParse,
            JsonPathItemsSelectorNode.TryParse
        };

        internal static bool TryParse(ReadOnlySpan<char> expr, out int advance, out JsonPathNode node)
        {
            advance = default;
            node = default;

            if (expr.Length <= 3) return false;

            if (expr[1] == '\'') return false;

            if (expr[0] != '[') return false;

            int nesting = 1;
            int i = 1;
            for (; i < expr.Length && nesting != 0; ++i)
            {
                if (expr[i] == '[') ++nesting;
                else if (expr[i] == ']') --nesting;
            }

            if (nesting != 0) return false;

            advance = expr.Length;
            node = new JsonPathItemsSelectorNode(expr.Slice(i))
            {
                FilterRoot = new JsonPathFilterRootNode(expr.Slice(1, i - 2))
            };

            return true;
        }

        protected internal override Expression ToExpression(Expression expression)
        {
            Type collectionType = expression.Type;
            ParameterExpression collectionParameter = Expression.Parameter(collectionType);
            Expression filterExpression = FilterRoot.ToExpression(collectionParameter);
            
            Type itemType = EnumerableExtensions.GetItemType(collectionType);
            ParameterExpression itemParameter = Expression.Parameter(itemType);
            Expression selectorExpression = itemParameter;

            foreach (JsonPathNode node in Nodes)
            {
                selectorExpression = node.ToExpression(selectorExpression);
            }

            LambdaExpression selectorLambda = Expression.Lambda(selectorExpression, itemParameter);
            MethodInfo methodSelect = methodDefinitionSelect.MakeGenericMethod(itemType, selectorExpression.Type);
            expression = Expression.Call(methodSelect, filterExpression, selectorLambda);

            return base.ToExpression(expression);
        }
    }
}
