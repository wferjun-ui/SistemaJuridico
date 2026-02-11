using SistemaJuridico.ViewModels;
using System.Windows;

namespace SistemaJuridico.Views
{
    public partial class DashboardWindow : Window
    {
        public string Usuario =>
            $"Logado como: {App.Session.UsuarioAtual?.Email}";

        public DashboardWindow()
        {
            InitializeComponent();

            var vm = new DashboardViewModel();

            // Passa usuário para o binding
            this.DataContext = vm;

            // Permite binding do texto Usuario
            this.DataContext = vm;
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
