using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
using CommunityToolkit.Mvvm.Input;
using SistemaJuridico.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace SistemaJuridico.ViewModels
{
    public partial class AdminEmailsViewModel : ObservableObject
    {
        private readonly AutorizacaoService _service;

        public ObservableCollection<string> Emails { get; } = new();

        [ObservableProperty]
        private string novoEmail = "";

        public AdminEmailsViewModel()
        {
            _service = new AutorizacaoService(new DatabaseService());
            Carregar();
        }

        private void Carregar()
        {
            using var conn = new DatabaseService().GetConnection();

            var lista = conn.Query<string>("SELECT email FROM emails_autorizados ORDER BY email");

            Emails.Clear();
            foreach (var e in lista)
                Emails.Add(e);
        }

        [RelayCommand]
        private void Adicionar()
        {
            if (!App.Session.IsAdmin())
            {
                System.Windows.MessageBox.Show("Apenas administradores podem alterar e-mails autorizados.");
                return;
            }

            if (string.IsNullOrWhiteSpace(NovoEmail))
                return;

            var email = NovoEmail.Trim();
            var incluido = _service.AdicionarEmail(email);

            if (!incluido)
            {
                System.Windows.MessageBox.Show("E-mail já cadastrado ou inválido.");
                return;
            }

            Emails.Add(email);
            NovoEmail = "";
        }
    }
}
