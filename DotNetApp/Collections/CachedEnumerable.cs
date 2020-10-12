using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace DotNetApp.Collections
{

    public class CachedEnumerable<T> : IEnumerable<T>, IDisposable
    {
        private ReaderWriterLockSlim rwlock;
        private IEnumerable<T> enumerable;
        private IEnumerator<T> enumerator;
        private IList<T> cache;

        public CachedEnumerable(IEnumerable<T> enumerable)
        {
            this.enumerable = enumerable;
            this.cache = new List<T>();
        }

        public void Dispose()
        {
            rwlock?.Dispose();
            enumerator?.Dispose();
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (cache is ImmutableArray<T>)
            {
                foreach (T item in cache)
                {
                    yield return item;
                }

                yield break;
            }

            lock (cache)
            {
                if (rwlock is null)
                {
                    rwlock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
                    enumerator = enumerable.GetEnumerator();
                    enumerable = null;
                }
            }

            int index = 0;

            rwlock?.EnterUpgradeableReadLock();

            while (index < cache.Count)
            {
                T item = cache[index++];
                rwlock?.ExitUpgradeableReadLock();
                yield return item;
                rwlock?.EnterUpgradeableReadLock();
            }

            rwlock.EnterWriteLock();
            rwlock.ExitUpgradeableReadLock();

            while (enumerator?.MoveNext() is true)
            {
                T item = enumerator.Current;
                cache.Add(item);
                rwlock.ExitWriteLock();
                yield return item;
                ++index;

                rwlock?.EnterUpgradeableReadLock();

                while (index < cache.Count)
                {
                    item = cache[index++];
                    rwlock?.ExitUpgradeableReadLock();
                    yield return item;
                    rwlock?.EnterUpgradeableReadLock();
                }

                rwlock?.EnterWriteLock();
                rwlock?.ExitUpgradeableReadLock();
            }

            if (enumerator is null) yield break;

            cache = cache.ToImmutableArray();
            ReaderWriterLockSlim templock = rwlock;
            rwlock = null;
            templock.ExitWriteLock();
            
            while (templock.WaitingReadCount > 0)
            {
                // busy wait
            }

            templock.Dispose();
            enumerator.Dispose();
            enumerator = null;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public static class CachedEnumerable
    {
        public static CachedEnumerable<T> ToCachedEnumerable<T>(this IEnumerable<T> enumerable)
            => new CachedEnumerable<T>(enumerable);
    }
}
