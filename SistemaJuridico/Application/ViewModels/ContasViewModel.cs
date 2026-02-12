using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaJuridico.Models;
using SistemaJuridico.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace SistemaJuridico.ViewModels
{
    public partial class ContasViewModel : ObservableObject
    {
        private readonly ContaService _service;

        private readonly string _processoId;

        public ObservableCollection<Conta> Contas { get; } = new();

        [ObservableProperty]
        private Conta? _contaSelecionada;

        [ObservableProperty]
        private Conta _edicaoConta = new();

        public ContasViewModel() : this(string.Empty)
        {
        }

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

            if (string.IsNullOrWhiteSpace(_processoId))
                return;

            foreach (var c in _service.ListarPorProcesso(_processoId))
                Contas.Add(c);
        }

        [RelayCommand]
        private void NovaConta()
        {
            if (string.IsNullOrWhiteSpace(_processoId))
                return;

            EdicaoConta = new Conta
            {
                ProcessoId = _processoId
            };
        }

        [RelayCommand]
        private void SalvarConta()
        {
            if (string.IsNullOrWhiteSpace(EdicaoConta.Tipo))
            {
                MessageBox.Show("Tipo obrigatório");
                return;
            }

            if (!Contas.Any(x => x.Id == EdicaoConta.Id))
                _service.Inserir(EdicaoConta);
            else
                _service.Atualizar(EdicaoConta);

            NovaConta();
            Carregar();
        }

        [RelayCommand]
        private void EditarConta()
        {
            if (ContaSelecionada == null) return;

            if (!ContaSelecionada.PodeEditar)
            {
                MessageBox.Show("Conta já fechada.");
                return;
            }

            EdicaoConta = new Conta
            {
                Id = ContaSelecionada.Id,
                ProcessoId = ContaSelecionada.ProcessoId,
                Tipo = ContaSelecionada.Tipo,
                ValorAlvara = ContaSelecionada.ValorAlvara,
                ValorConta = ContaSelecionada.ValorConta,
                Observacao = ContaSelecionada.Observacao,
                StatusConta = ContaSelecionada.StatusConta
            };
        }

        [RelayCommand]
        private void ExcluirConta()
        {
            if (ContaSelecionada == null) return;

            if (MessageBox.Show("Excluir conta?",
                "Confirma",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _service.Excluir(ContaSelecionada.Id);
                Carregar();
            }
        }

        [RelayCommand]
        private void FecharConta()
        {
            if (ContaSelecionada == null) return;

            _service.FecharConta(ContaSelecionada.Id);
            Carregar();
        }
    }
}
