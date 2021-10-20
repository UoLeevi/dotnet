using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DotNetApp.Extensions
{
    public static partial class NotifyPropertyChangedExtensions
    {
        internal static class Store
        {
            private static readonly ConditionalWeakTable<object, ConcurrentDictionary<string, object>> BackingFields = new ConditionalWeakTable<object, ConcurrentDictionary<string, object>>();

            private static ConcurrentDictionary<string, object> CreateBackingFields(object source)
            {
                return new ConcurrentDictionary<string, object>();
            }

            internal static ConcurrentDictionary<string, object> GetBackingFields(object source)
            {
                return BackingFields.GetValue(source, CreateBackingFields);
            }
        }

        internal static class Store<T>
        {
            private static readonly ConditionalWeakTable<object, ConcurrentDictionary<string, T>> BackingFields = new ConditionalWeakTable<object, ConcurrentDictionary<string, T>>();

            private static ConcurrentDictionary<string, T> CreateBackingFields(object source)
            {
                return new ConcurrentDictionary<string, T>();
            }

            internal static ConcurrentDictionary<string, T> GetBackingFields(object source)
            {
                return BackingFields.GetValue(source, CreateBackingFields);
            }
        }
    }
}
