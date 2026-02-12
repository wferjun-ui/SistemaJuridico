using SistemaJuridico.ViewModels;
using System.Windows;
using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;

namespace SistemaJuridico.Views
{
    public partial class MigracaoView : UserControl
    {
        public MigracaoView()
        {
            InitializeComponent();
            Loaded += MigracaoView_Loaded;
        }

        private void MigracaoView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MigracaoViewModel vm)
            {
                vm.MigracaoConcluidaComSucesso += Vm_MigracaoConcluidaComSucesso;
            }
        }

        private void Vm_MigracaoConcluidaComSucesso()
        {
            var main = new MainWindow();
            main.Show();

            Window.GetWindow(this)?.Close();
        }
    }
}
