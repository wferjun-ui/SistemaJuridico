using SistemaJuridico.ViewModels;
using System.Windows;
using System.Windows.Controls;
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
    }
}
