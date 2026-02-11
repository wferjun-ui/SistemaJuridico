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

            _vm = new ProcessoDetalhesViewModel(processoId);
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
