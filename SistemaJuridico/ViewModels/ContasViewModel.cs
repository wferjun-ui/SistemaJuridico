using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaJuridico.Models;
using SistemaJuridico.Services;
using System.Collections.ObjectModel;

namespace SistemaJuridico.ViewModels
{
    public partial class ContasViewModel : ObservableObject
    {
        private readonly ContaService _service;

        private readonly string _processoId;

        public ObservableCollection<Conta> Contas { get; } = new();

        [ObservableProperty]
        private Conta _novaConta = new();

        public ContasViewModel(string processoId)
        {
            _processoId = processoId;

            var db = new DatabaseService();
            _service = new ContaService(db);

            Carregar();
        }

        [RelayCommand]
        private void Carregar()
        {
            Contas.Clear();

            foreach (var c in _service.ListarPorProcesso(_processoId))
                Contas.Add(c);
        }

        [RelayCommand]
        private void AdicionarConta()
        {
            NovaConta.ProcessoId = _processoId;

            _service.SalvarConta(NovaConta);

            NovaConta = new Conta();

            Carregar();
        }
    }
}
