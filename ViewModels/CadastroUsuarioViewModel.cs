using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaJuridico.Services;
using System.Windows;

namespace SistemaJuridico.ViewModels
{
    public partial class CadastroUsuarioViewModel : ObservableObject
    {
        private readonly AuthService _auth;
        private readonly AutorizacaoService _autorizacao;

        [ObservableProperty] private string username = "";
        [ObservableProperty] private string email = "";
        [ObservableProperty] private string senha = "";
        [ObservableProperty] private string perfil = "Operador";

        public CadastroUsuarioViewModel()
        {
            _auth = new AuthService(App.DB);
            _autorizacao = new AutorizacaoService(App.DB);
        }

        [RelayCommand]
        private void Cadastrar()
        {
            if (!_autorizacao.EmailAutorizado(Email))
            {
                MessageBox.Show("E-mail não autorizado.");
                return;
            }

            _auth.CriarUsuario(Username, Email, Senha, Perfil);

            MessageBox.Show("Usuário criado.");
        }
    }
}

