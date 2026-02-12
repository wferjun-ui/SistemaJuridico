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
            _auth = new AuthService(new DatabaseService().ConnectionString);
            _autorizacao = new AutorizacaoService(new DatabaseService());
        }

        [RelayCommand]
        private void Cadastrar()
        {
            if (!App.Session.IsAdmin())
            {
                MessageBox.Show("Apenas administradores podem criar usuários.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Username) ||
                string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Senha))
            {
                MessageBox.Show("Preencha usuário, e-mail e senha.");
                return;
            }

            if (!_autorizacao.EmailAutorizado(Email.Trim()))
            {
                MessageBox.Show("E-mail não autorizado.");
                return;
            }

            try
            {
                _auth.CriarUsuario(Username, Email, Senha, Perfil);
                MessageBox.Show("Usuário criado.");
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Erro ao criar usuário: {ex.Message}");
            }
        }
    }
}
