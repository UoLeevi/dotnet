using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DotNetApp.Extensions
{
    public static partial class NotifyPropertyChangedExtensions
    {
        private static class Store<TValue>
        {
            internal static ConditionalWeakTable<INotifyPropertyChanged, ConcurrentDictionary<string, TValue>> BackingFields { get; }

            static Store()
            {
                BackingFields = new ConditionalWeakTable<INotifyPropertyChanged, ConcurrentDictionary<string, TValue>>();
            }
        }

        private static ConcurrentDictionary<string, TValue> GetBackingFields<TValue>(INotifyPropertyChanged source)
        {
            if (!Store<TValue>.BackingFields.TryGetValue(source, out ConcurrentDictionary<string, TValue> fields))
            {
                fields = new ConcurrentDictionary<string, TValue>();
                Store<TValue>.BackingFields.Add(source, fields);
            }

            return fields;
        }
    }
}
