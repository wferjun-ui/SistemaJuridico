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
    }
}

