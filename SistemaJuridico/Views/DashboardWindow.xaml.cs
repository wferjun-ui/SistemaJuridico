using SistemaJuridico.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace SistemaJuridico.Views
{
    public partial class DashboardWindow : Window
    {
        private DashboardViewModel VM => (DashboardViewModel)DataContext;

        public string Usuario =>
            $"Logado como: {App.Session.UsuarioAtual?.Email}";

        public DashboardWindow()
        {
            InitializeComponent();
            DataContext = new DashboardViewModel();
        }

        private void AbrirProcessoDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (VM.Processos == null)
                return;

            if (((System.Windows.Controls.DataGrid)sender).SelectedItem is ProcessoResumoVM proc)
            {
                VM.AbrirProcessoCommand.Execute(proc);
            }
        }

        private void AbrirEmails(object sender, RoutedEventArgs e)
        {
            if (!App.Session.IsAdmin())
            {
                MessageBox.Show("Apenas admin.");
                return;
            }

            new AdminEmailsWindow().ShowDialog();
        }
    }
}
