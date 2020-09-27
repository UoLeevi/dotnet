using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace DotNetApp.Collections
{
    /// <summary>
    /// An intermediary that forwards changes made to observable source collections to a target list or lists.
    /// Synchronization can:
    /// - combine multiple source collections
    /// - filter source collection items
    /// - transform source collection items
    /// - sort target list items
    /// - raise INotifyCollectionChanged.CollectionChanged events
    /// </summary>
    /// <typeparam name="T">Type of list items</typeparam>
    public class ListSynchronizer<T> : ListSynchronizer<T, T>
    {
        public ListSynchronizer(
            params IEnumerable<T>[] sources) : this(sources.Select(s => ((INotifyCollectionChanged)s, s)).ToArray())
        {

        }

        public ListSynchronizer(
            params (INotifyCollectionChanged EventSource, IEnumerable<T> Items)[] sources) : base(x => x, sources)
        {

        }
    }

    /// <summary>
    /// An intermediary that forwards changes made to observable source collections to a target list or lists.
    /// Synchronization can:
    /// - combine multiple source collections
    /// - filter source collection items
    /// - transform source collection items
    /// - sort target list items
    /// - raise INotifyCollectionChanged.CollectionChanged events
    /// </summary>
    /// <typeparam name="TSourceItem">Source collection item type</typeparam>
    /// <typeparam name="TTargetItem">Target list item type</typeparam>
    public class ListSynchronizer<TSourceItem, TTargetItem>
    {
        public ListSynchronizer(
            Func<TSourceItem, TTargetItem> transformation,
            params IEnumerable<TSourceItem>[] sources) : this(transformation, sources.Select(s => ((INotifyCollectionChanged)s, s)).ToArray())
        {

        }

        public ListSynchronizer(
            Func<TSourceItem, TTargetItem> transformation,
            params (INotifyCollectionChanged EventSource, IEnumerable<TSourceItem> Items)[] sources) : this(transformation, null, null, sources)
        {

        }

        public ListSynchronizer(
            Func<TSourceItem, TTargetItem> transformation,
            Func<TSourceItem, bool> filterPredicate,
            IComparer<TTargetItem> comparer,
            params IEnumerable<TSourceItem>[] sources) : this(transformation, filterPredicate, comparer, sources.Select(s => ((INotifyCollectionChanged)s, s)).ToArray())
        {

        }

        public ListSynchronizer(
            Func<TSourceItem, TTargetItem> transformation,
            Func<TSourceItem, bool> filterPredicate,
            IComparer<TTargetItem> comparer,
            params (INotifyCollectionChanged EventSource, IEnumerable<TSourceItem> Items)[] sources)
        {
            SourceLists = new List<TSourceItem>[sources.Length];
            SourceListIndexes = new Dictionary<INotifyCollectionChanged, int>();
            ItemMap = new Dictionary<TSourceItem, (int, TTargetItem)>();
            ListTargets = new List<IList<TTargetItem>>();
            EventTargets = new List<INotifyCollectionChanged>();
            Target = new List<TTargetItem>();
            Transformation = transformation;
            FilterPredicate = filterPredicate;
            Comparer = comparer;

            if (Transformation == null)
            {
                var converter = TypeDescriptor.GetConverter(typeof(TTargetItem));
                if (!converter.CanConvertFrom(typeof(TSourceItem)))
                {
                    throw new ArgumentException($"Argument {nameof(transformation)} is null and {typeof(TSourceItem).Name} cannot be converted to type {typeof(TTargetItem).Name}.", nameof(transformation));
                }

                Transformation = x => (TTargetItem)converter.ConvertFrom(x);
            }

            if (FilterPredicate != null && Comparer == null)
            {
                Comparer = Comparer<TTargetItem>.Default;
            }

            for (int i = 0; i < sources.Length; ++i)
            {
                var source = sources[i];
                var items = new List<TSourceItem>(source.Items);
                SourceLists[i] = items;
                Target.AddRange(MapNewSourceItems(items));
                SourceListIndexes.Add(source.EventSource, i);
                source.EventSource.CollectionChanged += Synchronize;
            }

            if (Comparer != null)
            {
                Target.Sort(Comparer);
            }
        }

        public void AddListTarget(IList<TTargetItem> target)
        {
            foreach (var item in Target)
            {
                target.Add(item);
            }

            ListTargets.Add(target);
        }

        public void AddEventTarget(INotifyCollectionChanged target)
        {
            EventTargets.Add(target);
        }

        public void RemoveListTarget(IList<TTargetItem> target)
        {
            ListTargets.Remove(target);
        }

        public void RemoveEventTarget(INotifyCollectionChanged target)
        {
            EventTargets.Remove(target);
        }

        private List<TTargetItem> Target { get; }

        private List<IList<TTargetItem>> ListTargets { get; }
        private List<INotifyCollectionChanged> EventTargets { get; }

        private Dictionary<TSourceItem, (int ReferenceCount, TTargetItem Item)> ItemMap { get; }
        private Dictionary<INotifyCollectionChanged, int> SourceListIndexes { get; }
        private List<TSourceItem>[] SourceLists { get; }

        private Func<TSourceItem, TTargetItem> Transformation { get; }
        private IComparer<TTargetItem> Comparer { get; }
        private Func<TSourceItem, bool> FilterPredicate { get; }

        private IEnumerable<TTargetItem> MapNewSourceItems(IEnumerable<TSourceItem> sourceItems)
        {
            if (sourceItems == null) yield break;

            if (FilterPredicate != null)
            {
                sourceItems = sourceItems.Where(FilterPredicate);
            }

            foreach (TSourceItem sourceItem in sourceItems)
            {
                TTargetItem targetItem;

                if (ItemMap.TryGetValue(sourceItem, out var value))
                {
                    targetItem = value.Item;
                    ++value.ReferenceCount;
                    ItemMap[sourceItem] = value;
                }
                else
                {
                    targetItem = Transformation(sourceItem);
                    ItemMap.Add(sourceItem, (1, targetItem));
                }

                yield return targetItem;
            }
        }

        private IEnumerable<TTargetItem> MapOldSourceItems(IEnumerable<TSourceItem> sourceItems)
        {
            if (sourceItems == null) yield break;

            foreach (TSourceItem sourceItem in sourceItems)
            {
                TTargetItem targetItem;

                if (ItemMap.TryGetValue(sourceItem, out var value))
                {
                    targetItem = value.Item;
                    --value.ReferenceCount;

                    if (value.ReferenceCount == 0)
                    {
                        ItemMap.Remove(sourceItem);
                    }
                    else
                    {
                        ItemMap[sourceItem] = value;
                    }
                }
                else
                {
                    continue;
                }

                yield return targetItem;
            }
        }

        private IEnumerable<TTargetItem> MapSourceItems(IEnumerable<TSourceItem> sourceItems)
        {
            if (sourceItems == null) yield break;

            foreach (TSourceItem sourceItem in sourceItems)
            {
                if (ItemMap.TryGetValue(sourceItem, out var value))
                {
                    yield return value.Item;
                }
            }
        }

        private void InsertSorted(TTargetItem item)
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

            foreach (var eventTarget in EventTargets)
            {
                var newItems = new List<TTargetItem> { item };
                eventTarget.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems, index));
            }
        }

        private void Remove(TTargetItem item)
        {
            int index = Target.IndexOf(item);
            Target.RemoveAt(index);

            foreach (var target in ListTargets)
            {
                target.RemoveAt(index);
            }

            foreach (var eventTarget in EventTargets)
            {
                var oldItems = new List<TTargetItem> { item };
                eventTarget.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, oldItems, index));
            }
        }

        private void InsertRange(int index, List<TTargetItem> newItems)
        {
            Target.InsertRange(index, newItems);

            foreach (var target in ListTargets)
            {
                for (int i = 0; i < newItems.Count; ++i)
                {
                    TTargetItem item = newItems[i];
                    target.Insert(index + i, item);
                }
            }

            foreach (var eventTarget in EventTargets)
            {
                eventTarget.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems, index));
            }
        }

        private void MoveRange(int oldIndex, int newIndex, List<TTargetItem> items)
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

            foreach (var eventTarget in EventTargets)
            {
                eventTarget.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, items, oldIndex, newIndex));
            }
        }

        private void RemoveRange(int index, List<TTargetItem> oldItems)
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

            foreach (var eventTarget in EventTargets)
            {
                eventTarget.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItems, index));
            }
        }

        private void ReplaceRange(int index, List<TTargetItem> newItems, List<TTargetItem> oldItems)
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

            foreach (var eventTarget in EventTargets)
            {
                eventTarget.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItems, oldItems, index));
            }
        }

        private void Clear()
        {
            Target.Clear();

            foreach (var target in ListTargets)
            {
                target.Clear();
            }

            foreach (var eventTarget in EventTargets)
            {
                eventTarget.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        private void Synchronize(object sender, NotifyCollectionChangedEventArgs e)
        {
            INotifyCollectionChanged eventSource = (INotifyCollectionChanged)sender;
            int sourceListIndex = SourceListIndexes[eventSource];
            List<TSourceItem> sourceList = SourceLists[sourceListIndex];

            IEnumerable<TSourceItem> newSourceItems = e.NewItems?.Cast<TSourceItem>();
            List<TTargetItem> newTargetItems = MapNewSourceItems(newSourceItems).ToList();

            IEnumerable<TSourceItem> oldSourceItems = e.Action == NotifyCollectionChangedAction.Reset
                ? sourceList
                : e.OldItems?.Cast<TSourceItem>();

            List<TTargetItem> oldTargetItems = e.Action == NotifyCollectionChangedAction.Move
                ? MapSourceItems(oldSourceItems).ToList()
                : MapOldSourceItems(oldSourceItems).ToList();

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    sourceList.InsertRange(e.NewStartingIndex, newSourceItems);

                    break;

                case NotifyCollectionChangedAction.Move:
                    List<TTargetItem> items = new List<TTargetItem>();

                    sourceList.RemoveRange(e.OldStartingIndex, e.NewItems.Count);
                    sourceList.InsertRange(e.NewStartingIndex, newSourceItems);

                    if (Comparer != null) return;

                    break;

                case NotifyCollectionChangedAction.Remove:
                    sourceList.RemoveRange(e.OldStartingIndex, e.OldItems.Count);

                    break;

                case NotifyCollectionChangedAction.Replace:
                    sourceList.RemoveRange(e.OldStartingIndex, e.NewItems.Count);
                    sourceList.InsertRange(e.NewStartingIndex, newSourceItems);

                    break;

                case NotifyCollectionChangedAction.Reset:
                    sourceList.Clear();

                    break;
            }

            int offset = 0;

            for (int i = 0; i < sourceListIndex; ++i)
            {
                offset += SourceLists[i].Count;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (Comparer == null)
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
                    if (Comparer == null)
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
                    if (Comparer == null)
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
                    if (SourceLists.Length == 1)
                    {
                        Clear();
                        break;
                    }
                    
                    if (Comparer == null)
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
