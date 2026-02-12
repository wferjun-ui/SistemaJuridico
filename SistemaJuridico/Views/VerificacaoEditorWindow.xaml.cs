using SistemaJuridico.ViewModels;
using System.Windows;
using UserControl = System.Windows.Controls.UserControl;

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
    }
}
