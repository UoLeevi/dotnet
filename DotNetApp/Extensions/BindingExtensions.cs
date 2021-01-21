using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace DotNetApp.Extensions
{
    public static class BindingExtensions
    {
        public static Action Bind<TSource, TProperty>(
            this TSource source,
            Expression<Func<TSource, TProperty>> property,
            Action<TProperty> action,
            SynchronizationContext context = default)
            where TSource : INotifyPropertyChanged
        {
            MemberExpression propertyAccess = (MemberExpression)property.Body;
            string propertyName = propertyAccess.Member.Name;
            Func<TSource, TProperty> getValue = property.Compile();
            SendOrPostCallback callback = state => action((TProperty)state);


            if (context is null)
            {
                context = SynchronizationContext.Current;
            }

            Action<TProperty> execute = (value) =>
            {
                if (context == SynchronizationContext.Current)
                {
                    action(value);
                }
                else
                {
                    context.Send(callback, value);
                }
            };

            execute(getValue(source));
            return source.SubscribeToPropertyChanged(propertyName, s => execute(getValue(source)));
        }

        public static Action Bind<TSource, TProperty>(
            this TSource source,
            Expression<Func<TSource, TProperty>> property,
            Action<TProperty, TProperty> action,
            SynchronizationContext context = default)
            where TSource : INotifyPropertyChanged
        {
            MemberExpression propertyAccess = (MemberExpression)property.Body;
            string propertyName = propertyAccess.Member.Name;
            Func<TSource, TProperty> getValue = property.Compile();
            TProperty previousValue = getValue(source);

            SendOrPostCallback callback = state =>
            {
                var (oldValue, newValue) = ((TProperty, TProperty))state;
                action(oldValue, newValue);
            };


            if (context is null)
            {
                context = SynchronizationContext.Current;
            }

            Action<TProperty, TProperty> execute = (oldValue, newValue) =>
            {
                if (context == SynchronizationContext.Current)
                {
                    action(oldValue, newValue);
                }
                else
                {
                    context.Send(callback, (oldValue, newValue));
                }
            };

            execute(previousValue, previousValue);

            return source.SubscribeToPropertyChanged(propertyName, s =>
            {
                TProperty oldValue = previousValue;
                previousValue = getValue(s);
                execute(oldValue, previousValue);
            });
        }
    }
}
