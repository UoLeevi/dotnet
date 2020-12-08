using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace DotNetApp.Collections
{
    /// <summary> 
    /// A list binding that forwards changes made to observable source collections to a target list or lists.
    /// Binding can be configured to combine multiple source collections, filter, transform and sort list items
    /// and raise INotifyCollectionChanged.CollectionChanged events.
    /// </summary>
    /// <typeparam name="T">Type in which list binding stores items</typeparam>
    public class ListBinding<T> : IReadOnlyList<T>, INotifyPropertyChanged, INotifyCollectionChanged
    {
        private static object nullObject = new object();

        private class SourceItem
        {
            internal int ReferenceCount = 1;
            internal T Item;
        }

        private class Source
        {
            internal Source()
            {
                Map = new Dictionary<object, SourceItem>();
                List = new List<T>();
            }

            internal Source Previous;
            internal Dictionary<object, SourceItem> Map;
            internal List<T> List;
            internal Func<object, T> Convert;
            internal Func<object, bool> Filter;
        }

        private class Target
        {
            internal IList<T> List;
            internal SynchronizationContext Context;
        }

        private List<T> List { get; }
        private Dictionary<IList<T>, Target> Targets;
        private Dictionary<object, Source> Sources { get; }
        private Source Head { get; set; }
        private List<Func<T, bool>> Filters { get; set; }
        private IComparer<T> Comparer { get; set; }

        /// <summary>
        /// Gets the number of elements in the collection.
        /// </summary>
        public int Count => ((IReadOnlyCollection<T>)List).Count;

        /// <summary>
        /// Gets the element at the specified index in the read-only list.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>The element at the specified index in the read-only list.</returns>
        public T this[int index] => ((IReadOnlyList<T>)List)[index];

        /// <summary>
        /// Initializes a new instance of the ListBinding`1 class that is initially empty.
        /// </summary>
        public ListBinding()
        {
            List = new List<T>();
            Targets = new Dictionary<IList<T>, Target>();
            Sources = new Dictionary<object, Source>();
            Filters = new List<Func<T, bool>>();
        }

        /// <summary>
        /// Represents the method that handles the System.Collections.Specialized.INotifyCollectionChanged.CollectionChanged event.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Represents the method that will handle the System.ComponentModel.INotifyPropertyChanged.PropertyChanged event raised when a property is changed on a component.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that iterates through the collection.</returns>
        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)List).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)List).GetEnumerator();

        /// <summary>
        /// Adds a collection as a binding source.
        /// Current items in the source collection and future changes will be forwarded through this binding to target collections.
        /// </summary>
        /// <param name="collection">A collection instance to add as a binding source.</param>
        /// <param name="eventSource">An object instance which implement INotifyCollectionChanged interface.</param>
        /// <param name="convert">A transform function to apply to each source element.</param>
        /// <param name="filter">A filtering function to test each element for a condition.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ListBinding<T> AddSource<TSourceItem>(IEnumerable<TSourceItem> collection, INotifyCollectionChanged eventSource, Func<TSourceItem, T> convert, Func<TSourceItem, bool> filter)
        {
            if (collection is ISet<T> && Comparer == null)
            {
                Sort(Comparer<T>.Default);
            }

            Source source = new Source
            {
                Previous = Head
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

            Synchronize(eventSource, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, collection.ToList(), 0));

            eventSource.CollectionChanged += Synchronize;

            return this;
        }

        /// <summary>
        /// Adds a collection as a binding source.
        /// Current items in the source collection and future changes will be forwarded through this binding to target collections.
        /// </summary>
        /// <param name="collection">A collection instance to add as a binding source.</param>
        /// <param name="eventSource">An object instance which implement INotifyCollectionChanged interface.</param>
        /// <param name="convert">A transform function to apply to each source element.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ListBinding<T> AddSource<TSourceItem>(IEnumerable<TSourceItem> collection, INotifyCollectionChanged eventSource, Func<TSourceItem, T> convert)
            => AddSource(collection, eventSource, convert, null);

        /// <summary>
        /// Adds a collection as a binding source.
        /// Current items in the source collection and future changes will be forwarded through this binding to target collections.
        /// </summary>
        /// <param name="collection">A collection instance to add as a binding source. Collection should implement INotifyCollectionChanged interface.</param>
        /// <param name="convert">A transform function to apply to each source element.</param>
        /// <param name="filter">A filtering function to test each element for a condition.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ListBinding<T> AddSource<TSourceItem>(IEnumerable<TSourceItem> collection, Func<TSourceItem, T> convert, Func<TSourceItem, bool> filter)
            => AddSource(collection, (INotifyCollectionChanged)collection, convert, filter);

        /// <summary>
        /// Adds a collection as a binding source.
        /// Current items in the source collection and future changes will be forwarded through this binding to target collections.
        /// </summary>
        /// <param name="collection">A collection instance to add as a binding source. Collection should implement INotifyCollectionChanged interface.</param>
        /// <param name="convert">A transform function to apply to each source element.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ListBinding<T> AddSource<TSourceItem>(IEnumerable<TSourceItem> collection, Func<TSourceItem, T> convert)
            => AddSource(collection, (INotifyCollectionChanged)collection, convert);

        /// <summary>
        /// Adds a collection as a binding source.
        /// Current items in the source collection and future changes will be forwarded through this binding to target collections.
        /// </summary>
        /// <param name="collection">A collection instance to add as a binding source. Collection should implement INotifyCollectionChanged interface.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ListBinding<T> AddSource(IEnumerable<T> collection)
            => AddSource(collection, null);

        /// <summary>
        /// Stop binding for a binding source collection. 
        /// </summary>
        /// <param name="eventSource">Object instance which was previously added as a binding source by calling AddSource.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ListBinding<T> RemoveSource(INotifyCollectionChanged eventSource)
        {
            if (Sources.TryGetValue(eventSource, out Source source))
            {
                eventSource.CollectionChanged -= Synchronize;
                Synchronize(eventSource, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                Sources.Remove(eventSource);

                if (Sources.Values.FirstOrDefault(s => s.Previous == source) is Source nextSource)
                {
                    nextSource.Previous = source.Previous;
                }

                if (Head == source)
                {
                    Head = source.Previous;
                }
            }

            return this;
        }

        /// <summary>
        /// Adds an additional filtering to list binding.
        /// Items not fulfilling a predicate will be removed from the binding targets and new items not fulfilling the predicate will not be added.
        /// </summary>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ListBinding<T> Where(Func<T, bool> predicate)
        {
            foreach (Source source in Sources.Values)
            {
                var excluded = source.Map.Where(kvp => !predicate(kvp.Value.Item)).ToList();

                foreach (var kvp in excluded)
                {
                    source.Map.Remove(kvp.Key);
                    SourceItem sourceItem = kvp.Value;

                    while (--sourceItem.ReferenceCount >= 0)
                    {
                        source.List.Remove(sourceItem.Item);
                        Remove(sourceItem.Item);
                    }
                }
            }

            Filters.Add(predicate);
            return this;
        }

        /// <summary>
        /// Sorts the elements of the list binding in ascending order.
        /// </summary>
        /// <typeparam name="TKey">The type of the key returned by keySelector.</typeparam>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">An IComparer&lt;T&gt; to compare keys.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ListBinding<T> OrderBy<TKey>(Func<T, TKey> keySelector, IComparer<TKey> comparer)
        {
            Sort(Comparer<T>.Create((x, y) => comparer.Compare(keySelector(x), keySelector(y))));
            return this;
        }

        /// <summary>
        /// Sorts the elements of the list binding in ascending order.
        /// </summary>
        /// <typeparam name="TKey">The type of the key returned by keySelector.</typeparam>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ListBinding<T> OrderBy<TKey>(Func<T, TKey> keySelector)
            => OrderBy(keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Sorts the elements of the list binding in descending order.
        /// </summary>
        /// <typeparam name="TKey">The type of the key returned by keySelector.</typeparam>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <param name="comparer">An IComparer&lt;T&gt; to compare keys.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ListBinding<T> OrderByDescending<TKey>(Func<T, TKey> keySelector, IComparer<TKey> comparer)
        {
            Sort(Comparer<T>.Create((x, y) => comparer.Compare(keySelector(y), keySelector(x))));
            return this;
        }

        /// <summary>
        /// Sorts the elements of the list binding in descending order.
        /// </summary>
        /// <typeparam name="TKey">The type of the key returned by keySelector.</typeparam>
        /// <param name="keySelector">A function to extract a key from an element.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ListBinding<T> OrderByDescending<TKey>(Func<T, TKey> keySelector)
            => OrderByDescending(keySelector, Comparer<TKey>.Default);

        /// <summary>
        /// Creates a new instance of the ListBinding`1 class that uses this argument as a binding source after applying a transformation.
        /// </summary>
        /// <typeparam name="R">The type of elements in the resulting list.</typeparam>
        /// <param name="selector">A transform function to apply to each source element.</param>
        /// <returns>A new instance of ListBinding`1 class.</returns>
        public ListBinding<R> Select<R>(Func<T, R> selector)
        {
            return new ListBinding<R>()
                .AddSource(List, this, selector);
        }

        /// <summary>
        /// Adds a new list as binding target for this list binding instance.
        /// Changes to source lists will be forwarded to this list. Current items in the list binding will be inserted to the target list.
        /// </summary>
        /// <param name="target">A list instance to add as binding target.</param>
        /// <param name="context">Updates to target list are perfomed in this synchronization context. Current synchronization context is used by default.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ListBinding<T> AddTarget(IList<T> target, SynchronizationContext context = default)
        {
            if (context == default)
            {
                context = SynchronizationContext.Current;
            }

            foreach (var item in List)
            {
                target.Add(item);
            }

            Targets.Add(target, new Target
            {
                List = target,
                Context = context
            });
            return this;
        }

        /// <summary>
        /// Stop binding for a target list. 
        /// </summary>
        /// <param name="target">List instance which was previously add as a binding target by calling AddTarget.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ListBinding<T> RemoveTarget(IList<T> target)
        {
            Targets.Remove(target);
            return this;
        }

        private IEnumerable<T> MapNewItems(Source source, IEnumerable items)
        {
            if (items == null) yield break;

            foreach (object item in items)
            {
                if (source.Map.TryGetValue(item ?? nullObject, out SourceItem sourceItem))
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
                    source.Map.Add(item ?? nullObject, sourceItem);
                    yield return targetItem;
                }
            }
        }

        private IEnumerable<T> MapOldItems(Source source, IEnumerable items)
        {
            if (items == null) yield break;

            foreach (object item in items)
            {
                if (source.Map.TryGetValue(item ?? nullObject, out var sourceItem))
                {
                    --sourceItem.ReferenceCount;

                    if (sourceItem.ReferenceCount == 0)
                    {
                        source.Map.Remove(item ?? nullObject);
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
                if (source.Map.TryGetValue(item ?? nullObject, out var sourceItem))
                {
                    yield return sourceItem.Item;
                }
            }
        }

        private void Sort(IComparer<T> comparer)
        {
            Comparer = comparer;

            var target = List.ToList();

            RemoveRange(0, List);

            target.Sort(comparer);

            InsertRange(0, target);
        }

        private void InsertSorted(T item)
        {
            int index = -1;

            do
            {
                ++index;
                int count = List.Count - index;
                index = List.BinarySearch(index, count, item, Comparer);
            } while (index >= 0);

            index = ~index;

            List.Insert(index, item);

            foreach (var target in Targets.Values)
            {
                var context = target.Context;
                var list = target.List;

                if (context == SynchronizationContext.Current)
                {
                    list.Insert(index, item);
                }
                else
                {
                    context.Send(InsertCallback, (list, index, item));
                }
            }

            if (CollectionChanged != null)
            {
                var newItems = new List<T> { item };
                var eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems, index);

                CollectionChanged.Invoke(this, eventArgs);
            }

            NotifyCountChanged();
        }

        private void Remove(T item)
        {
            int index = List.IndexOf(item);
            List.RemoveAt(index);

            foreach (var target in Targets.Values)
            {
                var context = target.Context;
                var list = target.List;

                if (context == SynchronizationContext.Current)
                {
                    list.RemoveAt(index);
                }
                else
                {
                    context.Send(RemoveAtCallback, (list, index));
                }
            }

            if (CollectionChanged != null)
            {
                var oldItems = new List<T> { item };
                var eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItems, index);
                CollectionChanged.Invoke(this, eventArgs);
            }

            NotifyCountChanged();
        }

        private void InsertRange(int index, List<T> newItems)
        {
            List.InsertRange(index, newItems);

            foreach (var target in Targets.Values)
            {
                var context = target.Context;
                var list = target.List;

                if (context == SynchronizationContext.Current)
                {
                    for (int i = 0; i < newItems.Count; ++i)
                    {
                        T item = newItems[i];
                        list.Insert(index + i, item);
                    }
                }
                else
                {
                    context.Send(InsertRangeCallback, (list, index, newItems));
                }
            }

            if (CollectionChanged != null)
            {
                var eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems, index);
                CollectionChanged.Invoke(this, eventArgs);
            }

            NotifyCountChanged();
        }

        private void MoveRange(int oldIndex, int newIndex, List<T> items)
        {
            int count = items.Count;

            List.RemoveRange(oldIndex, count);
            List.InsertRange(newIndex, items);

            foreach (var target in Targets.Values)
            {
                var context = target.Context;
                var list = target.List;

                if (context == SynchronizationContext.Current)
                {
                    for (int i = oldIndex + count - 1; i >= oldIndex; --i)
                    {
                        list.RemoveAt(i);
                    }

                    for (int i = 0; i < count; ++i)
                    {
                        list.Insert(newIndex + i, items[i]);
                    }
                }
                else
                {
                    context.Send(MoveRangeCallback, (list, oldIndex, newIndex, count, items));
                }
            }

            if (CollectionChanged != null)
            {
                var eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, items, oldIndex, newIndex);
                CollectionChanged.Invoke(this, eventArgs);
            }
        }

        private void RemoveRange(int index, List<T> oldItems)
        {
            int count = oldItems.Count;
            List.RemoveRange(index, count);

            foreach (var target in Targets.Values)
            {
                var context = target.Context;
                var list = target.List;

                if (context == SynchronizationContext.Current)
                {
                    for (int i = index + count - 1; i >= index; --i)
                    {
                        list.RemoveAt(i);
                    }
                }
                else
                {
                    context.Send(RemoveRangeCallback, (list, index, count));
                }
            }

            if (CollectionChanged != null)
            {
                var eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItems, index);
                CollectionChanged.Invoke(this, eventArgs);
            }

            NotifyCountChanged();
        }

        private void ReplaceRange(int index, List<T> newItems, List<T> oldItems)
        {
            List.RemoveRange(index, newItems.Count);
            List.InsertRange(index, newItems);

            foreach (var target in Targets.Values)
            {
                var context = target.Context;
                var list = target.List;

                if (context == SynchronizationContext.Current)
                {
                    for (int i = 0; i < newItems.Count; ++i)
                    {
                        list.RemoveAt(index);
                    }

                    for (int i = 0; i < newItems.Count; ++i)
                    {
                        list.Insert(index + i, newItems[i]);
                    }
                }
                else
                {
                    context.Send(ReplaceRangeCallback, (list, index, newItems.Count, newItems));
                }
            }

            if (CollectionChanged != null)
            {
                var eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItems, oldItems, index);
                CollectionChanged.Invoke(this, eventArgs);
            }
        }

        private void Clear()
        {
            if (List.Count == 0) return;

            List.Clear();

            foreach (var target in Targets.Values)
            {
                var context = target.Context;
                var list = target.List;

                if (context == SynchronizationContext.Current)
                {
                    list.Clear();
                }
                else
                {
                    context.Send(ClearCallback, list);
                }
            }

            if (CollectionChanged != null)
            {
                var eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                CollectionChanged.Invoke(this, eventArgs);
            }

            NotifyCountChanged();
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

        private static void InsertCallback(object state)
        {
            (IList<T> list, int index, T item) = (ValueTuple<IList<T>, int, T>)state;
            list.Insert(index, item);
        }

        private static void RemoveAtCallback(object state)
        {
            (IList<T> list, int index) = (ValueTuple<IList<T>, int>)state;
            list.RemoveAt(index);
        }

        private static void InsertRangeCallback(object state)
        {
            (IList<T> list, int index, IEnumerable<T> items) = (ValueTuple<IList<T>, int, List<T>>)state;

            foreach (T item in items)
            {
                list.Insert(index++, item);
            }
        }

        private static void RemoveRangeCallback(object state)
        {
            (IList<T> list, int index, int count) = (ValueTuple<IList<T>, int, int>)state;

            for (int i = index + count - 1; i >= index; --i)
            {
                list.RemoveAt(i);
            }
        }

        private static void MoveRangeCallback(object state)
        {
            (IList<T> list, int oldIndex, int newIndex, int count, IEnumerable<T> items) = (ValueTuple<IList<T>, int, int, int, List<T>>)state;

            for (int i = oldIndex + count - 1; i >= oldIndex; --i)
            {
                list.RemoveAt(i);
            }

            foreach (T item in items)
            {
                list.Insert(newIndex++, item);
            }
        }

        private static void ReplaceRangeCallback(object state)
        {
            (IList<T> list, int index, int count, IEnumerable<T> items) = (ValueTuple<IList<T>, int, int, List<T>>)state;

            for (int i = index + count - 1; i >= index; --i)
            {
                list.RemoveAt(i);
            }

            foreach (T item in items)
            {
                list.Insert(index++, item);
            }
        }

        private static void ClearCallback(object state)
        {
            IList<T> list = (IList<T>)state;
            list.Clear();
        }

        private void NotifyCountChanged()
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
    }
}
