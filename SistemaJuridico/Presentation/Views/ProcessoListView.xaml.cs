using SistemaJuridico.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;
using UserControl = System.Windows.Controls.UserControl;

namespace SistemaJuridico.Views
{
    public partial class ProcessoListView : UserControl
    {
        public ProcessoListView()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not ProcessoListViewModel vm)
                return;

            if (sender is DataGrid grid && grid.SelectedItem is ProcessoBuscaItemVM processo)
                vm.AbrirProcessoCommand.Execute(processo);
        }
    }
}
