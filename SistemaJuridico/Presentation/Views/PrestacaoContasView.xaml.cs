using SistemaJuridico.ViewModels;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TextBox = System.Windows.Controls.TextBox;
using UserControl = System.Windows.Controls.UserControl;

namespace SistemaJuridico.Views
{
    public partial class PrestacaoContasView : UserControl
    {
        private static readonly Regex NumericRegex = new("^[0-9]*(?:[\\.,][0-9]*)?$", RegexOptions.Compiled);

        public static readonly DependencyProperty ProcessoIdProperty = DependencyProperty.Register(
            nameof(ProcessoId),
            typeof(string),
            typeof(PrestacaoContasView),
            new PropertyMetadata(string.Empty, OnProcessoIdChanged));

        public string ProcessoId
        {
            get => (string)GetValue(ProcessoIdProperty);
            set => SetValue(ProcessoIdProperty, value);
        }

        public PrestacaoContasView()
        {
            InitializeComponent();
            DataContext = new PrestacaoContasViewModel();
        }

        private static void OnProcessoIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PrestacaoContasView view && e.NewValue is string processoId)
                view.DataContext = new PrestacaoContasViewModel(processoId, App.Session.UsuarioAtual?.Nome ?? "Sistema");
        }

        private void NumericTextInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;

            var proposed = textBox.Text.Insert(textBox.SelectionStart, e.Text);
            e.Handled = !NumericRegex.IsMatch(proposed);
        }

        private void IncrementarQuantidade_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is PrestacaoContasViewModel vm)
                vm.TratamentoEmEdicao.Quantidade += 1;
        }

        private void DecrementarQuantidade_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is PrestacaoContasViewModel vm && vm.TratamentoEmEdicao.Quantidade > 0)
                vm.TratamentoEmEdicao.Quantidade -= 1;
        }

        private void DatePicker_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is DatePicker datePicker)
                DatePickerInputHelper.Configure(datePicker);
        }
    }
}
