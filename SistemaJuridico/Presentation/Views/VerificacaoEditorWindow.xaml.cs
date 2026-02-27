using SistemaJuridico.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace SistemaJuridico.Views
{
    public partial class VerificacaoEditorWindow : Window
    {
        public VerificacaoEditorWindow(VerificacaoEditorViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;

            vm.FecharSolicitado = () =>
            {
                DialogResult = true;
                Close();
            };
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void DatePicker_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not DatePicker dp) return;
            dp.ApplyTemplate();

            // Hide the calendar button so the field looks like a plain text input
            var button = dp.Template.FindName("PART_Button", dp) as System.Windows.Controls.Button;
            if (button != null)
                button.Visibility = Visibility.Collapsed;

            // Make the text box fill the available space and open calendar on click
            var textBox = dp.Template.FindName("PART_TextBox", dp) as DatePickerTextBox;
            if (textBox != null)
            {
                textBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                textBox.PreviewMouseLeftButtonDown += (s, args) =>
                {
                    if (!dp.IsDropDownOpen)
                        dp.IsDropDownOpen = true;
                };
            }
        }
    }
}
