using SistemaJuridico.Infrastructure;
using SistemaJuridico.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace SistemaJuridico.ViewModels
{
    public class PrestacaoContasViewModel : ViewModelBase
    {
        private readonly string _usuarioAtual;
        private readonly string _processoId;
        private readonly List<string> _eventosRascunho = new();

        private ContaRegistro _contaSelecionada = new();
        private AlvaraRegistro _alvaraEmEdicao = new();
        private TratamentoRegistro _tratamentoEmEdicao = new();
        private bool _podeSalvar;
        private bool _podeEditar;
        private bool _podeExcluir;

        public PrestacaoContasViewModel() : this(string.Empty, App.Session.UsuarioAtual?.Nome ?? "Sistema")
        {
        }

        public PrestacaoContasViewModel(string processoId, string usuarioAtual)
        {
            _processoId = processoId;
            _usuarioAtual = string.IsNullOrWhiteSpace(usuarioAtual) ? "Sistema" : usuarioAtual;

            Contas = new ObservableCollection<ContaRegistro>();
            Alvaras = new ObservableCollection<AlvaraRegistro>();
            Tratamentos = new ObservableCollection<TratamentoRegistro>();
            Historico = new ObservableCollection<HistoricoConta>();

            TiposMovimento = new ObservableCollection<string> { "Receita", "Despesa", "Transferência", "Reembolso" };

            AdicionarContaCommand = new RelayCommand(AdicionarConta, () => PodeSalvar, nameof(AdicionarContaCommand));
            RemoverContaCommand = new RelayCommand(RemoverConta, () => PodeExcluir, nameof(RemoverContaCommand));
            SalvarContaCommand = new RelayCommand(SalvarConta, () => PodeSalvar, nameof(SalvarContaCommand));
            AdicionarTratamentoCommand = new RelayCommand(AdicionarTratamento, PodeAdicionarTratamento, nameof(AdicionarTratamentoCommand));
            RemoverTratamentoCommand = new RelayCommand<TratamentoRegistro>(RemoverTratamento, t => t != null, nameof(RemoverTratamentoCommand));
            AdicionarAlvaraCommand = new RelayCommand(AdicionarAlvara, PodeAdicionarAlvara, nameof(AdicionarAlvaraCommand));
            SalvarRascunhoCommand = new RelayCommand(SalvarRascunho, () => _eventosRascunho.Any(), nameof(SalvarRascunhoCommand));
            GerarHistoricoCommand = new RelayCommand(GerarHistorico, null, nameof(GerarHistoricoCommand));

            HistoricoView = CollectionViewSource.GetDefaultView(Historico);
            HistoricoView.SortDescriptions.Add(new SortDescription(nameof(HistoricoConta.DataEvento), ListSortDirection.Descending));

            ContaSelecionada = NovaConta();
            AlvaraEmEdicao = new AlvaraRegistro();
            TratamentoEmEdicao = new TratamentoRegistro();

            HookContaEvents();
            AtualizarEstadoBotoes();
        }

        public ObservableCollection<ContaRegistro> Contas { get; }
        public ObservableCollection<TratamentoRegistro> Tratamentos { get; }
        public ObservableCollection<AlvaraRegistro> Alvaras { get; }
        public ObservableCollection<HistoricoConta> Historico { get; }
        public ObservableCollection<string> TiposMovimento { get; }
        public ICollectionView HistoricoView { get; }

        public ContaRegistro ContaSelecionada
        {
            get => _contaSelecionada;
            set
            {
                var conta = value ?? NovaConta();

                if (_contaSelecionada != null)
                    _contaSelecionada.PropertyChanged -= ContaSelecionada_PropertyChanged;

                if (!SetProperty(ref _contaSelecionada, conta))
                    return;

                HookContaEvents();
                AtualizarEstadoBotoes();
            }
        }

        public AlvaraRegistro AlvaraEmEdicao
        {
            get => _alvaraEmEdicao;
            set
            {
                var alvara = value ?? new AlvaraRegistro();

                if (_alvaraEmEdicao != null)
                    _alvaraEmEdicao.PropertyChanged -= AlvaraEmEdicao_PropertyChanged;

                if (!SetProperty(ref _alvaraEmEdicao, alvara))
                    return;

                if (_alvaraEmEdicao != null)
                    _alvaraEmEdicao.PropertyChanged += AlvaraEmEdicao_PropertyChanged;
            }
        }

        public TratamentoRegistro TratamentoEmEdicao
        {
            get => _tratamentoEmEdicao;
            set => SetProperty(ref _tratamentoEmEdicao, value);
        }

        public bool PodeSalvar { get => _podeSalvar; private set => SetProperty(ref _podeSalvar, value); }
        public bool PodeEditar { get => _podeEditar; private set => SetProperty(ref _podeEditar, value); }
        public bool PodeExcluir { get => _podeExcluir; private set => SetProperty(ref _podeExcluir, value); }

        public ICommand AdicionarContaCommand { get; }
        public ICommand RemoverContaCommand { get; }
        public ICommand SalvarContaCommand { get; }
        public ICommand AdicionarTratamentoCommand { get; }
        public ICommand RemoverTratamentoCommand { get; }
        public ICommand AdicionarAlvaraCommand { get; }
        public ICommand SalvarRascunhoCommand { get; }
        public ICommand GerarHistoricoCommand { get; }

        private void AdicionarConta()
        {
            var nova = ClonarConta(ContaSelecionada);
            nova.Id = Guid.NewGuid().ToString();
            nova.Status = "Criada";
            Contas.Add(nova);
            ContaSelecionada = nova;

            RegistrarHistorico("Criação de conta", $"Conta '{nova.Descricao}' criada no processo {_processoId} com valor {nova.Valor:C2}.");
            AtualizarEstadoBotoes();
        }

        private void SalvarConta()
        {
            if (!ContaValida(ContaSelecionada))
                return;

            ContaSelecionada.Status = "Salva";
            RegistrarHistorico("Alteração de valor", $"Conta '{ContaSelecionada.Descricao}' salva/atualizada com valor {ContaSelecionada.Valor:C2}.");
            AtualizarEstadoBotoes();
        }

        private void RemoverConta()
        {
            if (ContaSelecionada == null)
                return;

            var descricao = ContaSelecionada.Descricao;
            Contas.Remove(ContaSelecionada);
            ContaSelecionada = NovaConta();

            RegistrarHistorico("Remoção de conta", $"Conta '{descricao}' removida.");
            AtualizarEstadoBotoes();
        }

        private void AdicionarTratamento()
        {
            var novo = new TratamentoRegistro
            {
                DescricaoTratamento = TratamentoEmEdicao.DescricaoTratamento,
                Quantidade = TratamentoEmEdicao.Quantidade,
                ValorUnitario = TratamentoEmEdicao.ValorUnitario,
                DataInicio = TratamentoEmEdicao.DataInicio,
                DataFim = TratamentoEmEdicao.DataFim
            };

            Tratamentos.Add(novo);
            RegistrarHistorico("Inclusão de tratamento", $"Tratamento '{novo.DescricaoTratamento}' incluído com total de {novo.ValorTotal:C2}.");
            TratamentoEmEdicao = new TratamentoRegistro();
        }

        private void RemoverTratamento(TratamentoRegistro? tratamento)
        {
            if (tratamento == null)
                return;

            Tratamentos.Remove(tratamento);
            RegistrarHistorico("Remoção de tratamento", $"Tratamento '{tratamento.DescricaoTratamento}' removido.");
        }

        private void AdicionarAlvara()
        {
            var novo = new AlvaraRegistro
            {
                NumeroAlvara = AlvaraEmEdicao.NumeroAlvara,
                DataExpedicao = AlvaraEmEdicao.DataExpedicao,
                ValorAutorizado = AlvaraEmEdicao.ValorAutorizado,
                ValorRecebido = AlvaraEmEdicao.ValorRecebido,
                Observacoes = AlvaraEmEdicao.Observacoes
            };

            Alvaras.Add(novo);
            RegistrarHistorico("Inclusão de alvará", $"Alvará '{novo.NumeroAlvara}' incluído com saldo de {novo.SaldoDisponivel:C2}.");
            RegistrarHistorico("Alteração de saldo", $"Saldo disponível atualizado para {novo.SaldoDisponivel:C2}.");
            AlvaraEmEdicao = new AlvaraRegistro();
        }

        private void SalvarRascunho()
        {
            foreach (var evento in _eventosRascunho)
                RegistrarHistorico("Salvamento de rascunho", evento);

            _eventosRascunho.Clear();
            RaiseCommands();
        }

        private void GerarHistorico()
        {
            RegistrarHistorico("Atualização manual", "Histórico regenerado manualmente.");
        }

        private bool PodeAdicionarTratamento()
            => !string.IsNullOrWhiteSpace(TratamentoEmEdicao.DescricaoTratamento)
               && TratamentoEmEdicao.Quantidade >= 0
               && TratamentoEmEdicao.ValorUnitario >= 0
               && (!TratamentoEmEdicao.DataInicio.HasValue || !TratamentoEmEdicao.DataFim.HasValue || TratamentoEmEdicao.DataFim >= TratamentoEmEdicao.DataInicio);

        private bool PodeAdicionarAlvara()
            => !string.IsNullOrWhiteSpace(AlvaraEmEdicao.NumeroAlvara)
               && AlvaraEmEdicao.ValorAutorizado >= 0
               && AlvaraEmEdicao.ValorRecebido >= 0;

        private bool ContaValida(ContaRegistro conta)
            => conta != null
               && !string.IsNullOrWhiteSpace(conta.Descricao)
               && conta.Valor > 0
               && conta.DataMovimento.HasValue;

        private void RegistrarHistorico(string evento, string detalhes)
        {
            Historico.Add(new HistoricoConta
            {
                DataEvento = DateTime.Now,
                DescricaoEvento = evento,
                Usuario = _usuarioAtual,
                Detalhes = detalhes
            });
            HistoricoView.Refresh();
        }

        private void RegistrarEventoRascunho(string detalhe)
        {
            _eventosRascunho.Add(detalhe);
            RaiseCommands();
        }

        private void AtualizarEstadoBotoes()
        {
            PodeSalvar = ContaValida(ContaSelecionada);
            PodeEditar = Contas.Contains(ContaSelecionada);
            PodeExcluir = PodeEditar;
            RaiseCommands();
        }

        private void RaiseCommands()
        {
            (AdicionarContaCommand as RelayCommand)?.Raise();
            (RemoverContaCommand as RelayCommand)?.Raise();
            (SalvarContaCommand as RelayCommand)?.Raise();
            (AdicionarTratamentoCommand as RelayCommand)?.Raise();
            (AdicionarAlvaraCommand as RelayCommand)?.Raise();
            (SalvarRascunhoCommand as RelayCommand)?.Raise();
        }

        private void HookContaEvents()
        {
            if (ContaSelecionada != null)
                ContaSelecionada.PropertyChanged += ContaSelecionada_PropertyChanged;
        }

        private void AlvaraEmEdicao_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not AlvaraRegistro alvara)
                return;

            if (e.PropertyName == nameof(AlvaraRegistro.SaldoDisponivel))
                RegistrarEventoRascunho($"Saldo de alvará atualizado para {alvara.SaldoDisponivel:C2}.");
        }

        private void ContaSelecionada_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not ContaRegistro conta)
                return;

            if (e.PropertyName == nameof(ContaRegistro.Valor) ||
                e.PropertyName == nameof(ContaRegistro.Descricao) ||
                e.PropertyName == nameof(ContaRegistro.DataMovimento))
            {
                RegistrarEventoRascunho($"Alteração pendente em conta '{conta.Descricao}' ({e.PropertyName}).");
                AtualizarEstadoBotoes();
            }
        }

        private static ContaRegistro NovaConta() => new()
        {
            DataMovimento = DateTime.Today,
            TipoMovimento = "Receita",
            Status = "Rascunho"
        };

        private static ContaRegistro ClonarConta(ContaRegistro origem) => new()
        {
            Id = origem.Id,
            Descricao = origem.Descricao,
            Valor = origem.Valor,
            DataMovimento = origem.DataMovimento,
            TipoMovimento = origem.TipoMovimento,
            Observacoes = origem.Observacoes,
            Status = origem.Status,
            Responsavel = origem.Responsavel
        };
    }
}
