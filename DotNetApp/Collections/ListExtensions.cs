using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetApp.Collections.Extensions
{
    public static class ListExtensions
    {
        public static int BinarySearch<T>(this IList<T> list, T value, IComparer<T> comparer = null)
        {
            if (list is null) throw new ArgumentNullException(nameof(list));

            comparer = comparer ?? Comparer<T>.Default;

            int lower = 0;
            int upper = list.Count - 1;

            while (lower <= upper)
            {
                int middle = lower + (upper - lower) / 2;
                int comparison = comparer.Compare(value, list[middle]);

                if (comparison == 0)
                {
                    return middle;
                }
                else if (comparison < 0)
                {
                    upper = middle - 1;
                }
                else
                {
                    lower = middle + 1;
                }
            }

            return ~lower;
        }

        public static void InplaceUpdateSorted<T>(this IList<T> target, IList<T> source, IComparer<T> comparer)
        {
            if (source.Count == 0)
            {
                // Source list is empty.
                // -> Clear target list.

                target.Clear();
                return;
            }

            if (target.Count == 0)
            {
                // Target list is empty.
                // -> All items from source list can be added to target list.

                foreach (T item in source)
                {
                    target.Add(item);
                }

                return;
            }

            int i = 0;

            T itemTarget = target[i];
            T itemSource = source[i];

            while (true)
            {
                int cmp = comparer.Compare(itemTarget, itemSource);

                if (cmp < 0)
                {
                    // Target item should appear before source item so target item does not exist in source list.
                    // -> Remove target item

                    target.RemoveAt(i);

                    if (i == target.Count)
                    {
                        // Target list has no more items.
                        // -> All remaining items from source list can be added to target list.

                        while (i < source.Count)
                        {
                            itemSource = source[i++];
                            target.Add(itemSource);
                        }

                        return;
                    }

                    itemTarget = target[i];
                }
                else if (cmp > 0)
                {
                    // Target item should appear after source item so the source item does not exist in target list.
                    // -> Insert source item

                    target.Insert(i++, itemSource);

                    if (i == source.Count)
                    {
                        // Source list has no more items.
                        // -> All remaining items in the target list can removed.

                        while (i < target.Count)
                        {
                            target.RemoveAt(i);
                        }

                        return;
                    }

                    itemSource = source[i];
                }
                else if (EqualityComparer<T>.Default.Equals(itemTarget, itemSource))
                {
                    // Items exist in both lists
                    // -> No actions

                    ++i;

                    if (i == target.Count)
                    {
                        // Target list has no more items.
                        // -> All remaining items from source list can be added to target list.

                        while (i < source.Count)
                        {
                            itemSource = source[i++];
                            target.Add(itemSource);
                        }

                        return;
                    }

                    if (i == source.Count)
                    {
                        // Source list has no more items.
                        // -> All remaining items in the target list can removed.

                        while (i < target.Count)
                        {
                            target.RemoveAt(i);
                        }

                        return;
                    }

                    itemTarget = target[i];
                    itemSource = source[i];
                }
                else
                {
                    throw new InvalidOperationException("Comparison should return non-zero if items are not equal");
                }
            }
        }

        // https://stackoverflow.com/a/24058279
        public static IEnumerable<T> TopologicalSort<T>(this IEnumerable<T> nodes, Func<T, IEnumerable<T>> dependencies)
        {
            var elements = nodes.ToDictionary(node => node, node => new HashSet<T>(dependencies(node)));

            while (elements.Count > 0)
            {
                var next = elements.First(x => x.Value.Count == 0);

                if (next.Key == null)
                {
                    throw new ArgumentException("Cyclic connections are not allowed");
                }

                elements.Remove(next.Key);

                foreach (var element in elements)
                {
                    element.Value.Remove(next.Key);
                }

                yield return next.Key;
            }
        }
    }

}
