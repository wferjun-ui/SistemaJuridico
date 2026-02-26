using System.Windows;
using System.Windows.Controls;
using SistemaJuridico.ViewModels;

namespace SistemaJuridico.Views
{
    public partial class DashboardView : System.Windows.Controls.UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
        }

        private void SearchTextBox_OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (DataContext is DashboardViewModel vm)
            {
                vm.ExibirListaBuscaCompleta();
            }
        }
    }
}
