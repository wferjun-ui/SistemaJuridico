using SistemaJuridico.Infrastructure;
using SistemaJuridico.ViewModels;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace SistemaJuridico.Views
{
    public partial class CadastroProcessoWindow : Window
    {
        private const double LarguraBase = 1150d;
        private const double AlturaBase = 780d;

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

            Loaded += (_, _) => AtualizarEscalaLayout();
            SizeChanged += (_, _) => AtualizarEscalaLayout();
        }

        private void AtualizarEscalaLayout()
        {
            if (LayoutScale == null)
                return;

            var escalaLargura = ActualWidth / LarguraBase;
            var escalaAltura = ActualHeight / AlturaBase;
            var escala = Math.Min(escalaLargura, escalaAltura);

            escala = Math.Clamp(escala, 0.75, 1.5);

            LayoutScale.ScaleX = escala;
            LayoutScale.ScaleY = escala;
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
