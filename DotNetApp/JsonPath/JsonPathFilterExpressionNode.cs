using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DotNetApp.Collections.Extensions;

namespace DotNetApp.Expressions
{
    public class JsonPathFilterExpressionNode : JsonPathNode
    {
        private static MethodInfo methodDefinitionWhere;

        static JsonPathFilterExpressionNode()
        {
            methodDefinitionWhere = typeof(Enumerable)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(x => x.Name == nameof(Enumerable.Where))
                .Where(x => x.GetParameters() is var parameters
                    && parameters.Length == 2
                    && parameters[0].ParameterType.IsGenericType
                    && parameters[0].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                    && parameters[1].ParameterType.IsGenericType
                    && parameters[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>))
                .Single();
        }

        public JsonPathFilterExpressionNode(ReadOnlySpan<char> expr) : base(expr) {}

        protected override Parser[] NodeParsers { get; } = new Parser[] {
            JsonPathFilterWildcardNode.TryParse
        };

        internal static bool TryParse(ReadOnlySpan<char> expr, out int advance, out JsonPathNode node)
        {
            advance = default;
            node = default;

            if (expr.Length < 4) return false;

            if (!expr.StartsWith("?(".AsSpan())) return false;

            if (expr[expr.Length - 1] != ')') return false;

            throw new NotImplementedException();

            return true;
        }

        protected internal override Expression ToExpression(Expression expression)
        {
            Type itemType = EnumerableExtensions.GetItemType(expression.Type);
            ParameterExpression itemParameter = Expression.Parameter(itemType);
            Expression predicateExpression = itemParameter;

            foreach (JsonPathNode node in Nodes)
            {
                predicateExpression = node.ToExpression(predicateExpression);
            }

            LambdaExpression predicateLambda = Expression.Lambda(predicateExpression, itemParameter);
            MethodInfo methodWhere = methodDefinitionWhere.MakeGenericMethod(itemType);
            expression = Expression.Call(methodWhere, expression, predicateLambda);

            return base.ToExpression(expression);
        }
    }
}
