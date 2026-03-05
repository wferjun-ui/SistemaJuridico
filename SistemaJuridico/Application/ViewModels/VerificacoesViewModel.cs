using SistemaJuridico.Infrastructure;
using SistemaJuridico.Presentation.Models;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace SistemaJuridico.ViewModels
{
    public class VerificacoesViewModel : ViewModelBase
    {
        private int _proximoId = 1;
        private Verificacao? _verificacaoSelecionada;
        private bool _podeSalvar;
        private bool _podeExcluir;

        public ObservableCollection<Verificacao> Verificacoes { get; } = new();
        public ObservableCollection<string> TiposVerificacao { get; } = new()
        {
            "Documental",
            "Médica",
            "Financeira",
            "Administrativa"
        };

        public ObservableCollection<string> StatusDisponiveis { get; } = new()
        {
            "Pendente",
            "Em andamento",
            "Concluído"
        };

        public Verificacao? VerificacaoSelecionada
        {
            get => _verificacaoSelecionada;
            set
            {
                if (SetProperty(ref _verificacaoSelecionada, value))
                {
                    PodeExcluir = value != null;
                    AtualizarPodeSalvar();
                    RaiseCommands();
                }
            }
        }

        public bool PodeSalvar
        {
            get => _podeSalvar;
            private set => SetProperty(ref _podeSalvar, value);
        }

        public bool PodeExcluir
        {
            get => _podeExcluir;
            private set => SetProperty(ref _podeExcluir, value);
        }

        public bool PodeAdicionar => true;

        public ICommand AdicionarVerificacaoCommand { get; }
        public ICommand RemoveVerificacaoCommand { get; }
        public ICommand SalvarVerificacaoCommand { get; }
        public ICommand LimparFormularioCommand { get; }
        public ICommand CarregarVerificacoesCommand { get; }
        public ICommand IncrementarQuantidadeTratamentosCommand { get; }
        public ICommand DecrementarQuantidadeTratamentosCommand { get; }

        public VerificacoesViewModel()
        {
            Verificacoes.CollectionChanged += OnVerificacoesChanged;

            AdicionarVerificacaoCommand = new RelayCommand(AdicionarVerificacao, () => PodeAdicionar, nameof(AdicionarVerificacao));
            RemoveVerificacaoCommand = new RelayCommand(RemoverVerificacao, () => PodeExcluir, nameof(RemoverVerificacao));
            SalvarVerificacaoCommand = new RelayCommand(SalvarVerificacao, () => PodeSalvar, nameof(SalvarVerificacao));
            LimparFormularioCommand = new RelayCommand(LimparFormulario, () => VerificacaoSelecionada != null, nameof(LimparFormulario));
            CarregarVerificacoesCommand = new RelayCommand(CarregarVerificacoes, name: nameof(CarregarVerificacoes));
            IncrementarQuantidadeTratamentosCommand = new RelayCommand(IncrementarQuantidadeTratamentos, () => VerificacaoSelecionada != null, nameof(IncrementarQuantidadeTratamentos));
            DecrementarQuantidadeTratamentosCommand = new RelayCommand(DecrementarQuantidadeTratamentos, () => VerificacaoSelecionada != null && VerificacaoSelecionada.QuantidadeTratamentos > 0, nameof(DecrementarQuantidadeTratamentos));

            CarregarVerificacoes();
        }

        private void OnVerificacoesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<Verificacao>())
                    item.PropertyChanged += OnVerificacaoPropertyChanged;
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<Verificacao>())
                    item.PropertyChanged -= OnVerificacaoPropertyChanged;
            }

            RaiseCommands();
        }

        private void OnVerificacaoPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender == VerificacaoSelecionada)
                AtualizarPodeSalvar();

            if (e.PropertyName == nameof(Verificacao.QuantidadeTratamentos))
                RaiseCommands();
        }

        private void AdicionarVerificacao()
        {
            var novaVerificacao = new Verificacao
            {
                Id = _proximoId++,
                Status = "Pendente",
                DataSolicitacao = DateTime.Today,
                DataResposta = null,
                QuantidadeTratamentos = 0
            };

            Verificacoes.Add(novaVerificacao);
            VerificacaoSelecionada = novaVerificacao;
        }

        private void RemoverVerificacao()
        {
            if (VerificacaoSelecionada == null)
                return;

            var indiceAtual = Verificacoes.IndexOf(VerificacaoSelecionada);
            var confirmacao = MessageBox.Show(
                "Deseja realmente remover a verificação selecionada?",
                "Confirmar exclusão",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmacao != MessageBoxResult.Yes)
                return;

            Verificacoes.Remove(VerificacaoSelecionada);

            if (Verificacoes.Count == 0)
            {
                VerificacaoSelecionada = null;
                return;
            }

            var novoIndice = Math.Min(indiceAtual, Verificacoes.Count - 1);
            VerificacaoSelecionada = Verificacoes[novoIndice];
        }

        private void SalvarVerificacao()
        {
            if (VerificacaoSelecionada == null)
                return;

            if (!VerificacaoSelecionada.IsValid())
            {
                MessageBox.Show(
                    "Preencha os campos obrigatórios e corrija os dados inválidos antes de salvar.",
                    "Validação",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                AtualizarPodeSalvar();
                return;
            }

            MessageBox.Show(
                "Verificação salva com sucesso.",
                "Salvar",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            AtualizarPodeSalvar();
        }

        private void LimparFormulario()
        {
            if (VerificacaoSelecionada == null)
                return;

            VerificacaoSelecionada.TipoVerificacao = string.Empty;
            VerificacaoSelecionada.DataSolicitacao = null;
            VerificacaoSelecionada.DataResposta = null;
            VerificacaoSelecionada.Status = "Pendente";
            VerificacaoSelecionada.Responsavel = string.Empty;
            VerificacaoSelecionada.QuantidadeTratamentos = 0;
            VerificacaoSelecionada.Resultado = string.Empty;
            VerificacaoSelecionada.Observacoes = string.Empty;

            AtualizarPodeSalvar();
        }

        private void CarregarVerificacoes()
        {
            Verificacoes.Clear();

            var exemplo = new Verificacao
            {
                Id = _proximoId++,
                TipoVerificacao = "Documental",
                DataSolicitacao = DateTime.Today,
                Status = "Pendente",
                Responsavel = "",
                QuantidadeTratamentos = 0
            };

            Verificacoes.Add(exemplo);
            VerificacaoSelecionada = exemplo;
            AtualizarPodeSalvar();
        }

        private void IncrementarQuantidadeTratamentos()
        {
            if (VerificacaoSelecionada == null)
                return;

            VerificacaoSelecionada.QuantidadeTratamentos++;
            AtualizarPodeSalvar();
        }

        private void DecrementarQuantidadeTratamentos()
        {
            if (VerificacaoSelecionada == null || VerificacaoSelecionada.QuantidadeTratamentos <= 0)
                return;

            VerificacaoSelecionada.QuantidadeTratamentos--;
            AtualizarPodeSalvar();
        }

        private void AtualizarPodeSalvar()
        {
            PodeSalvar = VerificacaoSelecionada != null && VerificacaoSelecionada.IsValid();
            RaiseCommands();
        }

        private void RaiseCommands()
        {
            (AdicionarVerificacaoCommand as RelayCommand)?.Raise();
            (RemoveVerificacaoCommand as RelayCommand)?.Raise();
            (SalvarVerificacaoCommand as RelayCommand)?.Raise();
            (LimparFormularioCommand as RelayCommand)?.Raise();
            (IncrementarQuantidadeTratamentosCommand as RelayCommand)?.Raise();
            (DecrementarQuantidadeTratamentosCommand as RelayCommand)?.Raise();
        }
    }
}
