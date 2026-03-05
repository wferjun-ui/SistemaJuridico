using SistemaJuridico.ViewModels;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace SistemaJuridico.Views
{
    public partial class VerificacoesView : UserControl
    {
        public VerificacoesView()
        {
            InitializeComponent();
            DataContext = new VerificacoesViewModel();
        }

        private void DatePicker_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is DatePicker datePicker)
                DatePickerInputHelper.Configure(datePicker);
        }

        private void QuantidadeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = e.Text.Any(c => !char.IsDigit(c));
        }
    }
}
