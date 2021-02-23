using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetApp.Collections.Extensions
{
    public static class ListExtensions
    {
        public static void InplaceUpdateSorted<T>(this IList<T> target, IList<T> source, IComparer<T> comparer)
        {
            if (source.Count == 0)
            {
                target.Clear();
                return;
            }

            if (target.Count == 0)
            {
                foreach (T item in source)
                {
                    target.Add(item);
                }

                return;
            }

            int iTarget = 0;
            int iSource = 0;

            T itemTarget = target[iTarget];
            T itemSource = source[iSource];

            while (true)
            {
                int cmp = comparer.Compare(itemTarget, itemSource);

                if (cmp < 0)
                {
                    // Target item should appear before source item so target item does not exist in source list.
                    // -> Remove target item

                    target.RemoveAt(iTarget);

                    if (iTarget == target.Count)
                    {
                        while (iSource < source.Count)
                        {
                            itemSource = source[iSource++];
                            target.Add(itemSource);
                        }

                        return;
                    }

                    itemTarget = target[iTarget];
                }
                else if (cmp > 0)
                {
                    // Target item should appear after source item so the source item does not exist in target list.
                    // -> Insert source item

                    target.Insert(iTarget++, itemSource);
                    ++iSource;

                    if (iSource == source.Count)
                    {
                        while (iTarget < target.Count)
                        {
                            target.RemoveAt(iSource);
                        }

                        return;
                    }

                    itemSource = source[iSource];
                }
                else if (EqualityComparer<T>.Default.Equals(itemTarget, itemSource))
                {
                    // Items exist in both lists
                    // -> No actions

                    ++iTarget;
                    ++iSource;

                    if (iTarget == target.Count)
                    {
                        while (iSource < source.Count)
                        {
                            itemSource = source[iSource++];
                            target.Add(itemSource);
                        }

                        return;
                    }

                    if (iSource == source.Count)
                    {
                        while (iTarget < target.Count)
                        {
                            target.RemoveAt(iSource);
                        }

                        return;
                    }

                    itemTarget = target[iTarget];
                    itemSource = source[iSource];
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
