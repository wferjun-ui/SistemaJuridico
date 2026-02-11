using SistemaJuridico.Services;
using SistemaJuridico.ViewModels;
using System.ComponentModel;
using System.Windows;

namespace SistemaJuridico.Views
{
    public partial class ProcessoDetalhesWindow : Window
    {
        private readonly ProcessoDetalhesViewModel _vm;

        public ProcessoDetalhesWindow(string processoId)
        {
            InitializeComponent();

            var db = new DatabaseService();
            var processService = new ProcessService(db);

            _vm = new ProcessoDetalhesViewModel(processoId, processService);

            DataContext = _vm;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_vm.PodeFechar())
                e.Cancel = true;

            base.OnClosing(e);
        }
    }
}
