using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;
using UserControl = System.Windows.Controls.UserControl;

namespace SistemaJuridico.Views
{
    public partial class ProcessoDetalhesView : UserControl
    {
        public ProcessoDetalhesView()
        {
            InitializeComponent();
        }

        private void IncrementarQuantidade_Click(object sender, RoutedEventArgs e)
            => AjustarQuantidadeNoCampo(sender, 1);

        private void DecrementarQuantidade_Click(object sender, RoutedEventArgs e)
            => AjustarQuantidadeNoCampo(sender, -1);

        private static void AjustarQuantidadeNoCampo(object sender, int delta)
        {
            if (sender is not Button { CommandParameter: TextBox textBox })
                return;

            if (!int.TryParse(textBox.Text, out var valorAtual))
                valorAtual = 0;

            var novoValor = Math.Max(0, valorAtual + delta);
            textBox.Text = novoValor.ToString();
            BindingOperations.GetBindingExpression(textBox, TextBox.TextProperty)?.UpdateSource();
        }
    }

    public class QuantidadeFaltanteConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var qtdTotal = ParseInteiro(values, 0);
            var qtdSus = ParseInteiro(values, 1);
            var qtdParticular = ParseInteiro(values, 2);
            var faltam = Math.Max(0, qtdTotal - (qtdSus + qtdParticular));
            return faltam.ToString();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();

        private static int ParseInteiro(object[] values, int index)
        {
            if (index >= values.Length || values[index] is null)
                return 0;

            return int.TryParse(values[index].ToString(), out var numero) ? Math.Max(0, numero) : 0;
        }
    }
}
