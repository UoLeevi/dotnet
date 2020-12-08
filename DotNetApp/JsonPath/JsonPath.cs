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
            => Create(typeof(T), jsonpath);

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
            return jsonPath.Delegate.DynamicInvoke(obj);
        }

        private Delegate function;

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

        private LambdaExpression ToExpression(Type type)
        {
            ParameterExpression parameter = LinqExpression.Parameter(type, "arg");
            Expression body = Root.ToExpression(parameter);

            return LinqExpression.Lambda(body, parameter);
        }

        public LambdaExpression Expression { get; }

        public Delegate Delegate
        {
            get
            {
                if (function is null)
                {
                    function = Expression.Compile();
                }

                return function;
            }
        }

        public JsonPathRootNode Root { get; }
    }
}
