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
        private static ImmutableDictionary<Type, ImmutableDictionary<string, ImmutableStack<PropertyInfo>[]>> dependencyDefinitions;

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

                foreach (ImmutableStack<PropertyInfo> properties in dependency.Value)
                {
                    INotifyPropertyChanged source = target;
                    source.ForwardPropertyChanged(properties, target, targetPropertyName);
                }
            }
        }

        private static ImmutableStack<PropertyInfo> FlattenChainedMemberExpression(MemberExpression memberExpression)
        {
            List<PropertyInfo> properties = new List<PropertyInfo>();

            do
            {
                properties.Add(memberExpression.Member as PropertyInfo);
                memberExpression = memberExpression.Expression as MemberExpression;
            } while (memberExpression is MemberExpression);

            return ImmutableStack.Create(properties.ToArray());
        }

        static NotifyPropertyChangedExtensions()
        {
            instanceTable = new ConditionalWeakTable<INotifyPropertyChanged, object>();

            var propertyDependencyMap = new Dictionary<Type, ImmutableDictionary<string, ImmutableStack<PropertyInfo>[]>>();

            var assemblyName = typeof(NotifyPropertyChangedExtensions).Assembly.GetName();

            var referencingAssemblies = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a => a.GetReferencedAssemblies().Any(r => AssemblyName.ReferenceMatchesDefinition(r, assemblyName)));

            var types = referencingAssemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(INotifyPropertyChanged).IsAssignableFrom(t) && t.IsClass);

            foreach (var type in types)
            {
                Dictionary<string, List<ImmutableStack<PropertyInfo>>> newDependencyMap = new Dictionary<string, List<ImmutableStack<PropertyInfo>>>();

                var propertiesWithDependencies = type
                    .GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .Select<PropertyInfo, (PropertyInfo property, DependsOnAttribute[] dependencies)>(p => (p, p.GetCustomAttributes<DependsOnAttribute>().ToArray()))
                    .Where(p => p.dependencies.Length != 0);

                foreach ((PropertyInfo property, DependsOnAttribute[] dependsOnAttributes) in propertiesWithDependencies)
                {
                    List<ImmutableStack<PropertyInfo>> dependencies = new List<ImmutableStack<PropertyInfo>>();
                    newDependencyMap.Add(property.Name, dependencies);

                    foreach (DependsOnAttribute dependsOnAttribute in dependsOnAttributes)
                    {
                        foreach (JsonPath propertyDependencyPath in dependsOnAttribute.PropertyDependencyPaths)
                        {
                            MemberExpression memberExpression = propertyDependencyPath.ToExpression(type).Body as MemberExpression;
                            dependencies.Add(FlattenChainedMemberExpression(memberExpression));
                        }
                    }
                }

                propertyDependencyMap[type] = newDependencyMap.Select(kvp => new KeyValuePair<string, ImmutableStack<PropertyInfo>[]>(kvp.Key, kvp.Value.ToArray())).ToImmutableDictionary();
            }

            dependencyDefinitions = propertyDependencyMap.ToImmutableDictionary();
        }

        public static void SetProperty<TValue>(this INotifyPropertyChanged source, TValue value, Action<TValue> callback = default, [CallerMemberName] string propertyName = default)
        {
            ConditionallyRegisterDependentInstance(source);

            var fields = GetBackingFields<TValue>(source);
            var current = fields.GetOrAdd(propertyName, default(TValue));

            if (EqualityComparer<TValue>.Default.Equals(value, current)) return;

            fields[propertyName] = value;
            source.RaisePropertyChanged(propertyName);
            callback?.Invoke(value);
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

        private static Action ForwardPropertyChanged(this INotifyPropertyChanged source, ImmutableStack<PropertyInfo> properties, INotifyPropertyChanged target, string targetPropertyName)
        {
            properties = properties.Pop(out PropertyInfo property);
            string sourcePropertyName = property.Name;

            Action unsubscribe = source.ForwardPropertyChanged(sourcePropertyName, target, targetPropertyName);

            if (properties.IsEmpty) return unsubscribe;

            INotifyPropertyChanged nestedSource = property.GetValue(source) as INotifyPropertyChanged;
            Action unsubscribeNested = nestedSource?.ForwardPropertyChanged(properties, target, targetPropertyName);
            unsubscribe += unsubscribeNested;

            WeakReference wr = new WeakReference(target);
            unsubscribe += source.SubscribeToPropertyChanged(sourcePropertyName, sender =>
            {
                unsubscribe -= unsubscribeNested;
                unsubscribeNested?.Invoke();
                if (!wr.IsAlive) return;
                INotifyPropertyChanged nestedSrc = property.GetValue(sender) as INotifyPropertyChanged;
                unsubscribeNested = nestedSrc?.ForwardPropertyChanged(properties, (INotifyPropertyChanged)wr.Target, targetPropertyName);
                unsubscribe += unsubscribeNested;
            });

            return unsubscribe;
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
                eventSource.CollectionChanged += (s, e) =>
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

                    foreach (var item in added)
                    {
                        if (filterPredicate?.Invoke(item) is false) continue;
                        subscriptions.Add(item, item.SubscribeToPropertyChanged(propertyName, action));
                    }
                };
            }

            var wr = new WeakReference(source);
            return () =>
            {
                if (subscriptions is null || !wr.IsAlive) return;

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
        }


        public static void RaisePropertyChanged(this INotifyPropertyChanged sender, [CallerMemberName] string propertyName = default)
        {
            if (sender == null) return;
            EventExtensions.RaiseEvent(sender, nameof(INotifyPropertyChanged.PropertyChanged), new PropertyChangedEventArgs(propertyName));
        }
    }
}
