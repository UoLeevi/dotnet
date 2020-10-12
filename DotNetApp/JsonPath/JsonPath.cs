using System;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;

namespace DotNetApp.Expressions
{
    public class JsonPath
    {
        internal JsonPath(string jsonpathExpression)
        {
            ReadOnlySpan<char> expr = jsonpathExpression.AsSpan();
            Root = JsonPathAst.Root.Parse(ref expr);
        }

        public JsonPathAst.Root Root;

        public static JsonPath Parse(string jsonpathExpression)
            => new JsonPath(jsonpathExpression);

        public LambdaExpression ToExpression(Type type)
        {
            ParameterExpression parameter = Expression.Parameter(type, "arg");
            Expression body = Root.ToExpression(parameter);

            return Expression.Lambda(body, parameter);
        }
    }
}
