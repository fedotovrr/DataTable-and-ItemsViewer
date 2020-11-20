using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Windows.Data;
using System.Runtime.CompilerServices;

namespace ItemsViewer
{
    internal class DefaultItem : Grid
    {
        public DefaultItem()
        {
            Background = new SolidColorBrush(new Color { A = 0, R = 0, G = 0, B = 0  });

            ContentPresenter contentPresenter = new ContentPresenter();
            TextBlock textBlock = new TextBlock();

            Children.Add(contentPresenter);
            contentPresenter.Content = textBlock;
            
            Binding c = new Binding();
            textBlock.SetBinding(TextBlock.TextProperty, c);
        }

        public void InitSelctable()
        {
            Border selectBorder = new Border() { Background = Brushes.LightGray, Visibility = Visibility.Collapsed };
            Border focusBorder = new Border() { BorderThickness = new Thickness(1), BorderBrush = Brushes.Gray, Visibility = Visibility.Collapsed };
            Children.Insert(0, focusBorder);
            Children.Insert(0, selectBorder);

            Binding bs = new Binding(nameof(ViewItem.Selected)) { Converter = new BooleanToVisibilityConverter() };
            selectBorder.SetBinding(Border.VisibilityProperty, bs);

            Binding bf = new Binding(nameof(ViewItem.Focused)) { Converter = new BooleanToVisibilityConverter() };
            focusBorder.SetBinding(Border.VisibilityProperty, bf);
        }
    }
}
