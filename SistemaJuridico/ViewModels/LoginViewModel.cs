using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaJuridico.Services;
using SistemaJuridico.Views;
using System;

namespace SistemaJuridico.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly AuthService _auth;

        public event EventHandler? LoginSucesso;

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
                System.Windows.MessageBox.Show("Usuário ou senha inválidos.");
                return;
            }

            App.Session.SetUsuario(user);
            LoginSucesso?.Invoke(this, EventArgs.Empty);

            var dash = new DashboardWindow();
            dash.Show();

            System.Windows.Application.Current.MainWindow?.Close();
        }
    }
}
