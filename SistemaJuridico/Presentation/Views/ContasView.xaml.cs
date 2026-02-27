using SistemaJuridico.ViewModels;
using System.Windows;
using System.Windows.Controls;
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

        private void DatePicker_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is DatePicker dp && !dp.IsDropDownOpen)
            {
                dp.IsDropDownOpen = true;
                e.Handled = true;
            }
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
