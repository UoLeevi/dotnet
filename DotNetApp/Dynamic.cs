using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace DotNetApp
{
    public static class Dynamic
    {
        private static class Store<TSource, TValue>
        {
            internal static readonly ConcurrentDictionary<(Type type, string propertyOrFieldName), Func<TSource, TValue>> Getters = new ConcurrentDictionary<(Type type, string propertyOrFieldName), Func<TSource, TValue>>();

            internal static Func<TSource, TValue> CreateGetter((Type type, string propertyOrFieldName) key)
            {
                (Type type, string propertyOrFieldName) = key;
                ParameterExpression parameter = Expression.Parameter(typeof(TSource));
                Expression propertyAccess = Expression.PropertyOrField(parameter.Type == type ? (Expression)parameter : Expression.Convert(parameter, type), propertyOrFieldName);
                Expression body = propertyAccess.Type == typeof(TValue) ? propertyAccess : Expression.Convert(propertyAccess, typeof(TValue));
                var lambda = Expression.Lambda<Func<TSource, TValue>>(body, parameter);
                return lambda.Compile();
            }
        }

        public static TValue GetPropertyOrFieldValue<TSource, TValue>(TSource source, string propertyOrFieldName)
        {
            var getValue = Store<TSource, TValue>.Getters.GetOrAdd((source.GetType(), propertyOrFieldName), Store<TSource, TValue>.CreateGetter);
            return getValue(source);
        }
    }
}
