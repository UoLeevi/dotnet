using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace DotNetApp.Extensions
{
    public static class BindingExtensions
    {
        public static Action Bind<TSource, TProperty>(this TSource source, Expression<Func<TSource, TProperty>> property, Action<TProperty> action, SynchronizationContext context = default)
            where TSource : INotifyPropertyChanged
        {
            MemberExpression propertyAccess = (MemberExpression)property.Body;
            PropertyInfo propertyInfo = (PropertyInfo)propertyAccess.Member;
            Func<TSource, TProperty> getValue = s => (TProperty)propertyInfo.GetValue(s);
            SendOrPostCallback callback = state => action((TProperty)state);

            if (context is null)
            {
                context = SynchronizationContext.Current;
            }

            action = value =>
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

            action(getValue(source));
            return source.SubscribeToPropertyChanged(propertyInfo.Name, s => action(getValue(s)));
        }
    }

}
