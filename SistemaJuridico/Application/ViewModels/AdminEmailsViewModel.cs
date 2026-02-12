using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dapper;
using SistemaJuridico.Models;
using SistemaJuridico.Services;
using System.Collections.ObjectModel;

namespace SistemaJuridico.ViewModels
{
    public partial class AdminEmailsViewModel : ObservableObject
    {
        private readonly AutorizacaoService _autorizacaoService;
        private readonly AuthService _authService;

        public ObservableCollection<string> Emails { get; } = new();
        public ObservableCollection<Usuario> Usuarios { get; } = new();
        public IReadOnlyList<string> PerfisDisponiveis { get; } = new[] { "Admin", "Operador", "Leitura" };

        [ObservableProperty]
        private string novoEmail = "";

        [ObservableProperty]
        private Usuario? usuarioSelecionado;

        [ObservableProperty]
        private string novoPerfil = "Operador";

        [ObservableProperty]
        private string novaSenha = "";

        public AdminEmailsViewModel()
        {
            var db = new DatabaseService();
            _autorizacaoService = new AutorizacaoService(db);
            _authService = new AuthService(db.ConnectionString);

            CarregarEmails();
            CarregarUsuarios();
        }

        [RelayCommand]
        private void Recarregar()
        {
            CarregarEmails();
            CarregarUsuarios();
        }

        [RelayCommand]
        private void AdicionarEmail()
        {
            if (!ValidarAdmin())
                return;

            if (string.IsNullOrWhiteSpace(NovoEmail))
                return;

            var incluido = _autorizacaoService.AdicionarEmail(NovoEmail.Trim());
            if (!incluido)
            {
                System.Windows.MessageBox.Show("E-mail já cadastrado ou inválido.");
                return;
            }

            NovoEmail = "";
            CarregarEmails();
        }

        [RelayCommand]
        private void RemoverEmail(string? email)
        {
            if (!ValidarAdmin())
                return;

            if (string.IsNullOrWhiteSpace(email))
                return;

            if (_autorizacaoService.RemoverEmail(email.Trim()))
                CarregarEmails();
        }

        [RelayCommand]
        private void AlterarPerfilUsuario()
        {
            if (!ValidarAdmin())
                return;

            if (UsuarioSelecionado == null)
            {
                System.Windows.MessageBox.Show("Selecione um usuário.");
                return;
            }

            if (string.IsNullOrWhiteSpace(NovoPerfil))
            {
                System.Windows.MessageBox.Show("Selecione um perfil válido.");
                return;
            }

            _authService.AlterarPerfil(UsuarioSelecionado.Id, NovoPerfil);
            CarregarUsuarios();
            System.Windows.MessageBox.Show("Perfil atualizado.");
        }

        [RelayCommand]
        private void AlterarSenhaUsuario()
        {
            if (!ValidarAdmin())
                return;

            if (UsuarioSelecionado == null)
            {
                System.Windows.MessageBox.Show("Selecione um usuário.");
                return;
            }

            if (string.IsNullOrWhiteSpace(NovaSenha))
            {
                System.Windows.MessageBox.Show("Informe a nova senha.");
                return;
            }

            _authService.AlterarSenha(UsuarioSelecionado.Id, NovaSenha);
            NovaSenha = "";
            System.Windows.MessageBox.Show("Senha alterada com sucesso.");
        }

        [RelayCommand]
        private void ExcluirUsuario()
        {
            if (!ValidarAdmin())
                return;

            if (UsuarioSelecionado == null)
            {
                System.Windows.MessageBox.Show("Selecione um usuário.");
                return;
            }

            var usuarioAtual = App.Session.UsuarioAtual?.Id;
            if (UsuarioSelecionado.Id == usuarioAtual)
            {
                System.Windows.MessageBox.Show("Você não pode excluir o próprio usuário logado.");
                return;
            }

            var confirmar = System.Windows.MessageBox.Show(
                $"Deseja excluir o usuário '{UsuarioSelecionado.Username}'?",
                "Confirmação",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (confirmar != System.Windows.MessageBoxResult.Yes)
                return;

            _authService.ExcluirUsuario(UsuarioSelecionado.Id);
            CarregarUsuarios();
        }

        partial void OnUsuarioSelecionadoChanged(Usuario? value)
        {
            if (value == null)
                return;

            NovoPerfil = value.Perfil;
            NovaSenha = "";
        }

        private bool ValidarAdmin()
        {
            if (App.Session.IsAdmin())
                return true;

            System.Windows.MessageBox.Show("Apenas administradores podem acessar este recurso.");
            return false;
        }

        private void CarregarEmails()
        {
            using var conn = new DatabaseService().GetConnection();
            var lista = conn.Query<string>("SELECT email FROM emails_autorizados ORDER BY email");

            Emails.Clear();
            foreach (var item in lista)
                Emails.Add(item);
        }

        private void CarregarUsuarios()
        {
            var usuarios = _authService.ListarUsuarios();

            Usuarios.Clear();
            foreach (var u in usuarios)
                Usuarios.Add(u);
        }
    }
}
