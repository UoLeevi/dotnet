using System;
using System.Linq.Expressions;
using LinqExpression = System.Linq.Expressions.Expression;

namespace DotNetApp.Expressions
{
    public class JsonPath
    {
        public JsonPath(Type type, string jsonpath)
        {
            ReadOnlySpan<char> expr = jsonpath.AsSpan();

            if (expr[0] == '$')
            {
                expr.Slice(1);
            }

            if (expr[0] != '.' && expr[0] != '[')
            {
                expr = string.Concat(".", expr.ToString()).AsSpan();
            }


            Root = new JsonPathRootNode(expr);
            Expression = ToExpression(type);
        }

        private LambdaExpression ToExpression(Type type)
        {
            ParameterExpression parameter = LinqExpression.Parameter(type, "arg");
            Expression body = Root.ToExpression(parameter);

            return LinqExpression.Lambda(body, parameter);
        }

        public LambdaExpression Expression { get; }

        public JsonPathRootNode Root { get; }
    }
}
