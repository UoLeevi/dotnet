using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using LinqExpression = System.Linq.Expressions.Expression;

namespace DotNetApp.Expressions
{
    public class JsonPath
    {
        private const int cacheCapacityPerType = 1000;
        private static ConcurrentDictionary<Type, ConcurrentDictionary<string, JsonPath>> cache;

        static JsonPath()
        {
            cache = new ConcurrentDictionary<Type, ConcurrentDictionary<string, JsonPath>>();
        }

        public static JsonPath Create<T>(string jsonpath)
            where T : class
        {
            return Create(typeof(T), jsonpath);
        }

        public static JsonPath Create(Type type, string jsonpath)
        {
            var cache = JsonPath.cache.GetOrAdd(type, CreateCache);

            try
            {
                return cache.GetOrAdd(jsonpath, CreateJsonPath);
            }
            catch (OverflowException)
            {
                cache.Clear();
                return cache.GetOrAdd(jsonpath, CreateJsonPath);
            }

            ConcurrentDictionary<string, JsonPath> CreateCache(Type _)
                => new ConcurrentDictionary<string, JsonPath>(1, capacity: cacheCapacityPerType);

            JsonPath CreateJsonPath(string expr)
                => new JsonPath(type, expr);
        }

        public static object Evaluate<T>(T obj, string jsonpath)
            where T : class
        {
            JsonPath jsonPath = Create(obj.GetType(), jsonpath);
            return jsonPath.GetValue(obj);
        }

        private Func<object, object> getValueFunc;

        public JsonPath(Type type, string jsonpath)
        {
            ReadOnlySpan<char> expr = jsonpath.AsSpan();

            if (expr[0] == '$')
            {
                expr = expr.Slice(1);
            }

            if (expr[0] != '.' && expr[0] != '[')
            {
                expr = string.Concat(".", expr.ToString()).AsSpan();
            }


            Root = new JsonPathRootNode(expr);
            Expression = ToExpression(type);
        }

        private Expression<Func<object, object>> ToExpression(Type type)
        {
            var parameter = LinqExpression.Parameter(typeof(object));
            var castedParameter = LinqExpression.Convert(parameter, type);
            var jsonpathExpr = Root.ToExpression(castedParameter);
            var castedResult = LinqExpression.Convert(jsonpathExpr, typeof(object));
            var lambda = LinqExpression.Lambda<Func<object, object>>(castedResult, parameter);

            return lambda;
        }

        public Expression<Func<object, object>> Expression { get; }

        public Func<object, object> GetValue
        {
            get
            {
                if (getValueFunc is null)
                {
                    getValueFunc = Expression.Compile();
                }

                return getValueFunc;
            }
        }

        public JsonPathRootNode Root { get; }
    }
}
