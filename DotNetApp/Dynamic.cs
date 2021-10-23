using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DotNetApp
{
    public static class Dynamic
    {
        private static readonly MethodInfo actionMethodInfo;

        static Dynamic()
        {
            actionMethodInfo = typeof(Action).GetMethod(nameof(Action.Invoke));
        }

        private static class Store
        {
            internal static readonly ConcurrentDictionary<(Type type, string eventName), (EventInfo eventInfo, ParameterExpression[] parameters)> Events = new ConcurrentDictionary<(Type, string), (EventInfo, ParameterExpression[])>();
            internal static (EventInfo eventInfo, ParameterExpression[] parameters) CreateEventInfo((Type type, string eventName) key)
            {
                (Type type, string eventName) = key;
                var eventInfo = type.GetEvent(eventName);
                var methodInfo = eventInfo.EventHandlerType.GetMethod(nameof(Action.Invoke));
                var parameters = methodInfo
                    .GetParameters()
                    .Select(p => Expression.Parameter(p.ParameterType))
                    .ToArray();

                return (eventInfo, parameters);
            }
        }

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

        public static Action SubscribeToEvent<TSource>(TSource source, string eventName, Action action)
            where TSource : class
        {
            var (eventInfo, parameters) = Store.Events.GetOrAdd((source.GetType(), eventName), Store.CreateEventInfo);
            var call = Expression.Call(Expression.Constant(action), actionMethodInfo);
            var lambda = Expression.Lambda(eventInfo.EventHandlerType, call, parameters);
            var handler = lambda.Compile();
            eventInfo.AddEventHandler(source, handler);
            var wrSource = new WeakReference<TSource>(source);
            var wrHandler = new WeakReference<Delegate>(handler);
            return () =>
            {
                if (!wrSource.TryGetTarget(out var s) || !wrHandler.TryGetTarget(out var h)) return;
                eventInfo.RemoveEventHandler(s, h);
            };
        }
    }
}
