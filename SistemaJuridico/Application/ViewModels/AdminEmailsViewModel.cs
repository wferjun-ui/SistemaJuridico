using CommunityToolkit.Mvvm.ComponentModel;
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
            _service = new AutorizacaoService(App.DB);
            Carregar();
        }

        private void Carregar()
        {
            using var conn = App.DB.GetConnection();

            var lista = conn.Query<string>("SELECT email FROM emails_autorizados");

            Emails.Clear();
            foreach (var e in lista)
                Emails.Add(e);
        }

        [RelayCommand]
        private void Adicionar()
        {
            if (string.IsNullOrWhiteSpace(NovoEmail))
                return;

            _service.AdicionarEmail(NovoEmail);

            Emails.Add(NovoEmail);
            NovoEmail = "";
        }
    }
}

