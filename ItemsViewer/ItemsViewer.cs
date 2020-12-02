using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using ItemsViewer.Collection;

namespace ItemsViewer
{
    //Отрисовка и скруллы
    public partial class ItemsViewer : UserControl
    {
        private readonly object RefreshLock = new object();

        private int TopItemIndex;
        private int ViewCount;
        private double ScrollViewCount;
        private double ScrollOffset;
        private double PanelOffset;
        private Timer RefreshDelay = new Timer(3);

        internal Grid HeaderContainer = new Grid();
        internal Grid ControlGrid;
        private StackPanel ContentContainer;
        internal StackPanel ItemsPanel;
        internal Border ItemsBorder;
        private ScrollBar ItemsVSB;
        internal ScrollBar ItemsHSB;

        private DataTemplate itemTemplate;

        internal virtual ViewCollection ItemsCollection { get; set; }

        public event EventHandler ViewRefreshed;

        public event EventHandler ItemsSourceChanged;

        public double VerticalScrollValue { get => ItemsVSB.Value; set { if (ItemsVSB.Value != value) { ItemsVSB.Value = value; Refresh(); } } }

        public double HorizontalScrollValue { get => ItemsHSB.Value; set { if (ItemsHSB.Value != value) { ItemsHSB.Value = value; RefreshHScroll(); } } }

        public event ScrollEventHandler VerticalScroll;

        public event ScrollEventHandler HorizontalScroll;

        public bool HorizontalScrollable { get => ContentContainer?.Orientation == Orientation.Horizontal; set { if (ContentContainer == null) return; if (value) { ContentContainer.Orientation = Orientation.Horizontal; ItemsHSB.Visibility = Visibility.Visible; } else { ContentContainer.Orientation = Orientation.Vertical; ItemsHSB.Visibility = Visibility.Collapsed; } Refresh(); } }

        public DataTemplate ItemTemplate
        {
            get
            {
                return itemTemplate;
            }
            set
            {
                itemTemplate = value;
                ClearChild();
            }
        }


        public object ItemsSource
        {
            get
            {
                return ItemsCollection?.Source;
            }
            set
            {
                if (ItemsSource == value) return;
                if (ItemsCollection != null)
                {
                    ItemsCollection.CollectionChanged -= ItemCollection_CollectionChanged;
                    ClearChild();
                    ItemsCollection.Dispose();
                }
                if (value != null)
                {
                    ItemsCollection = new ViewCollection(value);
                    ItemsCollection.CollectionChanged += ItemCollection_CollectionChanged;
                }
                else
                    ItemsCollection = null;
                ItemsSourceChanged?.Invoke(this, null);
                Refresh();
            }
        }

        internal void ScrollIntoView(ViewItem item)
        {
            if (item == null)
                return;
            ScrollIntoView(item.Index);
        }

        public void ScrollIntoView(int index)
        {
            index = ItemsCollection.CheckIndex(index);
            int endIndex = TopItemIndex + ViewCount - 1;
            if (index <= TopItemIndex)
            {
                TopItemIndex = index;
                RefreshMethod(1);
            }
            else if (index >= endIndex)
            {
                TopItemIndex = index;
                RefreshMethod(2);
            }
        }


        public ItemsViewer()
        {
            InitControls();
            SizeChanged += BigList_SizeChanged;
            RefreshDelay.AutoReset = false;
            RefreshDelay.Elapsed += RefreshTimer_Elapsed;
        }

        private void InitControls()
        {
            SnapsToDevicePixels = true;
            ClipToBounds = true;
            Focusable = true;
            FocusVisualStyle = null;
            //BorderThickness = new Thickness(1);
            //BorderBrush = Brushes.Blue;

            //сетка элемента
            ControlGrid = new Grid();
            ControlGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            ControlGrid.RowDefinitions.Add(new RowDefinition());
            ControlGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            ControlGrid.ColumnDefinitions.Add(new ColumnDefinition());
            ControlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Content = ControlGrid;

            //панель элементов
            ItemsPanel = new StackPanel();

            ContentContainer = new StackPanel() { Orientation = Orientation.Horizontal, ClipToBounds = true };
            ContentContainer.Children.Add (ItemsPanel);

            ItemsBorder = new Border() { AllowDrop = true, Background = new SolidColorBrush(new Color { A = 0, B = 0, R = 0, G = 0 }) };
            //ItemsBorder.BorderThickness = new Thickness(1);
            //ItemsBorder.BorderBrush = Brushes.Blue;
            ItemsBorder.MouseWheel += ItemsBorder_MouseWheel;
            ItemsBorder.Child = ContentContainer;
            Grid.SetRow(ItemsBorder, 1);
            ControlGrid.Children.Add(ItemsBorder);

            //скруллы
            ItemsVSB = new ScrollBar { Orientation = Orientation.Vertical };
            Grid.SetRow(ItemsVSB, 1);
            Grid.SetColumn(ItemsVSB, 1);
            ItemsVSB.Scroll += ItemsVSB_Scroll;
            ControlGrid.Children.Add(ItemsVSB);

            ItemsHSB = new ScrollBar { Orientation = Orientation.Horizontal };
            Grid.SetRow(ItemsHSB, 2);
            ItemsHSB.Scroll += ItemsHSB_Scroll;
            ControlGrid.Children.Add(ItemsHSB);

            //голова
            ControlGrid.Children.Add(HeaderContainer);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            this.Focus();
        }

        internal void ItemsHSB_SetValue(double newValue)
        {
            ItemsHSB.Value = newValue;
            RefreshHScroll();
        }

        private void ItemsHSB_Scroll(object sender, ScrollEventArgs e)
        {
            RefreshHScroll();
        }

        private void ItemsVSB_Scroll(object sender, ScrollEventArgs e)
        {
            Refresh();
        }

        private void ItemsBorder_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            double newVal = ItemsVSB.Value + (0 - e.Delta) / 120 * (ScrollViewCount * 0.3);
            if (newVal < 0) newVal = 0;
            if (newVal > ItemsVSB.Maximum) newVal = ItemsVSB.Maximum;
            ItemsVSB.Value = newVal;
            Refresh();
        }

        private void BigList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Refresh();
        }

        internal virtual void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Dropped" && sender is ViewItem)
                ItemsCollection.RefreshInfoCollection();
            Refresh();
        }

        internal virtual void ItemCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Refresh();
        }

        internal virtual void Child_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        internal virtual void Child_MouseUp(object sender, MouseButtonEventArgs e)
        {

        }


        private void RefreshHScroll()
        {
            ItemsPanel.Margin = new Thickness(0 - Math.Round(ItemsHSB.Value), PanelOffset, 0, 0);
            HeaderContainer.Margin = new Thickness(0 - Math.Round(ItemsHSB.Value), 0, 0, 0);

            if (ItemsPanel.Children.Count == 0) return;
            double width = ItemsPanel.ActualWidth;
            double pwidth = ItemsBorder.ActualWidth;
            double vwidth = Math.Abs(ItemsPanel.Margin.Left) + pwidth;
            if (width > vwidth) vwidth = width;
            double unvwidth = vwidth - pwidth;
            if (unvwidth > 0 && ItemsHSB.Track != null)
            {
                double ThumbH = pwidth / vwidth * ItemsHSB.Track.ActualWidth;
                if (ThumbH < 0) ThumbH = 0;
                ItemsHSB.Maximum = unvwidth;
                ItemsHSB.ViewportSize = ThumbH * (ItemsHSB.Maximum - ItemsHSB.Minimum) / (ItemsHSB.Track.ActualWidth - ThumbH);
            }
            else
            {
                ItemsHSB.ViewportSize = width;
                ItemsHSB.Maximum = 0;
            }

            HorizontalScroll?.Invoke(this, new ScrollEventArgs(ScrollEventType.ThumbTrack, ItemsHSB.Value));
        }

        internal void Refresh()
        {
            //Dispatcher.Invoke(new Action(() => RefreshMethod(0)));
            try
            {
                RefreshDelay.Enabled = false;
                RefreshDelay.Enabled = true;
            }
            catch (Exception) { }
        }

        private void RefreshTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try { Dispatcher.Invoke(new Action(() => RefreshMethod(0))); } catch (Exception) { }
        }

        private void RefreshMethod(int typeLoad)
        {
            //System.Diagnostics.Debug.WriteLine("RefreshMethod");
            //typeLoad == 0 по значению скрулла
            //typeLoad == 1 от верхнего элемента
            //typeLoad == 2 от нижнего элемента
            lock (RefreshLock)
            {
                if (ItemsCollection == null)
                {
                    ItemsPanel.Children.Clear();
                    return;
                }
                if (typeLoad == 0)
                {
                    double rv = ItemsVSB.Value - ItemsCollection.CheckIndex(TopItemIndex);
                    if (rv > 0)
                    {
                        int irv = (int)Math.Abs(rv);
                        TopItemIndex = ItemsCollection.CheckIndex(TopItemIndex + irv);
                        ScrollOffset = rv - irv;
                    }
                    else
                    {
                        int irv = (int)Math.Ceiling(Math.Abs(rv));
                        TopItemIndex = ItemsCollection.CheckIndex(TopItemIndex - irv);
                        ScrollOffset = rv + irv;
                    }
                }
                else if (typeLoad > 0)
                    ScrollOffset = 0;

                //Очистка панели
                for (int i = 0; i < ItemsPanel.Children.Count; i++)
                    ((FrameworkElement)ItemsPanel.Children[i]).Visibility = Visibility.Collapsed;
                UpdateLayoutItemsPanel();

                //Добавление элементов
                ViewCount = 0;
                double height = 0;
                double maxHeight = ItemsBorder.ActualHeight;
                int CollectionCount = ItemsCollection.Count;
                int index = TopItemIndex = ItemsCollection.CheckIndex(TopItemIndex);
                int findex = TopItemIndex - 1;
                double sizeViewTopChild = 0;
                double sizeViewLastChild = 0;
                double sizeTopChild = 0;
                while (height < maxHeight && index < CollectionCount)
                {
                    FrameworkElement child = GetChild(ViewCount, index);
                    UpdateLayoutItemsPanel();
                    height += child.ActualHeight;
                    ViewCount++;
                    index++;
                    if (ViewCount == 1)
                    {
                        sizeTopChild = child.ActualHeight;
                        sizeViewTopChild = (child.ActualHeight - child.ActualHeight * ScrollOffset) / child.ActualHeight;
                        PanelOffset = 0 - child.ActualHeight * ScrollOffset;
                        height += PanelOffset;
                        if (typeLoad == 2)
                            break;
                    }
                }

                //Заполнение оставшегося пространства
                if (height < maxHeight)
                {
                    if (findex >= 0)
                    {
                        height -= PanelOffset;
                        while (findex >= 0)
                        {
                            FrameworkElement child = GetChild(-1, findex);
                            UpdateLayoutItemsPanel();
                            height += child.ActualHeight;
                            TopItemIndex = findex;
                            ViewCount++;
                            findex--;
                            if (height > maxHeight)
                            {
                                ScrollOffset = (height - maxHeight) / child.ActualHeight;
                                sizeViewTopChild = 1 - ScrollOffset;
                                PanelOffset = 0 - child.ActualHeight * ScrollOffset;
                                height += PanelOffset;
                                break;
                            }
                        }
                    }
                    else if (PanelOffset < 0)
                    {
                        if (sizeTopChild == 0 || (height - PanelOffset - maxHeight) / sizeTopChild < 0)
                        {
                            ScrollOffset = 0;
                            PanelOffset = 0;
                            sizeViewTopChild = 1;
                        }
                        else
                        {
                            height -= PanelOffset;
                            ScrollOffset = (height - maxHeight) / sizeTopChild;
                            System.Diagnostics.Debug.WriteLine(ScrollOffset);
                            sizeViewTopChild = 1 - ScrollOffset;
                            PanelOffset = 0 - sizeTopChild * ScrollOffset;
                            height += PanelOffset;
                        }
                    }
                }

                //Обновление вертикального скрулла
                sizeViewTopChild = Double.IsNaN(sizeViewTopChild) || Double.IsInfinity(sizeViewTopChild) ? 0 : sizeViewTopChild;
                ScrollViewCount = ViewCount;
                if (PanelOffset <= 0)
                    ScrollViewCount--;
                if (height > maxHeight)
                    ScrollViewCount--;
                if (GetLastChild() is FrameworkElement lastChild)
                    sizeViewLastChild = height > maxHeight ? (lastChild.ActualHeight - height + maxHeight) / lastChild.ActualHeight : 0;
                ScrollViewCount += sizeViewLastChild + sizeViewTopChild;
                if (ScrollViewCount < 0)
                    ScrollViewCount = 0;
                ItemsVSB.Maximum = CollectionCount - ScrollViewCount;
                ItemsVSB.Value = TopItemIndex + ScrollOffset;
                if (ItemsVSB.Track != null && ItemsVSB.Track.ActualHeight > 0)
                {
                    double ThumbH = ScrollViewCount / (double)CollectionCount * ItemsVSB.Track.ActualHeight;
                    if (Double.IsNaN(ThumbH) || Double.IsInfinity(ThumbH)) ThumbH = 0;
                    if (ItemsVSB.Track.ActualHeight - ThumbH == 0)
                        ItemsVSB.ViewportSize = CollectionCount;
                    else
                        ItemsVSB.ViewportSize = ThumbH * (ItemsVSB.Maximum - ItemsVSB.Minimum) / (ItemsVSB.Track.ActualHeight - ThumbH);
                }

                //Смещение панели
                ItemsPanel.Margin = new Thickness(0, PanelOffset, 0, 0);

                //Обновление горизонтального скрулла
                RefreshHScroll();

                ViewRefreshed?.Invoke(this, new EventArgs());
                VerticalScroll?.Invoke(this, new ScrollEventArgs(ScrollEventType.ThumbTrack, ItemsVSB.Value));
            }
        }

        internal virtual FrameworkElement GetChild(int indexChild, int indexSource)
        {
            //Создание элемента
            FrameworkElement itemControl = null;
            if (indexChild == -1)
            {
                if (ItemsPanel.Children.Count > 0 && ItemsPanel.Children[ItemsPanel.Children.Count - 1].Visibility == Visibility.Collapsed)
                {
                    itemControl = ItemsPanel.Children[ItemsPanel.Children.Count - 1] as FrameworkElement;
                    itemControl.Visibility = Visibility.Visible;
                    ItemsPanel.Children.RemoveAt(ItemsPanel.Children.Count - 1);
                }
                else
                    itemControl = CreateChild();
                ItemsPanel.Children.Insert(0, itemControl);
            }
            else if (indexChild >= 0 && indexChild < ItemsPanel.Children.Count)
            {
                ItemsPanel.Children[indexChild].Visibility = Visibility.Visible;
                itemControl = ItemsPanel.Children[indexChild] as FrameworkElement;
            }
            else
            {
                itemControl = CreateChild();
                ItemsPanel.Children.Add(itemControl);
            }
            itemControl.UpdateLayout();

            //Задание контекста данных для представления контента
            if (TypeManager.GetChildOfType<ContentPresenter>(itemControl) is FrameworkElement presenther && 
                ItemsCollection.GetItem(indexSource) is object item && 
                presenther.DataContext != item)
            {
                if (presenther.DataContext is INotifyPropertyChanged oldChanged)
                    oldChanged.PropertyChanged -= Item_PropertyChanged;
                if (item is INotifyPropertyChanged newChanged)
                    newChanged.PropertyChanged += Item_PropertyChanged;
                presenther.DataContext = item;
            }

            //Задание контекста данных для контейнера
            ViewItem viewItem = ItemsCollection.GetViewItem(indexSource);
            if (itemControl.DataContext != viewItem)
            {
                if (viewItem != null && itemControl is DefaultItem defaultItem)
                    defaultItem.InitSelctable();
                if (itemControl.DataContext is INotifyPropertyChanged oldViewChanged)
                    oldViewChanged.PropertyChanged -= Item_PropertyChanged;
                if (viewItem is INotifyPropertyChanged viewChanged)
                    viewChanged.PropertyChanged += Item_PropertyChanged;
                itemControl.DataContext = viewItem;
            }

            return itemControl;
        }

        private FrameworkElement CreateChild()
        {
            FrameworkElement child = null;
            if (itemTemplate != null)
                child = ItemTemplate.LoadContent() as FrameworkElement;
            if (child == null)
                child = new DefaultItem();
            child.MouseDown += Child_MouseDown;
            child.MouseUp += Child_MouseUp;
            return child;
        }

        private FrameworkElement GetLastChild()
        {
            for (int i = ItemsPanel.Children.Count - 1; i >= 0; i--)
                if (ItemsPanel.Children[i].Visibility == Visibility.Visible)
                    return ItemsPanel.Children[i] as FrameworkElement;
            return null;
        }

        private void ClearChild()
        {
            for (int i = 0; i < ItemsPanel.Children.Count; i++)
                if (ItemsPanel.Children[i] is FrameworkElement itemControl)
                {
                    itemControl.MouseDown -= Child_MouseDown;
                    itemControl.MouseUp -= Child_MouseUp;
                    if (itemControl.DataContext is INotifyPropertyChanged viewChanged)
                        viewChanged.PropertyChanged -= Item_PropertyChanged;
                    itemControl.DataContext = null;
                    if (TypeManager.GetChildOfType<ContentPresenter>(itemControl) is FrameworkElement presenther)
                    {
                        if (presenther.DataContext is INotifyPropertyChanged presentherChanged)
                            presentherChanged.PropertyChanged -= Item_PropertyChanged;
                        presenther.DataContext = null;
                    }
                    itemControl.ContextMenu = null;
                }
            ItemsPanel.Children.Clear();
            Refresh();
        }

        private void UpdateLayoutItemsPanel()
        {
            Size size = new Size(ItemsBorder.ActualWidth, ItemsBorder.ActualHeight);
            ItemsBorder.Measure(size);
            //ItemsBorder.Arrange(new Rect(size));
            ItemsBorder.UpdateLayout();
        }
    }
}