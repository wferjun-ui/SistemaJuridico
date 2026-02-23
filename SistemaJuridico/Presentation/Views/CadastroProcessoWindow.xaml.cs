using SistemaJuridico.Infrastructure;
using SistemaJuridico.ViewModels;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace SistemaJuridico.Views
{
    public partial class CadastroProcessoWindow : Window
    {
        public CadastroProcessoWindow()
        {
            InitializeComponent();

            var vm = new CadastroProcessoViewModel(
                ServiceLocator.ProcessService,
                ServiceLocator.ItemSaudeService,
                ServiceLocator.VerificacaoService,
                ServiceLocator.HistoricoService);

            vm.FecharTela = Close;
            DataContext = vm;

            vm.ItensSaudeCadastro.CollectionChanged += ItensSaudeCadastro_CollectionChanged;
            Closed += (_, _) => vm.ItensSaudeCadastro.CollectionChanged -= ItensSaudeCadastro_CollectionChanged;
        }

        private void ItensSaudeCadastro_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems == null || e.NewItems.Count == 0)
                return;

            if (e.NewItems[0] is not SaudeItemCadastroViewModel novoItem)
                return;

            Dispatcher.BeginInvoke(() =>
            {
                if (ItensSaudeDataGrid.Columns.Count == 0)
                    return;

                ItensSaudeDataGrid.SelectedItem = novoItem;
                ItensSaudeDataGrid.CurrentCell = new DataGridCellInfo(novoItem, ItensSaudeDataGrid.Columns[0]);
                ItensSaudeDataGrid.ScrollIntoView(novoItem, ItensSaudeDataGrid.Columns[0]);
                ItensSaudeDataGrid.Focus();
                ItensSaudeDataGrid.BeginEdit();
            });
        }
    }
}
