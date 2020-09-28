using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace DotNetApp.Collections
{
    /// <summary>
    /// An intermediary that forwards changes made to observable source collections to a target list or lists.
    /// Binding can be configured to combine multiple source collections, filter, transform and sort list items
    /// and raise INotifyCollectionChanged.CollectionChanged events.
    /// </summary>
    /// <typeparam name="T">Type in which list binding stores items</typeparam>
    public class ListBinding<T> : INotifyCollectionChanged
    {
        private class SourceItem
        {
            internal int ReferenceCount = 1;
            internal T Item;
        }

        private class Source
        {

            public Source()
            {
                Map = new Dictionary<object, SourceItem>();
                List = new List<T>();
            }

            internal Source Previous;
            internal Dictionary<object, SourceItem> Map;
            internal List<T> List;
            internal INotifyCollectionChanged EventSource;
            internal Func<object, T> Convert;
            internal Func<object, bool> Filter;
        }

        private List<T> Target { get; }
        private List<IList<T>> ListTargets;
        private List<INotifyCollectionChanged> EventTargets;

        private Source Head { get; set; }
        private Dictionary<object, Source> Sources { get; }
        private List<Func<T, bool>> Filters { get; set; }
        private IComparer<T> Comparer { get; set; }

        public ListBinding()
        {
            Target = new List<T>();
            ListTargets = new List<IList<T>>();
            EventTargets = new List<INotifyCollectionChanged>();
            Sources = new Dictionary<object, Source>();
            Filters = new List<Func<T, bool>>();
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public ListBinding<T> AddSource<TSourceItem>(IEnumerable<TSourceItem> collection, INotifyCollectionChanged eventSource, Func<TSourceItem, T> convert, Func<TSourceItem, bool> filter)
        {
            if (collection is ISet<T>)
            {
                Comparer = Comparer<T>.Default;
            }

            Source source = new Source
            {
                Previous = Head,
                EventSource = eventSource
            };

            Head = source;

            if (convert != null)
            {
                source.Convert = item => convert((TSourceItem)item);
            }

            if (filter != null)
            {
                source.Filter = item => filter((TSourceItem)item);
            }
            
            Sources.Add(eventSource, source);

            Synchronize(eventSource, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, collection.ToList(), Target.Count));

            eventSource.CollectionChanged += Synchronize;

            return this;
        }

        public ListBinding<T> AddSource<TSourceItem>(IEnumerable<TSourceItem> collection, INotifyCollectionChanged eventSource, Func<TSourceItem, T> convert)
            => AddSource(collection, eventSource, convert, null);

        public ListBinding<T> AddSource<TSourceItem>(IEnumerable<TSourceItem> collection, Func<TSourceItem, T> convert, Func<TSourceItem, bool> filter)
            => AddSource(collection, (INotifyCollectionChanged)collection, convert, filter);

        public ListBinding<T> AddSource<TSourceItem>(IEnumerable<TSourceItem> collection, Func<TSourceItem, T> convert)
            => AddSource(collection, (INotifyCollectionChanged)collection, convert);

        public ListBinding<T> AddSource(IEnumerable<T> collection)
            => AddSource(collection, null);

        public ListBinding<T> RemoveSource(INotifyCollectionChanged eventSource)
        {
            if (Sources.TryGetValue(eventSource, out Source source))
            {
                Sources.Remove(eventSource);
                eventSource.CollectionChanged -= Synchronize;
                Synchronize(eventSource, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

                var nextSource = Sources.Values.FirstOrDefault(s => s.Previous == source);
                nextSource.Previous = source.Previous;

                if (Head == source)
                {
                    Head = source.Previous;
                }
            }

            return this;
        }

        public ListBinding<T> Where(Func<T, bool> predicate)
        {
            Filters.Add(predicate);
            return this;
        }

        public ListBinding<T> OrderBy<TKey>(Func<T, TKey> keySelector, IComparer<TKey> comparer)
        {
            Comparer = Comparer<T>.Create((x, y) => comparer.Compare(keySelector(x), keySelector(y)));
            return this;
        }

        public ListBinding<T> OrderBy<TKey>(Func<T, TKey> keySelector)
            => OrderBy(keySelector, Comparer<TKey>.Default);

        public ListBinding<T> OrderByDescending<TKey>(Func<T, TKey> keySelector, IComparer<TKey> comparer)
        {
            Comparer = Comparer<T>.Create((x, y) => comparer.Compare(keySelector(y), keySelector(x)));
            return this;
        }

        public ListBinding<T> OrderByDescending<TKey>(Func<T, TKey> keySelector)
            => OrderByDescending(keySelector, Comparer<TKey>.Default);

        public ListBinding<R> Select<R>(Func<T, R> selector)
        {
            return new ListBinding<R>()
                .AddSource(Target, this, selector);
        }

        public ListBinding<T> AddListTarget(IList<T> target)
        {
            foreach (var item in Target)
            {
                target.Add(item);
            }

            ListTargets.Add(target);
            return this;
        }

        public ListBinding<T> AddEventTarget(INotifyCollectionChanged target)
        {
            EventTargets.Add(target);
            return this;
        }

        public ListBinding<T> RemoveListTarget(IList<T> target)
        {
            ListTargets.Remove(target);
            return this;
        }

        public ListBinding<T> RemoveEventTarget(INotifyCollectionChanged target)
        {
            EventTargets.Remove(target);
            return this;
        }

        private IEnumerable<T> MapNewItems(Source source, IEnumerable items)
        {
            if (items == null) yield break;

            foreach (object item in items)
            {

                if (source.Map.TryGetValue(item, out SourceItem sourceItem))
                {
                    ++sourceItem.ReferenceCount;
                    yield return sourceItem.Item;
                }
                else
                {
                    if (source.Filter?.Invoke(item) == false) continue;

                    T targetItem = source.Convert == null ? (T)item : source.Convert(item);

                    if (Filters.Any(filter => !filter(targetItem))) continue;

                    sourceItem = new SourceItem { Item = targetItem };
                    source.Map.Add(item, sourceItem);
                    yield return targetItem;
                }
            }
        }

        private IEnumerable<T> MapOldItems(Source source, IEnumerable items)
        {
            if (items == null) yield break;

            foreach (object item in items)
            {
                if (source.Map.TryGetValue(item, out var sourceItem))
                {
                    --sourceItem.ReferenceCount;

                    if (sourceItem.ReferenceCount == 0)
                    {
                        source.Map.Remove(item);
                    }

                    yield return sourceItem.Item;
                }
            }
        }

        private IEnumerable<T> MapItems(Source source, IEnumerable items)
        {
            if (items == null) yield break;

            foreach (object item in items)
            {
                if (source.Map.TryGetValue(item, out var sourceItem))
                {
                    yield return sourceItem.Item;
                }
            }
        }

        private void InsertSorted(T item)
        {
            int index = -1;

            do
            {
                ++index;
                int count = Target.Count - index;
                index = Target.BinarySearch(index, count, item, Comparer);
            } while (index >= 0);

            index = ~index;

            Target.Insert(index, item);

            foreach (var target in ListTargets)
            {
                target.Insert(index, item);
            }

            var newItems = new List<T> { item };
            var eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems, index);

            foreach (var eventTarget in EventTargets)
            {
                eventTarget.OnCollectionChanged(eventArgs);
            }

            CollectionChanged?.Invoke(this, eventArgs);
        }

        private void Remove(T item)
        {
            int index = Target.IndexOf(item);
            Target.RemoveAt(index);

            foreach (var target in ListTargets)
            {
                target.RemoveAt(index);
            }

            var oldItems = new List<T> { item };
            var eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItems, index);

            foreach (var eventTarget in EventTargets)
            {
                eventTarget.OnCollectionChanged(eventArgs);
            }

            CollectionChanged?.Invoke(this, eventArgs);
        }

        private void InsertRange(int index, List<T> newItems)
        {
            Target.InsertRange(index, newItems);

            foreach (var target in ListTargets)
            {
                for (int i = 0; i < newItems.Count; ++i)
                {
                    T item = newItems[i];
                    target.Insert(index + i, item);
                }
            }

            var eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems, index);

            foreach (var eventTarget in EventTargets)
            {
                eventTarget.OnCollectionChanged(eventArgs);
            }

            CollectionChanged?.Invoke(this, eventArgs);
        }

        private void MoveRange(int oldIndex, int newIndex, List<T> items)
        {
            int count = items.Count;

            Target.RemoveRange(oldIndex, count);
            Target.InsertRange(newIndex, items);

            foreach (var target in ListTargets)
            {
                for (int i = 0; i < count; ++i)
                {
                    target.RemoveAt(oldIndex);
                }

                for (int i = 0; i < count; ++i)
                {
                    target.Insert(newIndex + i, items[i]);
                }
            }

            var eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, items, oldIndex, newIndex);

            foreach (var eventTarget in EventTargets)
            {
                eventTarget.OnCollectionChanged(eventArgs);
            }

            CollectionChanged?.Invoke(this, eventArgs);
        }

        private void RemoveRange(int index, List<T> oldItems)
        {
            int count = oldItems.Count;
            Target.RemoveRange(index, count);

            foreach (var target in ListTargets)
            {
                for (int i = 0; i < count; ++i)
                {
                    target.RemoveAt(index);
                }
            }

            var eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItems, index);

            foreach (var eventTarget in EventTargets)
            {
                eventTarget.OnCollectionChanged(eventArgs);
            }

            CollectionChanged?.Invoke(this, eventArgs);
        }

        private void ReplaceRange(int index, List<T> newItems, List<T> oldItems)
        {
            Target.RemoveRange(index, newItems.Count);
            Target.InsertRange(index, newItems);

            foreach (var target in ListTargets)
            {
                for (int i = 0; i < newItems.Count; ++i)
                {
                    target.RemoveAt(index);
                }

                for (int i = 0; i < newItems.Count; ++i)
                {
                    target.Insert(index + i, newItems[i]);
                }
            }

            var eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItems, oldItems, index);

            foreach (var eventTarget in EventTargets)
            {
                eventTarget.OnCollectionChanged(eventArgs);
            }

            CollectionChanged?.Invoke(this, eventArgs);
        }

        private void Clear()
        {
            Target.Clear();

            foreach (var target in ListTargets)
            {
                target.Clear();
            }

            var eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);

            foreach (var eventTarget in EventTargets)
            {
                eventTarget.OnCollectionChanged(eventArgs);
            }

            CollectionChanged?.Invoke(this, eventArgs);
        }

        private void Synchronize(object sender, NotifyCollectionChangedEventArgs e)
        {
            Source source = Sources[sender];

            List<T> newTargetItems = MapNewItems(source, e.NewItems).ToList();
            List<T> oldTargetItems = e.Action == NotifyCollectionChangedAction.Reset
                ? source.List.ToList()
                : e.Action == NotifyCollectionChangedAction.Move
                    ? MapItems(source, e.OldItems).ToList()
                    : MapOldItems(source, e.OldItems).ToList();

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewStartingIndex < 0 || Filters.Any() || source.Filter != null)
                    {
                        source.List.AddRange(newTargetItems);
                        break;
                    }

                    source.List.InsertRange(e.NewStartingIndex, newTargetItems);

                    break;

                case NotifyCollectionChangedAction.Move:
                    if (Comparer != null || Filters.Any() || source.Filter != null) return;

                    source.List.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                    source.List.InsertRange(e.NewStartingIndex, oldTargetItems);

                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldStartingIndex < 0 || Filters.Any() || source.Filter != null)
                    {
                        foreach (var item in oldTargetItems)
                        {
                            source.List.Remove(item);
                        }

                        break;
                    }

                    source.List.RemoveRange(e.OldStartingIndex, e.OldItems.Count);

                    break;

                case NotifyCollectionChangedAction.Replace:
                    if (Filters.Any() || source.Filter != null)
                    {
                        foreach (var item in oldTargetItems)
                        {
                            source.List.Remove(item);
                        }

                        if (e.NewStartingIndex < 0 || Filters.Any() || source.Filter != null)
                        {
                            source.List.AddRange(newTargetItems);
                        }

                        break;
                    }

                    source.List.RemoveRange(e.OldStartingIndex, e.NewItems.Count);
                    source.List.InsertRange(e.NewStartingIndex, newTargetItems);

                    break;

                case NotifyCollectionChangedAction.Reset:
                    source.List.Clear();

                    break;
            }

            int offset = 0;

            while (source.Previous != null)
            {
                source = source.Previous;
                offset += source.List.Count;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (Comparer == null && e.NewStartingIndex >= 0 && !Filters.Any() && !Sources.Values.Any(s => s.Filter != null))
                    {
                        int index = offset + e.NewStartingIndex;
                        InsertRange(index, newTargetItems);
                        break;
                    }

                    foreach (var item in newTargetItems)
                    {
                        InsertSorted(item);
                    }

                    break;

                case NotifyCollectionChangedAction.Move:
                    if (Comparer == null)
                    {
                        int newIndex = offset + e.NewStartingIndex;
                        int oldIndex = offset + e.OldStartingIndex;
                        MoveRange(oldIndex, newIndex, oldTargetItems);
                        break;
                    }

                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (Comparer == null && e.OldStartingIndex >= 0 && !Sources.Values.Any(s => s.Filter != null))
                    {
                        int oldIndex = offset + e.OldStartingIndex;
                        RemoveRange(oldIndex, oldTargetItems);
                        break;
                    }

                    foreach (var item in oldTargetItems)
                    {
                        Remove(item);
                    }

                    break;

                case NotifyCollectionChangedAction.Replace:
                    if (Comparer == null && e.OldStartingIndex >= 0 && !Sources.Values.Any(s => s.Filter != null))
                    {
                        int index = offset + e.OldStartingIndex;
                        ReplaceRange(index, newTargetItems, oldTargetItems);
                        break;
                    }

                    foreach (var item in oldTargetItems)
                    {
                        Remove(item);
                    }

                    foreach (var item in newTargetItems)
                    {
                        InsertSorted(item);
                    }

                    break;

                case NotifyCollectionChangedAction.Reset:
                    if (Sources.Count == 1)
                    {
                        Clear();
                        break;
                    }
                    
                    if (Comparer == null && !Sources.Values.Any(s => s.Filter != null))
                    {
                        RemoveRange(offset, oldTargetItems);
                        break;
                    }

                    foreach (var item in oldTargetItems)
                    {
                        Remove(item);
                    }

                    break;
            }
        }
    }
}
