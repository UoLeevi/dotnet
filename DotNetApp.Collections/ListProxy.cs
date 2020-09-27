using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DotNetApp.Collections
{
    /// <summary>
    /// Provides a proxy for a list where individual IList&lt;T&gt; interface members can be overriden using delegates.
    /// </summary>
    /// <typeparam name="T">The type of objects in the list.</typeparam>
    public class ListProxy<T> : IList<T>
    {
        private IList NonGenericList { get; }
        private IList<T> GenericList { get; }

        public ListProxy(List<T> list)
        {
            GenericList = list;
        }

        public ListProxy(IList list)
        {
            NonGenericList = list;
        }

        public ListProxy(IList<T> list)
        {
            GenericList = list;
        }

        public Func<int, T> ProxyGetItem { get; set; }
        public Action<int, T> ProxySetItem { get; set; }
        public T this[int index]
        { 
            get
            {
                if (ProxyGetItem != null) return ProxyGetItem(index);
                else if (GenericList != null) return GenericList[index];
                else if (NonGenericList != null) return (T)NonGenericList[index];
                else throw new NotImplementedException();
            }
            set
            {
                if (ProxySetItem != null) ProxySetItem(index, value);
                else if (GenericList != null) GenericList[index] = value;
                else if (NonGenericList != null) NonGenericList[index] = value;
                else throw new NotImplementedException();
            }
        }

        public Func<int> ProxyCount { get; set; }
        public int Count
        {
            get
            {
                if (ProxyCount != null) return ProxyCount();
                else if (GenericList != null) return GenericList.Count;
                else if (NonGenericList != null) return NonGenericList.Count;
                else throw new NotImplementedException();
            }
        }

        public Func<bool> ProxyIsReadOnly { get; set; }
        public bool IsReadOnly
        {
            get
            {
                if (ProxyIsReadOnly != null) return ProxyIsReadOnly();
                else if (GenericList != null) return GenericList.IsReadOnly;
                else if (NonGenericList != null) return NonGenericList.IsReadOnly;
                else throw new NotImplementedException();
            }
        }

        public Action<T> ProxyAdd { get; set; }
        public void Add(T item)
        {
            if (ProxyAdd != null) ProxyAdd(item);
            else if (GenericList != null) GenericList.Add(item);
            else if (NonGenericList != null) NonGenericList.Add(item);
            else throw new NotImplementedException();
        }

        public Action ProxyClear { get; set; }
        public void Clear()
        {
            if (ProxyClear != null) ProxyClear();
            else if (GenericList != null) GenericList.Clear();
            else if (NonGenericList != null) NonGenericList.Clear();
            else throw new NotImplementedException();
        }

        public Func<T, bool> ProxyContains{ get; set; }
        public bool Contains(T item)
        {
            if (ProxyContains != null) return ProxyContains(item);
            else if (GenericList != null) return GenericList.Contains(item);
            else if (NonGenericList != null) return NonGenericList.Contains(item);
            else throw new NotImplementedException();
        }

        public Action<T[], int> ProxyCopyTo { get; set; }
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (ProxyCopyTo != null) ProxyCopyTo(array, arrayIndex);
            else if (GenericList != null) GenericList.CopyTo(array, arrayIndex);
            else if (NonGenericList != null) NonGenericList.CopyTo(array, arrayIndex);
            else throw new NotImplementedException();
        }

        public Func<IEnumerator<T>> ProxyGetEnumerator { get; set; }
        public IEnumerator<T> GetEnumerator()
        {
            if (ProxyGetEnumerator != null) return ProxyGetEnumerator();
            else if (GenericList != null) return GenericList.GetEnumerator();
            else if (NonGenericList != null) return NonGenericList.Cast<T>().GetEnumerator();
            else throw new NotImplementedException();
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public Func<T, int> ProxyIndexOf { get; set; }
        public int IndexOf(T item)
        {
            if (ProxyIndexOf != null) return ProxyIndexOf(item);
            else if (GenericList != null) return GenericList.IndexOf(item);
            else if (NonGenericList != null) return NonGenericList.IndexOf(item);
            else throw new NotImplementedException();
        }

        public Action<int, T> ProxyInsert { get; set; }
        public void Insert(int index, T item)
        {
            if (ProxyInsert != null) ProxyInsert(index, item);
            else if (GenericList != null) GenericList.Insert(index, item);
            else if (NonGenericList != null) NonGenericList.Insert(index, item);
            else throw new NotImplementedException();
        }

        public Func<T, bool> ProxyRemove { get; set; }
        public bool Remove(T item)
        {
            if (ProxyRemove != null) return ProxyRemove(item);
            else if (GenericList != null) return GenericList.Remove(item);
            else if (NonGenericList != null)
            {
                NonGenericList.Remove(item);
                return true;
            }
            else throw new NotImplementedException();
        }

        public Action<int> ProxyRemoveAt { get; set; }
        public void RemoveAt(int index)
        {
            if (ProxyRemoveAt != null) ProxyRemoveAt(index);
            else if (GenericList != null) GenericList.RemoveAt(index);
            else if (NonGenericList != null) NonGenericList.RemoveAt(index);
            else throw new NotImplementedException();
        }
    }
}
