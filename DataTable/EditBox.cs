using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace DataTable
{
    internal class EditBox : Border
    {
        public TextBox eTextBox;
        public EventHandler MyLostFocus;

        public event SelectionChangedEventHandler SelectionChanged;

        public string NewText => eTextBox?.Text;

        public string OldText;

        public EditBox(CellInfo info, string[] values)
        {
            if (values == null || values.Length == 0)
            {
                eTextBox = new TextBox() { Style = Application.Current.TryFindResource(typeof(TextBox)) as Style};
                eTextBox.ApplyTemplate();
                Child = eTextBox;
            }
            else
            {
                ComboBox box = new ComboBox() { Style = Application.Current.TryFindResource(typeof(ComboBox)) as Style, IsEditable = true, FocusVisualStyle = null, BorderThickness = new Thickness(0), Margin = new Thickness(0), Padding = new Thickness(0) };
                box.ApplyTemplate();
                eTextBox = box.Template.FindName("PART_EditableTextBox", box) as TextBox;
                eTextBox.ApplyTemplate();
                box.LostFocus += _LostFocus;
                box.SelectionChanged += Box_SelectionChanged;
                for (int i = 0; i < values.Length; i++)
                    box.Items.Add(values[i]);
                Child = box;
            }
            ScrollViewer viewer = eTextBox.Template.FindName("PART_ContentHost", eTextBox) as ScrollViewer;
            viewer.VerticalAlignment = VerticalAlignment.Top;
            viewer.Padding = new Thickness(0);
            viewer.Margin = new Thickness(0);
            eTextBox.FocusVisualStyle = null;
            eTextBox.BorderThickness = new Thickness(0);
            eTextBox.Margin = new Thickness(1,0,0,1);
            eTextBox.Padding = new Thickness(0);
            eTextBox.IsReadOnly = info.ReadOnly;
            eTextBox.Text = OldText = info.Value;
            eTextBox.TextWrapping = info.TextWrapping;
            eTextBox.TextAlignment = info.TextAlignment;
            eTextBox.LostFocus += _LostFocus;
            eTextBox.KeyDown += ETextBox_KeyDown;
            DataObject.AddPastingHandler(eTextBox, OnPaste);
            eTextBox.Focus();
            eTextBox.CaretIndex = info.Value != null ? info.Value.Length : 0;
        }

        private void Box_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(sender, e);
        }

        private void ETextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && Keyboard.IsKeyDown(Key.Enter))
            {
                eTextBox.Text += "\r\n";
                eTextBox.SelectionStart = eTextBox.Text.Length;
                e.Handled = true;
            }
        }

        private void _LostFocus(object sender, RoutedEventArgs e)
        {
            if (!IsFocused && !eTextBox.IsFocused)
                MyLostFocus?.Invoke(this, e);
        }

        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            bool isText = e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, true);
            if (!isText || eTextBox == null) return;
            string text = e.SourceDataObject.GetData(DataFormats.UnicodeText) as string;
            string bt = eTextBox.Text;
            if (eTextBox.SelectionLength > 0)
            {
                int ci = eTextBox.SelectionStart;
                eTextBox.Text = bt.Substring(0, ci) + text + bt.Substring(ci + eTextBox.SelectionLength, bt.Length - ci - eTextBox.SelectionLength);
                eTextBox.CaretIndex += ci + text.Length;
            }
            else
            {
                int ci = eTextBox.CaretIndex;
                eTextBox.Text = bt.Substring(0, ci) + text + bt.Substring(ci, bt.Length - eTextBox.CaretIndex);
                eTextBox.CaretIndex += ci + text.Length;
            }
            e.CancelCommand();
        }
    }
}
