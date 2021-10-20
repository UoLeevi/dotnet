using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Threading;

namespace DotNetApp.Extensions
{
    public static class BindingExtensions
    {
        public static Action Bind<TSource, TProperty>(
            this TSource source,
            string propertyName,
            Action<TSource, TProperty, TProperty> action,
            SynchronizationContext context = null)
            where TSource : class, INotifyPropertyChanged
        {
            TProperty previousValue = Dynamic.GetPropertyOrFieldValue<TSource, TProperty>(source, propertyName);

            SendOrPostCallback callback;
            Action<TSource, TProperty, TProperty> execute = action;

            if (context is null)
            {
                execute = action;
            }
            else
            {
                callback = state =>
                {
                    var (src, oldValue, newValue) = ((TSource, TProperty, TProperty))state;
                    action(src, oldValue, newValue);
                };

                execute = (s, oldValue, newValue) =>
                {
                    if (context == SynchronizationContext.Current)
                    {
                        action(s, oldValue, newValue);
                    }
                    else
                    {
                        context.Send(callback, (s, oldValue, newValue));
                    }
                };
            }

            execute(source, previousValue, previousValue);
            source.PropertyChanged += PropertyChanged;
            WeakReference<TSource> wr = new WeakReference<TSource>(source);
            source = null;

            return () =>
            {
                if (wr == null) return;

                if (wr.TryGetTarget(out TSource s))
                {
                    s.PropertyChanged -= PropertyChanged;
                }

                wr = null;
            };

            void PropertyChanged(object sender, PropertyChangedEventArgs eventArgs)
            {
                if (eventArgs.PropertyName != propertyName) return;

                TSource s = (TSource)sender;
                TProperty oldValue = previousValue;

                previousValue = eventArgs is PropertyChangedExtendedEventArgs<TProperty> extendedEventArgs
                    ? extendedEventArgs.NewValue
                    : Dynamic.GetPropertyOrFieldValue<TSource, TProperty>(s, propertyName);

                execute(s, oldValue, previousValue);
            }
        }

        public static Action Bind<TSource, TProperty>(
            this TSource source,
            string propertyName,
            Action<TProperty> action,
            SynchronizationContext context = null)
            where TSource : class, INotifyPropertyChanged
            => Bind(source, propertyName, (TSource s, TProperty oldValue, TProperty newValue) => action(newValue), context);

        public static Action Bind<TSource, TProperty>(
            this TSource source,
            string propertyName,
            Action<TSource, TProperty> action,
            SynchronizationContext context = null)
            where TSource : class, INotifyPropertyChanged
            => Bind(source, propertyName, (TSource s, TProperty oldValue, TProperty newValue) => action(s, newValue), context);

        public static Action Bind<TSource, TProperty>(
            this TSource source,
            Expression<Func<TSource, TProperty>> property,
            Action<TProperty> action,
            SynchronizationContext context = null)
            where TSource : class, INotifyPropertyChanged
            => Bind(source, property, (s, oldValue, newValue) => action(newValue), context);

        public static Action Bind<TSource, TProperty>(
            this TSource source,
            Expression<Func<TSource, TProperty>> property,
            Action<TSource, TProperty> action,
            SynchronizationContext context = null)
            where TSource : class, INotifyPropertyChanged
            => Bind(source, property, (s, oldValue, newValue) => action(s, newValue), context);

        public static Action Bind<TSource, TProperty>(
            this TSource source,
            Expression<Func<TSource, TProperty>> property,
            Action<TSource, TProperty, TProperty> action,
            SynchronizationContext context = null)
            where TSource : class, INotifyPropertyChanged
            => Bind(source, ((MemberExpression)property.Body).Member.Name, action, context);
    }
}
