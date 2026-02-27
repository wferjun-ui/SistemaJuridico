using SistemaJuridico.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ScrollBar = System.Windows.Controls.Primitives.ScrollBar;
using UserControl = System.Windows.Controls.UserControl;
using TextBox = System.Windows.Controls.TextBox;

namespace SistemaJuridico.Views
{
    public partial class ContasView : UserControl
    {
        public static readonly DependencyProperty ProcessoIdProperty =
            DependencyProperty.Register(
                "ProcessoId",
                typeof(string),
                typeof(ContasView),
                new PropertyMetadata(OnProcessoIdChanged));

        public string ProcessoId
        {
            get => (string)GetValue(ProcessoIdProperty);
            set => SetValue(ProcessoIdProperty, value);
        }

        public ContasView()
        {
            InitializeComponent();
        }

        private static void OnProcessoIdChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is ContasView view && e.NewValue is string id)
                view.DataContext = new ContasViewModel(id);
        }

        private void ValorAlvara_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DataContext is not ContasViewModel vm || sender is not TextBox box)
                return;

            vm.AtualizarValorAlvaraTexto(box.Text);
        }

        private void ValorConta_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DataContext is not ContasViewModel vm || sender is not TextBox box)
                return;

            vm.AtualizarValorContaTexto(box.Text);
        }

        private void DatePicker_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not DatePicker dp) return;
            dp.ApplyTemplate();

            // Encontra o DatePickerTextBox interno e faz o clique nele abrir o calendário
            var textBox = dp.Template.FindName("PART_TextBox", dp) as DatePickerTextBox;
            if (textBox != null)
            {
                textBox.PreviewMouseLeftButtonDown += (s, args) =>
                {
                    if (!dp.IsDropDownOpen)
                        dp.IsDropDownOpen = true;
                };
            }
        }

        private void IncrementarQuantidade_Click(object sender, RoutedEventArgs e)
            => AjustarQuantidadeNoCampo(sender, 1);

        private void DecrementarQuantidade_Click(object sender, RoutedEventArgs e)
            => AjustarQuantidadeNoCampo(sender, -1);

        private static void AjustarQuantidadeNoCampo(object sender, int delta)
        {
            if (sender is not Button btn || btn.CommandParameter is not TextBox targetBox)
                return;

            if (int.TryParse(targetBox.Text, out var current))
                targetBox.Text = Math.Max(0, current + delta).ToString();
            else
                targetBox.Text = delta > 0 ? "1" : "0";

            targetBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        }

        private void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject ?? sender as DependencyObject;
            if (source == null)
                return;

            var verticalScrollBar = FindVisualParent<ScrollBar>(source);
            if (verticalScrollBar is { IsMouseOver: true })
                return;

            var scrollViewer = FindVisualParent<ScrollViewer>(source);
            if (scrollViewer == null)
                return;

            e.Handled = true;
            var forwarded = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = UIElement.MouseWheelEvent,
                Source = sender
            };
            scrollViewer.RaiseEvent(forwarded);
        }

        private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T typed)
                    return typed;

                child = System.Windows.Media.VisualTreeHelper.GetParent(child);
            }

            return null;
        }
    }
}
