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

        public static void SetProperty<TValue>(this INotifyPropertyChanged source, TValue value, [CallerMemberName] string propertyName = default)
        {
            ConditionallyRegisterDependentInstance(source);

            var fields = GetBackingFields<TValue>(source);
            var current = fields.GetOrAdd(propertyName, default(TValue));

            if (EqualityComparer<TValue>.Default.Equals(value, current)) return;

            if (source is INotifyPropertyChanging notifyChangingSource)
            {
                notifyChangingSource.RaisePropertyChanging(propertyName);
            }

            fields[propertyName] = value;
            source.RaisePropertyChanged(propertyName);
        }

        public static TValue GetProperty<TValue>(this INotifyPropertyChanged source, [CallerMemberName] string propertyName = default)
        {
            ConditionallyRegisterDependentInstance(source);

            var fields = GetBackingFields<TValue>(source);
            return fields.GetOrAdd(propertyName, default(TValue));
        }

        public static TValue GetProperty<TValue>(this INotifyPropertyChanged source, Func<TValue> getter, [CallerMemberName] string propertyName = default)
        {
            ConditionallyRegisterDependentInstance(source);
            return getter();
        }

        private static Action ForwardPropertyChanged(this INotifyPropertyChanged source, ImmutableQueue<JsonPathNode> dependencyNodes, INotifyPropertyChanged target, string targetPropertyName)
        {
            WeakReference wr = new WeakReference(target);
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

                    unsubscribe += source.SubscribeToPropertyChanged(sourcePropertyName, sender =>
                    {
                        unsubscribe -= unsubscribeNested;
                        unsubscribeNested?.Invoke();
                        if (!wr.IsAlive) return;

                        if (propertySelectorNode.GetValue(source) is INotifyPropertyChanged nestedSrc)
                        {
                            unsubscribeNested = nestedSrc.ForwardPropertyChanged(dependencyNodes, (INotifyPropertyChanged)wr.Target, targetPropertyName);
                            unsubscribe += unsubscribeNested;
                        }
                    });

                    return unsubscribe;

                case JsonPathItemsSelectorNode itemsSelectorNode:
                    if (source is IEnumerable<INotifyPropertyChanged> collection)
                    {
                        dependencyNodes = ImmutableQueue.Create(itemsSelectorNode.Nodes.ToArray());
                        dependencyNodes = dependencyNodes.Dequeue(out node);
                        var itemNode = node as JsonPathPropertySelectorNode;
                        unsubscribe = collection.ForwardItemPropertyChanged(itemNode.PropertyName, target, targetPropertyName);

                        if (dependencyNodes.IsEmpty) return unsubscribe;

                        unsubscribe += collection.SubscribeToItemPropertyChanged(itemNode.PropertyName, item =>
                        {
                            if (!wr.IsAlive) return;
                            unsubscribe += item.ForwardPropertyChanged(dependencyNodes, (INotifyPropertyChanged)wr.Target, targetPropertyName);
                        });
                    }

                    return unsubscribe;

                default:
                    throw new NotImplementedException();
            }
        }

        public static Action ForwardPropertyChanging(this INotifyPropertyChanging source, string sourcePropertyName, INotifyPropertyChanging target, string targetPropertyName, bool unsubscribeAfterEvent = false)
        {
            WeakReference wr = new WeakReference(target);
            return SubscribeToPropertyChanging(source, sourcePropertyName, _ =>
            {
                if (!wr.IsAlive) return;
                RaisePropertyChanging((INotifyPropertyChanging)wr.Target, targetPropertyName);
            }, unsubscribeAfterEvent);
        }

        public static Action ForwardPropertyChanged(this INotifyPropertyChanged source, string sourcePropertyName, INotifyPropertyChanged target, string targetPropertyName, bool unsubscribeAfterEvent = false)
        {
            WeakReference wr = new WeakReference(target);
            return SubscribeToPropertyChanged(source, sourcePropertyName, _ =>
            {
                if (!wr.IsAlive) return;
                RaisePropertyChanged((INotifyPropertyChanged)wr.Target, targetPropertyName);
            }, unsubscribeAfterEvent);
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
        
        public static Action SubscribeToPropertyChanged<T>(this T source, string propertyName, Action<T> action, bool unsubscribeAfterEvent = false)
            where T : INotifyPropertyChanged
        {
            ConditionallyRegisterDependentInstance(source);

            source.PropertyChanged += SourcePropertyChanged;
            return () => source.PropertyChanged -= SourcePropertyChanged;

            void SourcePropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName != propertyName) return;

                T src = (T)sender;

                if (unsubscribeAfterEvent)
                {
                    src.PropertyChanged -= SourcePropertyChanged;
                }

                action(src);
            }
        }

        public static Action ForwardItemPropertyChanged<T>(this IEnumerable<T> source, string sourcePropertyName, INotifyPropertyChanged target, string targetPropertyName)
            where T : class, INotifyPropertyChanged
            => ForwardItemPropertyChanged<INotifyPropertyChanged>(source, null, sourcePropertyName, target, targetPropertyName);

        public static Action ForwardItemPropertyChanged<T>(this IEnumerable<T> source, Func<T, bool> filterPredicate, string sourcePropertyName, INotifyPropertyChanged target, string targetPropertyName)
            where T : class, INotifyPropertyChanged
        {
            var wr = new WeakReference(target);
            return SubscribeToItemPropertyChanged(source, filterPredicate, sourcePropertyName, _ =>
            {
                if (!wr.IsAlive) return;
                RaisePropertyChanged((INotifyPropertyChanged)wr.Target, targetPropertyName);
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
                subscriptions.Add(item, item.SubscribeToPropertyChanged(propertyName, action));
            }

            if (source is INotifyCollectionChanged eventSource)
            {
                eventSource.CollectionChanged += SourceCollectionChanged;
            }

            var wr = new WeakReference(source);
            return () =>
            {
                if (!wr.IsAlive) return;

                if (wr.Target is INotifyCollectionChanged eventSrc)
                {
                    eventSrc.CollectionChanged -= SourceCollectionChanged;
                }

                if (subscriptions is null) return;

                var items = (IEnumerable<T>)wr.Target;

                foreach (var item in items.Distinct())
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
                    subscriptions.Add(item, item.SubscribeToPropertyChanged(propertyName, action));
                }
            };
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
