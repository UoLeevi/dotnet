using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace DotNetApp.Extensions
{
    public static class EventExtensions
    {
        private readonly static ConcurrentDictionary<(Type type, string eventName), FieldInfo> eventFields;

        static EventExtensions()
        {
            eventFields = new ConcurrentDictionary<(Type, string), FieldInfo>();
        }

        private static FieldInfo GetEventField(this object sender, string eventName)
        {
            Type sourceType = sender.GetType();

            return eventFields.GetOrAdd((sourceType, eventName), GetEventField);

            FieldInfo GetEventField((Type, string) _)
            {
                return sourceType.GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            }
        }

        public static void RaiseEvent<TEventArgs>(object sender, string eventName, TEventArgs eventArgs)
        {
            object[] parameters = new object[] { sender, eventArgs };

            if (GetEventField(sender, eventName).GetValue(sender) is MulticastDelegate eventDelegate)
            {
                foreach (Delegate handler in eventDelegate.GetInvocationList())
                {
                    handler.Method.Invoke(handler.Target, parameters);
                }
            }
        }
    }

}
