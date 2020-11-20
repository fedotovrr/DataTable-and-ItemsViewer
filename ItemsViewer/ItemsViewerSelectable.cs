using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ItemsViewer.Collection;

namespace ItemsViewer
{
    //Выбор элементов
    public class ItemsViewerSelectable : ItemsViewer
    {
        private SelectionMode selectionMode = SelectionMode.Extended;
        private int ShiftFocusIndex = -1;
        private int focusIndex = 0;
        private ViewItem focusItem;
        private ViewCollection itemsCollection;

        public event EventHandler<ViewItem> FocusItemChanged;

        private int FocusIndex { get => FocusItem == null ? focusIndex : FocusItem.Index; set => focusIndex = value; }

        public ViewItem FocusItem
        {
            get => focusItem;
            private set
            {
                if (focusItem != value)
                {
                    focusItem = value;
                    FocusItemChanged?.Invoke(this, focusItem);
                }
            }
        }

        internal ViewCollection InfoCollection => ItemsCollection;
        internal override ViewCollection ItemsCollection
        {
            get => itemsCollection;
            set
            {
                itemsCollection = value;
                itemsCollection?.CreateSelectable();
            }
        }


        public SelectionMode SelectionMode
        {
            get => selectionMode;
            set
            {
                if (selectionMode == SelectionMode.Multiple && value == SelectionMode.Single)
                    UnselectAll();
                selectionMode = value;
            }
        }

        public void SetFocus(int index)
        {
            RefreshFocus(index, 0);
        }

        private void RefreshFocus(int newIndex, int mode)
        {
            //mode = 0 None
            //mode = 1 Mouse or Space and Control
            //mode = 2 Mouse or Space and Shift
            //mode = 3 Key Up or Down and Control
            //mode = 4 Key Up or Down and Shift

            ViewItem oldValue = FocusItem;
            if (oldValue != null)
            {
                oldValue.Focused = false;
                if (SelectionMode == SelectionMode.Single)
                    oldValue.Selected = false;
            }
            newIndex = InfoCollection.CheckIndex(newIndex);
            if (newIndex < 0) return;
            ViewItem newValue = FocusItem = InfoCollection.GetViewItem(newIndex);
            if (newValue == null)
                return;
            
            FocusIndex = newValue.Index;
            newValue.Focused = true;
            if (SelectionMode == SelectionMode.Single)
                newValue.Selected = true;
            else if (SelectionMode == SelectionMode.Multiple)
                newValue.Selected = !newValue.Selected;
            else if (SelectionMode == SelectionMode.Extended)
            {
                if (mode == 0 || mode == 1)
                    ShiftFocusIndex = newValue.Index;

                if (mode == 0)
                {
                    UnselectAll();
                    newValue.Selected = true;
                }
                else if (mode == 1)
                    newValue.Selected = !newValue.Selected;
                else if ((mode == 2 || mode == 4) && InfoCollection.GetViewItemOrDefault(ShiftFocusIndex) is ViewItem shiftItem)
                    SelectRange(shiftItem.Index, newValue.Index);
            }
            ScrollIntoView(newValue);
        }

        internal override void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewItem.Focused) || e.PropertyName == nameof(ViewItem.Selected))
                return;
            else if (e.PropertyName == nameof(IChildCollection.Dropped) && sender is ViewItem item)
                RefreshFocus(item.Index, 0);
            base.Item_PropertyChanged(sender, e);
        }

        internal override void Child_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            int typeSelect = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control ? 1 : (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? 2 : 0;
            ViewItem item = ((FrameworkElement)sender).DataContext as ViewItem;
            if (item != null && (!item.Selected || typeSelect == 1 || typeSelect == 2))
                RefreshFocus(item.Index, typeSelect);
        }

        internal override void Child_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            int typeSelect = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control ? 1 : (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? 2 : 0;
            ViewItem item = ((FrameworkElement)sender).DataContext as ViewItem;
            if (item != null && item.Selected && typeSelect == 0)
                RefreshFocus(item.Index, typeSelect);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            bool handled = true;
            Key key = e.Key;
            int mode = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control ? 3 : (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? 4 : 0;
            switch (key)
            {
                case Key.A:
                    if (mode == 3 && SelectionMode == SelectionMode.Extended)
                        SelectAll();
                    else
                        handled = false;
                    break;
                case Key.Space:
                    if (SelectionMode == SelectionMode.Multiple || SelectionMode == SelectionMode.Extended)
                        RefreshFocus(FocusIndex, (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? 2 : 1);
                    else
                        handled = false;
                    break;
                case Key.Up:
                    RefreshFocus(FocusIndex - 1, mode);
                    break;
                case Key.Down:
                    RefreshFocus(FocusIndex + 1, mode);
                    break;
                case Key.Home:
                    RefreshFocus(0, mode);
                    break;
                case Key.End:
                    RefreshFocus(InfoCollection.Count - 1, mode);
                    break;
                default:
                    handled = false;
                    break;
            }
            if (handled)
                e.Handled = true;
            else
                base.OnKeyDown(e);
        }

        private void SelectAll()
        {
            int count = InfoCollection.Count;
            for (int i = 0; i < count; i++)
                InfoCollection.GetViewItem(i).Selected = true;
        }

        private void UnselectAll()
        {
            int count = InfoCollection.Count;
            for (int i = 0; i < count; i++)
                InfoCollection.GetViewItem(i).Selected = false;
        }

        private void SelectRange(int startIndex, int endIndex)
        {
            UnselectAll();
            if (endIndex < startIndex)
            {
                int t = startIndex;
                startIndex = endIndex;
                endIndex = t;
            }
            for (int i = startIndex; i <= endIndex; i++)
                InfoCollection.GetViewItem(i).Selected = true;
        }

        public List<T> GetSelectList<T>(out int level)
        {
            return GetSelectList(out level).Select(x => (T)x.Source).ToList();
        }

        private List<ViewItem> GetSelectList(out int level)
        {
            level = int.MaxValue;
            List<ViewItem> ret = new List<ViewItem>();
            if (ItemsCollection != null)
                if (ItemsCollection.SourceItemsIsIChildCollection)
                {
                    for (int i = 0; i < InfoCollection.Count; i++)
                        if (InfoCollection.GetViewItem(i) is ViewItem item && InfoCollection.GetItem(i) is IChildCollection collection && item.Selected && collection.DropLevel < level)
                            level = collection.DropLevel;
                    for (int i = 0; i < InfoCollection.Count; i++)
                        if (InfoCollection.GetViewItem(i) is ViewItem item && InfoCollection.GetItem(i) is IChildCollection collection && item.Selected && collection.DropLevel == level)
                            ret.Add(item);
                }
                else
                {
                    for (int i = 0; i < InfoCollection.Count; i++)
                        if (InfoCollection.GetViewItem(i) is ViewItem item && item.Selected)
                            ret.Add(item);
                }
            return ret;
        }

        public void Select(IEnumerable<object> items)
        {
            if (items == null)
                return;
            int count = ItemsCollection.Count;
            bool focus = false;
            for (int i = 0; i < count; i++)
            {
                ViewItem vItem = ItemsCollection.GetViewItem(i);
                object cItem = vItem.Source;
                IChildCollection vCollection = cItem as IChildCollection;
                foreach (object sItem in items)
                    if (sItem == cItem)
                    {
                        if (!focus)
                        {
                            focus = true;
                            SetFocus(vItem.Index);
                        }
                        vItem.Selected = true;
                        if (vCollection != null)
                            for (int j = i + 1; j < count; j++)
                                if (ItemsCollection.GetViewItem(i) is ViewItem item && item is IChildCollection collection)
                                    if (collection.DropLevel > vCollection.DropLevel)
                                        item.Selected = true;
                                    else
                                        break;
                    }
            }
        }
    }
}