using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DotNetApp.Extensions
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class DependsOnAttribute : Attribute
    {
        public DependsOnAttribute(params string[] propertyNames)
        {
            PropertyNames = propertyNames;
        }

        public string[] PropertyNames { get; set; }
    }

    public static class NotifyPropertyChangedExtensions
    {
        private static ConcurrentDictionary<Type, ConcurrentDictionary<string, string[]>> propertyDependencies;

        static NotifyPropertyChangedExtensions()
        {
            propertyDependencies = new ConcurrentDictionary<Type, ConcurrentDictionary<string, string[]>>();
        }

        public static Action ForwardPropertyChanged(this INotifyPropertyChanged source, string sourcePropertyName, INotifyPropertyChanged target, string targetPropertyName)
        {
            WeakReference wr = new WeakReference(target);
            return SubscribeToPropertyChanged(source, sourcePropertyName, _ =>
            {
                if (!wr.IsAlive) return;
                RaisePropertyChanged((INotifyPropertyChanged)wr.Target, targetPropertyName);
            });
        }

        public static Action SubscribeToPropertyChanged<T>(this T source, string propertyName, Action<T> action)
            where T : INotifyPropertyChanged
        {
            source.PropertyChanged += SourcePropertyChanged;

            return () => source.PropertyChanged -= SourcePropertyChanged;

            void SourcePropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName != propertyName) return;
                action((T)sender);
            }
        }

        public static void RaisePropertyChanged(this INotifyPropertyChanged sender, [CallerMemberName] string propertyName = default)
        {
            if (sender == null) return;
            EventExtensions.RaiseEvent(sender, nameof(INotifyPropertyChanged.PropertyChanged), new PropertyChangedEventArgs(propertyName));
            
            foreach (string dependentPropertyName in GetDependentPropertyNames(sender.GetType(), propertyName))
            {
                EventExtensions.RaiseEvent(sender, nameof(INotifyPropertyChanged.PropertyChanged), new PropertyChangedEventArgs(dependentPropertyName));
            }
        }

        private static IEnumerable<string> GetDependentPropertyNames(Type senderType, string propertyName)
        {
            if (!propertyDependencies.TryGetValue(senderType, out ConcurrentDictionary<string, string[]> dependencyMap))
            {
                Dictionary<string, SortedSet<string>> newDependencyMap = new Dictionary<string, SortedSet<string>>();

                foreach (PropertyInfo propertyInfo in senderType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    foreach (DependsOnAttribute dependsOnAttribute in propertyInfo.GetCustomAttributes<DependsOnAttribute>())
                    {
                        foreach (string precedentPropertyName in dependsOnAttribute.PropertyNames)
                        {
                            if (!newDependencyMap.TryGetValue(precedentPropertyName, out SortedSet<string> propertyNames))
                            {
                                propertyNames = new SortedSet<string>();
                                newDependencyMap.Add(precedentPropertyName, propertyNames);
                            }

                            propertyNames.Add(propertyInfo.Name);
                        }
                    }
                }

                dependencyMap = new ConcurrentDictionary<string, string[]>(newDependencyMap.Select(kvp => new KeyValuePair<string, string[]>(kvp.Key, kvp.Value.ToArray())));
                propertyDependencies[senderType] = dependencyMap;
            }

            if (dependencyMap.TryGetValue(propertyName, out string[] dependentPropertyNames))
            {
                foreach (string dependentPropertyName in dependentPropertyNames)
                {
                    yield return dependentPropertyName;
                }
            }

            if (senderType.BaseType is null) yield break;

            foreach (string dependentPropertyName in GetDependentPropertyNames(senderType.BaseType, propertyName))
            {
                yield return dependentPropertyName;
            }
        }
    }
}
