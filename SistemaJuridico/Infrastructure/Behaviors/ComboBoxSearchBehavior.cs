using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SistemaJuridico.Behaviors
{
    public static class ComboBoxSearchBehavior
    {
        public static readonly DependencyProperty EnableContainsFilterProperty =
            DependencyProperty.RegisterAttached(
                "EnableContainsFilter",
                typeof(bool),
                typeof(ComboBoxSearchBehavior),
                new PropertyMetadata(false, OnEnableContainsFilterChanged));

        public static bool GetEnableContainsFilter(DependencyObject obj)
            => (bool)obj.GetValue(EnableContainsFilterProperty);

        public static void SetEnableContainsFilter(DependencyObject obj, bool value)
            => obj.SetValue(EnableContainsFilterProperty, value);

        private static readonly DependencyProperty LastFilterProperty =
            DependencyProperty.RegisterAttached(
                "LastFilter",
                typeof(string),
                typeof(ComboBoxSearchBehavior),
                new PropertyMetadata(string.Empty));

        private static string GetLastFilter(DependencyObject obj)
            => (string)obj.GetValue(LastFilterProperty);

        private static void SetLastFilter(DependencyObject obj, string value)
            => obj.SetValue(LastFilterProperty, value);

        private static void OnEnableContainsFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ComboBox combo)
                return;

            if ((bool)e.NewValue)
                combo.Loaded += ComboOnLoaded;
            else
                combo.Loaded -= ComboOnLoaded;
        }

        private static void ComboOnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ComboBox combo || !combo.IsEditable)
                return;

            if (combo.Template.FindName("PART_EditableTextBox", combo) is not TextBox textBox)
                return;

            textBox.TextChanged -= TextBoxOnTextChanged;
            textBox.GotKeyboardFocus -= TextBoxOnGotKeyboardFocus;
            combo.DropDownClosed -= ComboOnDropDownClosed;

            textBox.TextChanged += TextBoxOnTextChanged;
            textBox.GotKeyboardFocus += TextBoxOnGotKeyboardFocus;
            combo.DropDownClosed += ComboOnDropDownClosed;
        }

        private static void TextBoxOnGotKeyboardFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox textBox || textBox.TemplatedParent is not ComboBox combo)
                return;

            RefreshFilter(combo, textBox.Text);

            if (combo.HasItems)
                combo.IsDropDownOpen = true;
        }

        private static void TextBoxOnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox || textBox.TemplatedParent is not ComboBox combo)
                return;

            var texto = textBox.Text ?? string.Empty;
            RefreshFilter(combo, texto);

            combo.IsDropDownOpen = combo.HasItems;
        }

        private static void ComboOnDropDownClosed(object? sender, EventArgs e)
        {
            if (sender is not ComboBox combo)
                return;

            ClearFilter(combo);
        }

        private static void RefreshFilter(ComboBox combo, string input)
        {
            if (combo.ItemsSource == null)
                return;

            var view = CollectionViewSource.GetDefaultView(combo.ItemsSource);
            if (view == null)
                return;

            var filtro = input?.Trim() ?? string.Empty;
            SetLastFilter(combo, filtro);

            view.Filter = item =>
            {
                if (item == null)
                    return false;

                if (string.IsNullOrWhiteSpace(GetLastFilter(combo)))
                    return true;

                var texto = item.ToString() ?? string.Empty;
                return texto.Contains(GetLastFilter(combo), StringComparison.OrdinalIgnoreCase);
            };

            view.Refresh();
        }

        private static void ClearFilter(ComboBox combo)
        {
            if (combo.ItemsSource == null)
                return;

            var view = CollectionViewSource.GetDefaultView(combo.ItemsSource);
            if (view == null)
                return;

            SetLastFilter(combo, string.Empty);
            view.Filter = null;
            view.Refresh();
        }
    }
}
