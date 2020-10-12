using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DotNetApp.Collections.Extensions
{
    public static class EnumerableExtensions
    {
        private readonly static ConcurrentDictionary<Type, Type> enumerableTypes;

        static EnumerableExtensions()
        {
            enumerableTypes = new ConcurrentDictionary<Type, Type>();
        }

        public static Type GetItemType(Type type)
        {
            return enumerableTypes.GetOrAdd(type, IEnumerableTypeFactory);
        }

        private static Type IEnumerableTypeFactory(Type type)
        {
            if (type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return type.GetGenericArguments()[0];
            }
            else
            {
                return type
                    .GetInterfaces()
                    .Single(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    .GetGenericArguments()[0];
            }
        }
    }
}
