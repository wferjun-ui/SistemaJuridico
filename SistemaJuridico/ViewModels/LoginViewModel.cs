using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaJuridico.Services;
using SistemaJuridico.Views;
using System.Windows;

namespace SistemaJuridico.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly AuthService _auth;

        [ObservableProperty]
        private string usuario = "";

        [ObservableProperty]
        private string senha = "";

        public LoginViewModel()
        {
            _auth = new AuthService(App.DB);
        }

        [RelayCommand]
        private void Entrar()
        {
            var user = _auth.Login(Usuario, Senha);

            if (user == null)
            {
                MessageBox.Show("Usuário ou senha inválidos.");
                return;
            }

            App.Session.SetUsuario(user);

            var dash = new DashboardWindow();
            dash.Show();

            Application.Current.MainWindow?.Close();
        }
    }
}

