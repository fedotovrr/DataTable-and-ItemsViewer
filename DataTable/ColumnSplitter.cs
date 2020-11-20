using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace DataTable
{
    internal class ColumnSplitter : Border
    {
        /// <summary>
        /// Поинт клика
        /// </summary>
        private double ClickX;

        /// <summary>
        /// Элемент изменения размеров стобцов
        /// </summary>
        public ColumnSplitter()
        {
            Cursor = Cursors.SizeWE;
        }

        /// <summary>
        /// Событие изменения размера прилегающего элемента
        /// </summary>
        public event EventHandler OnResize;


        /// <summary>
        /// Событие нажатия на элемент (захват элемента)
        /// </summary>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            ClickX = this.PointToScreen(Mouse.GetPosition(this)).X;
            CaptureMouse();
        }

        /// <summary>
        /// Событие перемещение мыши над элементом
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (IsMouseCaptured && this.Parent is StackPanel SP && SP.Children.Count > 1)
                if (SP.Children.IndexOf(this) - 1 is int index && index >= 0 && SP.Children[index] is Border b)
                    if (this.PointToScreen(Mouse.GetPosition(this)).X is double DragX && Math.Abs(DragX - ClickX) > 2)
                        if (Math.Round(b.Width + (DragX - ClickX)) is double NewWidth && NewWidth > 0)
                        {
                            b.Width = NewWidth;
                            ClickX = DragX;
                            OnResize?.Invoke(this, new EventArgs());
                        }
        }

        /// <summary>
        /// Событие отпускания мыши на элементе
        /// </summary>
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            ReleaseMouseCapture();
        }
    }
}