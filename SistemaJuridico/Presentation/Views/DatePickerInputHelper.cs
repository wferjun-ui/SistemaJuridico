using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using WpfButton = System.Windows.Controls.Button;
using WpfDataObject = System.Windows.DataObject;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;

namespace SistemaJuridico.Views
{
    internal static class DatePickerInputHelper
    {
        public static void Configure(DatePicker datePicker)
        {
            datePicker.ApplyTemplate();

            var button = datePicker.Template.FindName("PART_Button", datePicker) as WpfButton;
            if (button != null)
                button.Visibility = Visibility.Collapsed;

            var textBox = datePicker.Template.FindName("PART_TextBox", datePicker) as DatePickerTextBox;
            if (textBox == null)
                return;

            textBox.HorizontalAlignment = WpfHorizontalAlignment.Stretch;

            textBox.PreviewTextInput -= OnPreviewTextInput;
            textBox.PreviewTextInput += OnPreviewTextInput;

            WpfDataObject.RemovePastingHandler(textBox, OnPaste);
            WpfDataObject.AddPastingHandler(textBox, OnPaste);

            textBox.TextChanged -= OnTextChanged;
            textBox.TextChanged += OnTextChanged;

            textBox.GotKeyboardFocus -= OnGotKeyboardFocus;
            textBox.GotKeyboardFocus += OnGotKeyboardFocus;

            textBox.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            textBox.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
        }


        private static void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is not DatePickerTextBox textBox || textBox.TemplatedParent is not DatePicker datePicker)
                return;

            datePicker.IsDropDownOpen = true;
        }

        private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not DatePickerTextBox textBox || textBox.TemplatedParent is not DatePicker datePicker)
                return;

            if (!textBox.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                textBox.Focus();
            }

            datePicker.IsDropDownOpen = true;
        }

        private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = e.Text.Any(c => !char.IsDigit(c));
        }

        private static void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(typeof(string)))
            {
                e.CancelCommand();
                return;
            }

            var pastedText = (e.DataObject.GetData(typeof(string)) as string) ?? string.Empty;
            if (pastedText.Any(c => !char.IsDigit(c) && c != '/'))
                e.CancelCommand();
        }

        private static void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not DatePickerTextBox textBox)
                return;

            var raw = new string(textBox.Text.Where(char.IsDigit).ToArray());
            if (raw.Length > 8)
                raw = raw[..8];

            var formatted = raw.Length switch
            {
                <= 2 => raw,
                <= 4 => $"{raw[..2]}/{raw[2..]}",
                _ => $"{raw[..2]}/{raw.Substring(2, 2)}/{raw[4..]}"
            };

            if (textBox.Text == formatted)
                return;

            var caret = formatted.Length;
            textBox.Text = formatted;
            textBox.CaretIndex = caret;
        }
    }
}
