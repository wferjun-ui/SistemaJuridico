using System.Windows;
using System.Windows.Controls;
using SistemaJuridico.ViewModels;

namespace SistemaJuridico.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            Unloaded += DashboardView_Unloaded;
        }

        private void DashboardView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is DashboardViewModel vm)
                vm.FecharProcessoSelecionado();
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
