using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace ItemsViewer.Collection
{
    [Serializable]
    public class ObservableList<T> : INotifyCollectionChanged, IList<T>, ICollection<T>, IList, ICollection, IReadOnlyList<T>, IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable
    {
        private List<T> Source = new List<T>();

        [field: NonSerializedAttribute()]
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public T this[int index] { get => Source[index]; set { if (value is T obj) { object old = Source[index]; Source[index] = obj; CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, obj, old)); } } }

        object IList.this[int index] { get => Source[index]; set { if (value is T obj) { object old = Source[index]; Source[index] = obj; CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, obj, old)); } } }

        T IList<T>.this[int index] { get => Source[index]; set { if (value is T obj) { object old = Source[index]; Source[index] = obj; CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, obj, old)); } } }

        T IReadOnlyList<T>.this[int index] => Source[index];

        public bool IsReadOnly => ((IList)Source).IsReadOnly;

        public bool IsFixedSize => ((IList)Source).IsFixedSize;

        public int Count => Source.Count;

        public object SyncRoot => ((IList)Source).SyncRoot;

        public bool IsSynchronized => ((IList)Source).IsSynchronized;


        public ObservableList()
        {

        }

        public ObservableList(IEnumerable<T> collection)
        {
            Source.AddRange(collection);
        }

        public void AddRange(IEnumerable<T> collection)
        {
            Source.AddRange(collection);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, collection is IList list ? list : collection.ToList()));
        }

        public void AddRange(IEnumerable collection)
        {
            bool add = false;
            foreach (object item in collection)
                if (((IList)Source).Add(item) >= 0)
                    add = true;
            if (add)
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, collection is IList list ? list : collection));
        }

        public void InsertRange(int index, IEnumerable<T> collection)
        {
            Source.InsertRange(index, collection);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (IList)collection));
        }

        public void RemoveRange(IEnumerable<T> collection)
        {
            List<T> change = new List<T>();
            foreach (T item in collection)
                if (Source.Remove(item))
                    change.Add(item);
            if (change.Count > 0)
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, (IList)change));
        }

        public int Add(object value)
        {
            int r = ((IList)Source).Add(value);
            if (r > -1)
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, value));
            return r;
        }

        public void Clear()
        {
            Source.Clear();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public bool Contains(object value)
        {
            return ((IList)Source).Contains(value);
        }

        public void CopyTo(Array array, int index)
        {
            ((IList)Source).CopyTo(array, index);
        }

        public IEnumerator GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
                yield return this[i];
        }

        public int IndexOf(object value)
        {
            return ((IList)Source).IndexOf(value);
        }

        public void Insert(int index, object value)
        {
            ((IList)Source).Insert(index, value);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, value));
        }

        public void Remove(object value)
        {
            ((IList)Source).Remove(value);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, value));
        }

        public void RemoveAt(int index)
        {
            object value = Source[index];
            Source.RemoveAt(index);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, value));
        }

        public int IndexOf(T item)
        {
            return Source.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            Source.Insert(index, item);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        }

        public void Add(T item)
        {
            Source.Add(item);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        }

        public bool Contains(T item)
        {
            return Source.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Source.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            bool r = Source.Remove(item);
            if (r)
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
            return r;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
                yield return this[i];
        }
    }
}
