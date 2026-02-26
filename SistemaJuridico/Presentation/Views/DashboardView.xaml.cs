using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using SistemaJuridico.ViewModels;

namespace SistemaJuridico.Views
{
    public partial class DashboardView : System.Windows.Controls.UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            DataContextChanged += DashboardView_DataContextChanged;
            Unloaded += DashboardView_Unloaded;
        }

        private void DashboardView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is DashboardViewModel oldVm)
                oldVm.PropertyChanged -= DashboardViewModel_PropertyChanged;

            if (e.NewValue is DashboardViewModel newVm)
                newVm.PropertyChanged += DashboardViewModel_PropertyChanged;
        }

        private void DashboardViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(DashboardViewModel.ProcessoDetalhesSelecionado))
                return;

            if (DataContext is not DashboardViewModel vm || vm.ProcessoDetalhesSelecionado == null)
                return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                DetalhesProcessoContainer.BringIntoView();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void DashboardView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is DashboardViewModel vm)
            {
                vm.PropertyChanged -= DashboardViewModel_PropertyChanged;
                vm.FecharProcessoSelecionado();
            }
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
