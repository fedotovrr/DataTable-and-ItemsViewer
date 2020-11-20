using System;
using System.Collections.ObjectModel;
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
    //Тест на базе ListBox
    internal class Test : ContentControl
    {
        private ListBox _ItemsControl;
        internal Grid HeaderContainer = new Grid();
        internal Grid ControlGrid;
        internal Border ItemsBorder = new Border() { AllowDrop = true, Background = new SolidColorBrush(new Color { A = 0, B = 0, R = 0, G = 0 }) };

        internal virtual ViewCollection ItemsCollection { get; set; }

        public DataTemplate ItemTemplate { get => _ItemsControl.ItemTemplate; set => _ItemsControl.ItemTemplate = value; }

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
                    ItemsCollection.Dispose();
                }
                if (value != null)
                {
                    ItemsCollection = new ViewCollection(value);
                    ItemsCollection.CollectionChanged += ItemCollection_CollectionChanged;
                }
                else
                    ItemsCollection = null;
                _ItemsControl.ItemsSource = ItemsCollection;
                _ItemsControl.PreviewMouseDown += _ItemsControl_MouseDown;
                ItemsSourceChanged?.Invoke(this, null);
            }
        }

        private void _ItemsControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Focus();
        }


        public Test()
        {
            SnapsToDevicePixels = true;
            ClipToBounds = true;
            Focusable = true;
            //FocusVisualStyle = null;

            //элементы
            FrameworkElementFactory factory = new FrameworkElementFactory(typeof(ContentPresenter));
            factory.AddHandler(ContentPresenter.MouseDownEvent, new MouseButtonEventHandler(Child_MouseDown));
            factory.AddHandler(ContentPresenter.MouseUpEvent, new MouseButtonEventHandler(Child_MouseUp));
            _ItemsControl = new ListBox()
            {
                BorderThickness = new Thickness(0),
                Margin = new Thickness(-1, -1, 0, 0),
                ItemContainerStyle = new Style(typeof(ListBoxItem))
                {
                    Setters = {
                        new Setter(ListBoxItem.FocusableProperty, false),
                        new Setter(ListBoxItem.TemplateProperty, new ControlTemplate(typeof(ListBoxItem)) { VisualTree = factory } ),
                    }
                }
            };

            //сетка элемента
            ControlGrid = new Grid();
            ControlGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            ControlGrid.RowDefinitions.Add(new RowDefinition());
            Content = ControlGrid;

            //панель элементов
            ItemsBorder.Child = _ItemsControl;
            Grid.SetRow(ItemsBorder, 2);
            ControlGrid.Children.Add(ItemsBorder);

            //голова
            ControlGrid.Children.Add(HeaderContainer);

            ItemsPanel = new ItemsClass(this);
        }

        internal void ScrollIntoView(ViewItem item)
        {
            if (item != null)
                _ItemsControl.ScrollIntoView(ItemsCollection.GetItem(item.Index));
        }

        public void ScrollIntoView(int index)
        {
            _ItemsControl.ScrollIntoView(ItemsCollection.GetItem(index));
        }

        internal virtual void Child_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        internal virtual void Child_MouseUp(object sender, MouseButtonEventArgs e)
        {

        }

        internal virtual void ItemCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {

        }




        public event EventHandler ViewRefreshed;
        public event EventHandler ItemsSourceChanged;

        internal ScrollBar ItemsHSB { get; private set; } = new ScrollBar { Orientation = Orientation.Horizontal };

        public ItemsClass ItemsPanel { get; private set; }

        internal virtual void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // не требуется завязано свойство актуальности таблицы и селект при итема при дропе
        }

        internal void ItemsHSB_SetValue(double newValue)
        {

        }

        internal void Refresh()
        {
            // не требуется
        }

        public class ItemsClass
        {
            private Test source;

            internal ItemsClass(Test source)
            {
                this.source = source;
            }

            public ReadOnlyCollection<object> Children => source._ItemsControl.ItemContainerGenerator.Items;
        }
    }
}
