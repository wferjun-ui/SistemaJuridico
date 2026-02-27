using SistemaJuridico.Services;
using SistemaJuridico.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
            {
                e.Cancel = true;
                return;
            }

            _vm.LiberarLock();

            base.OnClosing(e);
        }

        private void Fechar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void VerificacaoScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not ScrollViewer scrollViewer)
                return;

            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }
}
