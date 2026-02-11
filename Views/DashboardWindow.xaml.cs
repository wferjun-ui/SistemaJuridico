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
            DataContext = this;
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
