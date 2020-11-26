using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Collections.ObjectModel;
using DataTable;

namespace ItemsViewer.Collection
{
    internal partial class ViewCollection : INotifyCollectionChanged
    {
        private List<ViewItem> InfoCollection;
        internal CollectionContainer SourceContainer;
        private bool IsSelectable;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public object Source => SourceContainer?.Source;

        public int Count => InfoCollection == null ? SourceContainer.Count : InfoCollection.Count;

        public Type SourceItemType => TypeManager.GetElementType(Source.GetType());

        public bool SourceItemsIsViewItem => SourceItemType is Type t && (t.BaseType == typeof(ViewItem) || t.BaseType == typeof(ViewItemCollection) || t.IsSubclassOf(typeof(ViewItem)) || t.IsSubclassOf(typeof(ViewItemCollection)));

        public bool SourceItemsIsIChildCollection => SourceItemType?.GetInterface(typeof(IChildCollection).FullName) != null;

        public SortProperties Sort { get; private set; }

        public FilterManager Filter { get; private set; }

        public ViewCollection(object collection)
        {
            SourceContainer = new CollectionContainer(collection);
            Sort = new SortProperties(this);
            Filter = new FilterManager(this);
            AddEventBySource();
            RefreshInfoCollection();
        }


        private void AddEventBySource()
        {
            if (Source is INotifyCollectionChanged sCollectionChanged)
                sCollectionChanged.CollectionChanged += CollectionChanged_CollectionChanged;
        }

        private void RemoveEventBySource()
        {
            if (Source is INotifyCollectionChanged sCollectionChanged)
                sCollectionChanged.CollectionChanged -= CollectionChanged_CollectionChanged;
        }

        private void RemoveEventByItems()
        {
            if (InfoCollection != null)
                for (int i = 0; i < InfoCollection.Count; i++)
                    if (InfoCollection[i] is INotifyCollectionChanged collectionChanged)
                        collectionChanged.CollectionChanged -= CollectionChanged_CollectionChanged;
        }

        public int CheckIndex(int index)
        {
            int count = this.Count;
            return index < 0 ? 0 : index < count ? index : count - 1;
        }

        public object GetItem(int index)
        {
            if (InfoCollection == null) return SourceContainer[index];
            ViewItem item = InfoCollection[index];
            return item == null ? null : item.Source;
        }

        public ViewItem GetViewItem(int index)
        {
            return InfoCollection == null ? SourceContainer[index] as ViewItem : InfoCollection[index];
        }

        public ViewItem GetViewItemOrDefault(int index)
        {
            return index >= 0 && index < Count ? GetViewItem(index) : null;
        }

        public List<UndoRedoManager.RemoveCommand> Remove(List<object> items, bool isUndoRedo)
        {
            if (items == null || items.Count == 0) return null;
            RemoveEventByItems();
            RemoveEventBySource();
            List<UndoRedoManager.RemoveCommand> commands = SourceContainer.Remove(items, isUndoRedo);
            AddEventBySource();
            CollectionChanged_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, items));
            return commands;
        }

        public void Insert(int index, List<object> items)
        {
            if (items == null || items.Count == 0) return;
            RemoveEventByItems();
            RemoveEventBySource();
            SourceContainer.Insert(index, items);
            AddEventBySource();
            CollectionChanged_CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items));
        }

        public void CollectionChanged_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshInfoCollection();
            CollectionChanged?.Invoke(sender, e);
        }

        public void RefreshInfoCollection()
        {
            RemoveEventByItems();
            InfoCollection = null;
            int count = SourceContainer.Count;
            if (SourceItemsIsIChildCollection || Sort.SortType != SortProperties.SortTypes.None || Filter.Any)
            {
                InfoCollection = new List<ViewItem>(count);
                //сборка коллекции первого уровня
                if (Sort.SortType == SortProperties.SortTypes.None && !Filter.Any)
                    for (int i = 0; i < count; i++)
                        CreateInfoCollection(SourceContainer[i], 1, null, true);
                else
                {
                    Tuple<object, CellInfo[]>[] items = new Tuple<object, CellInfo[]>[count];
                    for (int i = 0; i < count; i++)
                        if (SourceContainer[i] is object obj && obj is ITableRow row && row.GetRowInfo() is CellInfo[] cells)
                            items[i] = new Tuple<object, CellInfo[]>(obj, cells);
                    //фильтрация коллекции первого уровня
                    if (Filter.Any)
                        items = items.Where(x => Filter.CheckRow(x.Item2)).ToArray();
                    //сортировка коллекции первого уровня
                    if (Sort.SortType != SortProperties.SortTypes.None && Sort.Column >= 0)
                        Array.Sort(items, 0, items.Length, Sort.GetComparer());
                    for (int i = 0; i < items.Length; i++)
                        CreateInfoCollection(items[i]?.Item1, 1, null, true);
                }
            }
            if (IsSelectable)
                CreateSelectable();
            GC.Collect();
        }

        public void CreateSelectable()
        {
            IsSelectable = true;
            if (InfoCollection != null || Source == null)
                return;
            else if (SourceItemsIsViewItem)
            {
                int count = SourceContainer.Count;
                for (int i = 0; i < count; i++)
                    ((ViewItem)SourceContainer[i]).Index = i;
            }
            else
            {
                int count = SourceContainer.Count;
                InfoCollection = new List<ViewItem>(count);
                for (int i = 0; i < count; i++)
                    InfoCollection.Add(new ViewItem { Index = i, Source = SourceContainer[i] });
            }
        }

        private void CreateInfoCollection(object item, byte level, IChildCollection parent, bool isAdd)
        {
            if (item == null) return;
            if (isAdd)
            {
                ViewItem viewItem = item is ViewItem ? item as ViewItem : new ViewItem() { Source = item };
                viewItem.Index = InfoCollection.Count;
                InfoCollection.Add(viewItem);
            }
            if (item is IChildCollection collection)
            {
                collection.Parent = parent;
                collection.DropLevel = level;
                //if (collection.Dropped)
                {
                    level++;
                    if (isAdd && collection is INotifyCollectionChanged collectionChanged)
                        collectionChanged.CollectionChanged += CollectionChanged_CollectionChanged;
                    //сборка коллекций последующих уровней
                    if (Sort.SortType == SortProperties.SortTypes.None && !Filter.Any)
                        for (int i = 0; i < collection.Count; i++)
                            CreateInfoCollection(collection[i], level, collection, isAdd && collection.Dropped);
                    else
                    {
                        Tuple<object, CellInfo[]>[] items = new Tuple<object, CellInfo[]>[collection.Count];
                        for (int i = 0; i < collection.Count; i++)
                            if (collection[i] is object obj && obj is ITableRow row && row.GetRowInfo() is CellInfo[] cells)
                                items[i] = new Tuple<object, CellInfo[]>(obj, cells);
                        //фильтрация коллекций последующих уровней
                        if (Filter.Any)
                            items = items.Where(x => Filter.CheckRow(x.Item2)).ToArray();
                        //сортировка коллекций последующих уровней
                        if (Sort.SortType != SortProperties.SortTypes.None && Sort.Column >= 0)
                            Array.Sort(items, 0, items.Length, Sort.GetComparer());
                        for (int i = 0; i < items.Length; i++)
                            CreateInfoCollection(items[i]?.Item1, level, collection, isAdd && collection.Dropped);
                    }
                }
            }
            return;
        }

        public void Dispose()
        {
            RemoveEventByItems();
            RemoveEventBySource();
            InfoCollection?.Clear();
            SourceContainer?.Dispose();
            Sort?.Dispose();
            SourceContainer = null;
            GC.Collect();
        }
    }
}
