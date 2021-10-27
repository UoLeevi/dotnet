using System.Collections.Specialized;
using DotNetApp.Extensions;

namespace DotNetApp.Collections
{
    public static class NotifyCollectionChangedExtensions
    {
        public static void RaiseCollectionChanged(this INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs args)
        {
            if (sender == null) return;
            EventExtensions.RaiseEvent(sender, nameof(INotifyCollectionChanged.CollectionChanged), args);
        }
    }
}
