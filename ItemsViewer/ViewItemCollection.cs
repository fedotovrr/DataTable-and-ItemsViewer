using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace ItemsViewer
{
    /// <summary>
    /// Не наследуйте этот класс, если хотите использовать дубликаты объектов в коллекции ItemsSource
    /// </summary>
    [Serializable]
    public class ViewItemCollection : ViewItem, IChildCollection, INotifyCollectionChanged
    {
        //Drop

        [NonSerialized]
        [XmlIgnore]
        private byte dropLevel;

        [NonSerialized]
        [XmlIgnore]
        private bool dropped;

        [XmlIgnore]
        public byte DropLevel { get => dropLevel; set => dropLevel = value; }

        [XmlIgnore]
        public bool Dropped
        {
            get => dropped;
            set
            {
                if (dropped != value)
                {
                    dropped = value;
                    NotifyPropertyChanged();
                    //((NotifyCollectionChangedEventHandler)EventHandlerList?[CollectionChangedEventKey])?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
            }
        }

        [XmlIgnore]
        public bool DropMarkVisible => Children.Count > 0;


        //Children

        [XmlIgnore]
        public IChildCollection Parent { get; set; }

        [XmlIgnore]
        private ObservableCollection<object> children;

        [XmlIgnore]
        public ObservableCollection<object> Children { get { if (children == null) children = new ObservableCollection<object>(); return children; } set => children = value; }

        [XmlArray(nameof(Children))]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public object[] SerializableChildren
        {
            get
            {
                if (Children.Count == 0) return null;
                object[] arr = new object[Children.Count];
                for (int i = 0; i < Children.Count; i++)
                    arr[i] = Children[i];
                return arr;
            }
            set
            {
                Children.Clear();
                if (value != null)
                    for (int i = 0; i < value.Length; i++)
                        Children.Add(value[i]);
            }
        }

        [NonSerialized]
        [XmlIgnore]
        private EventHandlerList EventHandlerList;

        [NonSerialized]
        [XmlIgnore]
        private static readonly object CollectionChangedEventKey = new object();

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                if (EventHandlerList == null)
                {
                    EventHandlerList = new EventHandlerList();
                    Children.CollectionChanged += Children_CollectionChanged;
                }
                EventHandlerList.AddHandler(CollectionChangedEventKey, value);
            }
            remove
            {
                if (EventHandlerList != null)
                    EventHandlerList.RemoveHandler(CollectionChangedEventKey, value);
            }
        }

        [XmlIgnore]
        public int Count => Children.Count;

        [XmlIgnore]
        public object this[int index] => Children[index];

        private void Children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ((NotifyCollectionChangedEventHandler)EventHandlerList?[CollectionChangedEventKey])?.Invoke(this, e);
        }

        public virtual void Add(object item)
        {
            bool oldmarck = DropMarkVisible;
            Children.Add(item);
            if (DropMarkVisible != oldmarck)
                NotifyPropertyChanged("DropMarkVisible");
        }

        public virtual void Insert(int index, object item)
        {
            bool oldmarck = DropMarkVisible;
            Children.Insert(index, item);
            if (DropMarkVisible != oldmarck)
                NotifyPropertyChanged("DropMarkVisible");
        }

        public virtual void InsertRange(int index, IEnumerable<object> collection)
        {
            if (collection == null) return;
            Children.CollectionChanged -= Children_CollectionChanged;
            bool oldmarck = DropMarkVisible;
            foreach (object item in collection)
            {
                Children.Insert(index, item);
                index++;
            }
            Children.CollectionChanged += Children_CollectionChanged;
            if (DropMarkVisible != oldmarck)
                NotifyPropertyChanged("DropMarkVisible");
            Children_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, collection));
        }

        public virtual bool Remove(object item)
        {
            bool oldmarck = DropMarkVisible;
            bool ret = Children.Remove(item);
            if (DropMarkVisible != oldmarck)
                NotifyPropertyChanged("DropMarkVisible");
            return ret;
        }

        public virtual void Clear()
        {
            if (Children.Count > 0)
            {
                Children.Clear();
                NotifyPropertyChanged("DropMarkVisible");
            }
        }
    }
}
