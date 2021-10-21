using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using DotNetApp.Expressions;

namespace DotNetApp.Extensions
{
    public class PropertyChangingExtendedEventArgs<T> : PropertyChangingEventArgs
    {
        public T OldValue { get; }
        public T NewValue { get; }

        public PropertyChangingExtendedEventArgs(string propertyName, T oldValue, T newValue) : base(propertyName)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    public class PropertyChangedExtendedEventArgs<T> : PropertyChangedEventArgs
    {
        public T OldValue { get; }
        public T NewValue { get; }

        public PropertyChangedExtendedEventArgs(string propertyName, T oldValue, T newValue) : base(propertyName)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    public static partial class NotifyPropertyChangedExtensions
    {
        private static ConditionalWeakTable<INotifyPropertyChanged, object> instanceTable;
        private static ImmutableDictionary<Type, ImmutableDictionary<string, JsonPath[]>> dependencyDefinitions;


        private static void ConditionallyRegisterDependentInstance(INotifyPropertyChanged target)
        {
            if (instanceTable.TryGetValue(target, out _)) return;

            lock (target)
            {
                if (instanceTable.TryGetValue(target, out _)) return;
                instanceTable.Add(target, null);
            }

            Type dependentType = target.GetType();

            if (!dependencyDefinitions.TryGetValue(dependentType, out var dependenciesByPropertyName)) return;

            foreach (var dependency in dependenciesByPropertyName)
            {
                string targetPropertyName = dependency.Key;

                foreach (JsonPath jsonpath in dependency.Value)
                {
                    var dependencyNodes = ImmutableQueue.Create(jsonpath.Root.Nodes.ToArray());
                    INotifyPropertyChanged source = target;
                    source.ForwardPropertyChanged(dependencyNodes, target, targetPropertyName);
                }
            }
        }

        static NotifyPropertyChangedExtensions()
        {
            instanceTable = new ConditionalWeakTable<INotifyPropertyChanged, object>();

            var propertyDependencyMap = new Dictionary<Type, ImmutableDictionary<string, JsonPath[]>>();

            var assemblyName = typeof(NotifyPropertyChangedExtensions).Assembly.GetName();

            var referencingAssemblies = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a => a.GetReferencedAssemblies().Any(r => AssemblyName.ReferenceMatchesDefinition(r, assemblyName)));

            var types = referencingAssemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(INotifyPropertyChanged).IsAssignableFrom(t) && t.IsClass);

            foreach (var type in types)
            {
                Dictionary<string, List<JsonPath>> newDependencyMap = new Dictionary<string, List<JsonPath>>();

                var propertiesWithDependencies = type
                    .GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .Select<PropertyInfo, (PropertyInfo property, DependsOnAttribute[] dependencies)>(p => (p, p.GetCustomAttributes<DependsOnAttribute>().ToArray()))
                    .Where(p => p.dependencies.Length != 0);

                foreach ((PropertyInfo property, DependsOnAttribute[] dependsOnAttributes) in propertiesWithDependencies)
                {
                    List<JsonPath> dependencies = new List<JsonPath>();
                    newDependencyMap.Add(property.Name, dependencies);

                    foreach (DependsOnAttribute dependsOnAttribute in dependsOnAttributes)
                    {
                        foreach (string jsonpath in dependsOnAttribute.PropertyDependencies)
                        {
                            JsonPath propertyDependency = new JsonPath(type, jsonpath);

                            dependencies.Add(propertyDependency);
                        }
                    }
                }

                propertyDependencyMap[type] = newDependencyMap.Select(kvp => new KeyValuePair<string, JsonPath[]>(kvp.Key, kvp.Value.ToArray())).ToImmutableDictionary();
            }

            dependencyDefinitions = propertyDependencyMap.ToImmutableDictionary();
        }

        public static void InitializeChangeNotifications(this INotifyPropertyChanged source)
        {
            ConditionallyRegisterDependentInstance(source);
        }

        private static void NotifyComputedPropertyIfChanged(this INotifyPropertyChanged source, string propertyName = default)
        {
            ConditionallyRegisterDependentInstance(source);

            var value = Dynamic.GetPropertyOrFieldValue<INotifyPropertyChanged, object>(source, propertyName);
            var fields = Store.GetBackingFields(source);

            if (fields.TryGetValue(propertyName, out var current) && Equals(value, current))
            {
                return;
            }

            fields[propertyName] = value;
            source.RaisePropertyChanged(propertyName);
        }


        public static void SetProperty<TValue>(this INotifyPropertyChanged source, TValue value, [CallerMemberName] string propertyName = default)
        {
            ConditionallyRegisterDependentInstance(source);

            var fields = Store<TValue>.GetBackingFields(source);
            var current = fields.GetOrAdd(propertyName, default(TValue));

            if (EqualityComparer<TValue>.Default.Equals(value, current)) return;

            if (source is INotifyPropertyChanging notifyChangingSource)
            {
                notifyChangingSource.RaisePropertyChanging(propertyName);
            }

            fields[propertyName] = value;
            source.RaisePropertyChanged(current, value, propertyName);
        }


        public static TValue GetProperty<TValue>(this INotifyPropertyChanged source, [CallerMemberName] string propertyName = default)
        {
            ConditionallyRegisterDependentInstance(source);

            var fields = Store<TValue>.GetBackingFields(source);
            return fields.GetOrAdd(propertyName, default(TValue));
        }

        public static TValue GetProperty<TValue>(this INotifyPropertyChanged source, Func<TValue> getter, [CallerMemberName] string propertyName = default)
        {
            ConditionallyRegisterDependentInstance(source);
            return getter();
        }

        private static Action ForwardPropertyChanged(this INotifyPropertyChanged source, ImmutableQueue<JsonPathNode> dependencyNodes, INotifyPropertyChanged target, string targetPropertyName)
        {
            WeakReference<INotifyPropertyChanged> wr = new WeakReference<INotifyPropertyChanged>(target);
            Action unsubscribe = null;
            dependencyNodes = dependencyNodes.Dequeue(out var node);

            switch (node)
            {
                case JsonPathPropertySelectorNode propertySelectorNode:
                    string sourcePropertyName = propertySelectorNode.PropertyName;
                    unsubscribe = source.ForwardPropertyChanged(sourcePropertyName, target, targetPropertyName);

                    if (dependencyNodes.IsEmpty) return unsubscribe;

                    Action unsubscribeNested = null;

                    if (propertySelectorNode.GetValue(source) is INotifyPropertyChanged nestedSource)
                    {
                        unsubscribeNested = nestedSource.ForwardPropertyChanged(dependencyNodes, target, targetPropertyName);
                        unsubscribe += unsubscribeNested;
                    }

                    unsubscribe += source.Bind<INotifyPropertyChanged, object>(sourcePropertyName, (value) =>
                    {
                        unsubscribe -= unsubscribeNested;
                        unsubscribeNested?.Invoke();
                        if (!wr.TryGetTarget(out var t)) return;

                        if (value is INotifyPropertyChanged nestedSrc)
                        {
                            unsubscribeNested = nestedSrc.ForwardPropertyChanged(dependencyNodes, t, targetPropertyName);
                            unsubscribe += unsubscribeNested;
                        }
                    });

                    return unsubscribe;

                case JsonPathItemsSelectorNode itemsSelectorNode:
                    if (source is IEnumerable<INotifyPropertyChanged> collection)
                    {
                        dependencyNodes = ImmutableQueue.Create(itemsSelectorNode.Nodes.ToArray());
                        unsubscribe = collection.ForwardItemPropertyChanged(dependencyNodes, target, targetPropertyName);
                    }

                    return unsubscribe;

                default:
                    throw new NotImplementedException();
            }
        }

        public static Action ForwardPropertyChanging(this INotifyPropertyChanging source, string sourcePropertyName, INotifyPropertyChanging target, string targetPropertyName, bool unsubscribeAfterEvent = false)
        {
            WeakReference<INotifyPropertyChanging> wr = new WeakReference<INotifyPropertyChanging>(target);
            return SubscribeToPropertyChanging(source, sourcePropertyName, _ =>
            {
                if (!wr.TryGetTarget(out var t)) return;
                RaisePropertyChanging(t, targetPropertyName);
            }, unsubscribeAfterEvent);
        }

        public static Action ForwardPropertyChanged(this INotifyPropertyChanged source, string sourcePropertyName, INotifyPropertyChanged target, string targetPropertyName)
        {
            WeakReference<INotifyPropertyChanged> wr = new WeakReference<INotifyPropertyChanged>(target);
            return source.Bind<INotifyPropertyChanged, object>(sourcePropertyName, _ =>
            {
                if (!wr.TryGetTarget(out var t)) return;
                t.NotifyComputedPropertyIfChanged(targetPropertyName);
            });
        }


        public static Action SubscribeToPropertyChanging<T>(this T source, string propertyName, Action<T> action, bool unsubscribeAfterEvent = false)
            where T : INotifyPropertyChanging
        {
            ConditionallyRegisterDependentInstance((INotifyPropertyChanged)source);

            source.PropertyChanging += SourcePropertyChanging;
            return () => source.PropertyChanging -= SourcePropertyChanging;

            void SourcePropertyChanging(object sender, PropertyChangingEventArgs e)
            {
                if (e.PropertyName != propertyName) return;

                T src = (T)sender;

                if (unsubscribeAfterEvent)
                {
                    src.PropertyChanging -= SourcePropertyChanging;
                }

                action(src);
            }
        }

        private static Action ForwardItemPropertyChanged<T>(this IEnumerable<T> source, ImmutableQueue<JsonPathNode> dependencyNodes, INotifyPropertyChanged target, string targetPropertyName)
            where T : class, INotifyPropertyChanged
        {
            var subscriptions = new ConditionalWeakTable<T, Action>();
            var wrTarget = new WeakReference<INotifyPropertyChanged>(target);
            var wrSource = new WeakReference<IEnumerable<T>>(source);

            foreach (var item in source.Distinct())
            {
                subscriptions.Add(item, item.ForwardPropertyChanged(dependencyNodes, target, targetPropertyName));
            }

            if (source is INotifyCollectionChanged eventSource)
            {
                eventSource.CollectionChanged += SourceCollectionChanged;
            }

            return () =>
            {
                if (!wrSource.TryGetTarget(out var s)) return;

                if (s is INotifyCollectionChanged eventSrc)
                {
                    eventSrc.CollectionChanged -= SourceCollectionChanged;
                }

                if (subscriptions is null) return;

                foreach (var item in s.Distinct())
                {
                    if (subscriptions.TryGetValue(item, out Action unsubscribe))
                    {
                        unsubscribe();
                    }
                }

                subscriptions = null;
            };

            void SourceCollectionChanged(object s, NotifyCollectionChangedEventArgs e)
            {
                var items = (IEnumerable<T>)s;
                var empty = Enumerable.Empty<T>();
                var removed = e.OldItems?.Cast<T>().Distinct().Except(items) ?? empty;
                var added = e.NewItems?.Cast<T>().Distinct().Where(i => !subscriptions.TryGetValue(i, out _)) ?? empty;

                foreach (var item in removed)
                {
                    if (subscriptions.TryGetValue(item, out Action unsubscribe))
                    {
                        subscriptions.Remove(item);
                        unsubscribe();
                    }
                }

                if (!wrTarget.TryGetTarget(out var t)) return;

                t.NotifyComputedPropertyIfChanged(targetPropertyName);

                foreach (var item in added)
                {
                    subscriptions.Add(item, item.ForwardPropertyChanged(dependencyNodes, t, targetPropertyName));
                }
            };
        }


        public static Action ForwardItemPropertyChanged<T>(this IEnumerable<T> source, string sourcePropertyName, INotifyPropertyChanged target, string targetPropertyName)
            where T : class, INotifyPropertyChanged
            => ForwardItemPropertyChanged<INotifyPropertyChanged>(source, null, sourcePropertyName, target, targetPropertyName);

        public static Action ForwardItemPropertyChanged<T>(this IEnumerable<T> source, Func<T, bool> filterPredicate, string sourcePropertyName, INotifyPropertyChanged target, string targetPropertyName)
            where T : class, INotifyPropertyChanged
        {
            var wr = new WeakReference<INotifyPropertyChanged>(target);
            return SubscribeToItemPropertyChanged(source, filterPredicate, sourcePropertyName, (_) =>
            {
                if (!wr.TryGetTarget(out var t)) return;
                t.NotifyComputedPropertyIfChanged(targetPropertyName);
            });
        }

        public static Action SubscribeToItemPropertyChanged<T>(this IEnumerable<T> source, string propertyName, Action<T> action)
            where T : class, INotifyPropertyChanged
            => SubscribeToItemPropertyChanged<T>(source, null, propertyName, action);

        public static Action SubscribeToItemPropertyChanged<T>(this IEnumerable<T> source, Func<T, bool> filterPredicate, string propertyName, Action<T> action)
            where T : class, INotifyPropertyChanged
        {
            var subscriptions = new ConditionalWeakTable<T, Action>();

            foreach (var item in source.Distinct())
            {
                if (filterPredicate?.Invoke(item) is false) continue;
                subscriptions.Add(item, item.Bind<T, object>(propertyName, (s, v) => action(s)));
            }

            if (source is INotifyCollectionChanged eventSource)
            {
                eventSource.CollectionChanged += SourceCollectionChanged;
            }

            var wr = new WeakReference<IEnumerable<T>>(source);
            return () =>
            {
                if (!wr.TryGetTarget(out var s)) return;

                if (s is INotifyCollectionChanged eventSrc)
                {
                    eventSrc.CollectionChanged -= SourceCollectionChanged;
                }

                if (subscriptions is null) return;

                foreach (var item in s.Distinct())
                {
                    if (subscriptions.TryGetValue(item, out Action unsubscribe))
                    {
                        unsubscribe();
                    }
                }

                subscriptions = null;
            };

            void SourceCollectionChanged(object s, NotifyCollectionChangedEventArgs e)
            {
                var items = (IEnumerable<T>)s;
                var empty = Enumerable.Empty<T>();
                var removed = e.OldItems?.Cast<T>().Distinct().Except(items) ?? empty;
                var added = e.NewItems?.Cast<T>().Distinct().Where(i => !subscriptions.TryGetValue(i, out _)) ?? empty;

                foreach (var item in removed)
                {
                    if (subscriptions.TryGetValue(item, out Action unsubscribe))
                    {
                        subscriptions.Remove(item);
                        unsubscribe();
                        action(item);
                    }
                }

                foreach (var item in added)
                {
                    if (filterPredicate?.Invoke(item) is false) continue;
                    action(item);
                    subscriptions.Add(item, item.Bind<T, object>(propertyName, (i, v) => action(i)));
                }
            };
        }

        public static void RaisePropertyChanged<T>(this INotifyPropertyChanged sender, T oldValue, T newValue, [CallerMemberName] string propertyName = default)
        {
            if (sender == null) return;
            EventExtensions.RaiseEvent(sender, nameof(INotifyPropertyChanged.PropertyChanged), new PropertyChangedExtendedEventArgs<T>(propertyName, oldValue, newValue));
        }

        public static void RaisePropertyChanged(this INotifyPropertyChanged sender, [CallerMemberName] string propertyName = default)
        {
            if (sender == null) return;
            EventExtensions.RaiseEvent(sender, nameof(INotifyPropertyChanged.PropertyChanged), new PropertyChangedEventArgs(propertyName));
        }

        public static void RaisePropertyChanging(this INotifyPropertyChanging sender, [CallerMemberName] string propertyName = default)
        {
            if (sender == null) return;
            EventExtensions.RaiseEvent(sender, nameof(INotifyPropertyChanging.PropertyChanging), new PropertyChangingEventArgs(propertyName));
        }
    }
}
