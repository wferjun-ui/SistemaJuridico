using SistemaJuridico.ViewModels;
using System.Windows;

namespace SistemaJuridico.Views
{
    public partial class DashboardWindow : Window
    {
        private DashboardViewModel VM => (DashboardViewModel)DataContext;

        public DashboardWindow()
        {
            InitializeComponent();
            DataContext = new DashboardViewModel();
        }

        private void NovoProcesso_Click(object sender, RoutedEventArgs e)
        {
            new CadastroProcessoWindow().ShowDialog();
            VM.CarregarCommand.Execute(null);
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
