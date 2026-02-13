using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaJuridico.Models;
using SistemaJuridico.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace SistemaJuridico.ViewModels
{
    public partial class ContasViewModel : ObservableObject
    {
        private readonly ContaService _service;
        private readonly VerificacaoService _verificacaoService;
        private readonly AuditService _auditService;
        private readonly string _processoId;
        private readonly AppStateViewModel _appState;

        public ObservableCollection<Conta> Contas => _appState.ContasProcesso;
        public ObservableCollection<Conta> ContasRascunho => _appState.ContasRascunho;
        public ObservableCollection<string> TerapiasMedicamentosDisponiveis { get; } = new();
        public ObservableCollection<ContaHistoricoLinha> HistoricoContas { get; } = new();

        [ObservableProperty]
        private Conta? _contaSelecionada;

        [ObservableProperty]
        private Conta _edicaoConta = new();

        [ObservableProperty]
        private string _terapiaManual = string.Empty;

        [ObservableProperty]
        private string _tipoLancamentoSelecionado = "Alvará";

        public bool PodeCadastrar => _appState.PodeCadastrarContas;
        public bool PodeEditar => _appState.PodeEditarContas;
        public bool PodeExcluir => _appState.PodeExcluirContas;
        public bool IsAlvara => string.Equals(EdicaoConta.TipoLancamento, "Alvará", StringComparison.OrdinalIgnoreCase);
        public bool IsTratamento => string.Equals(EdicaoConta.TipoLancamento, "Tratamento", StringComparison.OrdinalIgnoreCase);
        public bool IsDespesaGeral => string.Equals(EdicaoConta.TipoLancamento, "Despesa Geral", StringComparison.OrdinalIgnoreCase);
        public bool IsValorContaHabilitado => !IsAlvara;
        public bool ExibirCampoTerapiaManual => IsTratamento && string.Equals(EdicaoConta.TerapiaMedicamentoNome, "OUTRO", StringComparison.OrdinalIgnoreCase);

        public ContasViewModel() : this(string.Empty)
        {
        }

        public ContasViewModel(string processoId)
        {
            _processoId = processoId;
            _appState = AppStateViewModel.Instance;
            _appState.DefinirContexto(App.Session.UsuarioAtual, _appState.ProcessoSelecionado);

            var db = new DatabaseService();
            _service = new ContaService(db);
            _verificacaoService = new VerificacaoService(db);
            _auditService = new AuditService(db);

            Carregar();
            NovaConta();
        }

        partial void OnEdicaoContaChanged(Conta value)
        {
            OnPropertyChanged(nameof(IsAlvara));
            OnPropertyChanged(nameof(IsTratamento));
            OnPropertyChanged(nameof(IsDespesaGeral));
            OnPropertyChanged(nameof(IsValorContaHabilitado));
            OnPropertyChanged(nameof(ExibirCampoTerapiaManual));
        }

        partial void OnTipoLancamentoSelecionadoChanged(string value)
        {
            EdicaoConta.TipoLancamento = value;
            AplicarRegraTipoLancamento(EdicaoConta);
            OnPropertyChanged(nameof(IsAlvara));
            OnPropertyChanged(nameof(IsTratamento));
            OnPropertyChanged(nameof(IsDespesaGeral));
            OnPropertyChanged(nameof(IsValorContaHabilitado));
            OnPropertyChanged(nameof(ExibirCampoTerapiaManual));
            OnPropertyChanged(nameof(ValorAlvaraTexto));
            OnPropertyChanged(nameof(ValorContaTexto));
        }

        [RelayCommand]
        private void Carregar()
        {
            Contas.Clear();
            HistoricoContas.Clear();

            if (string.IsNullOrWhiteSpace(_processoId))
                return;

            var contasOrdenadas = _service.ListarPorProcesso(_processoId)
                .OrderBy(c => ParseData(c.DataMovimentacao) ?? DateTime.MinValue)
                .ToList();

            foreach (var c in contasOrdenadas)
                Contas.Add(c);

            _appState.AtualizarContas(contasOrdenadas);

            var verificacoes = _verificacaoService.ListarPorProcesso(_processoId);
            _appState.AtualizarVerificacoes(verificacoes);

            TerapiasMedicamentosDisponiveis.Clear();
            foreach (var item in _appState.EstadoAtual.TerapiasEMedicamentos)
                TerapiasMedicamentosDisponiveis.Add(item);
            TerapiasMedicamentosDisponiveis.Add("OUTRO");

            decimal saldo = 0m;
            foreach (var conta in contasOrdenadas)
            {
                saldo += conta.ValorAlvara - conta.ValorConta;
                HistoricoContas.Add(new ContaHistoricoLinha(conta, saldo));
            }
        }

        [RelayCommand]
        private void NovaConta()
        {
            EdicaoConta = new Conta
            {
                ProcessoId = _processoId,
                Responsavel = App.Session.UsuarioAtual?.Nome ?? "Sistema",
                TipoLancamento = "Alvará"
            };
            TipoLancamentoSelecionado = "Alvará";
            TerapiaManual = string.Empty;
        }

        [RelayCommand]
        private void AdicionarRascunho()
        {
            if (!PodeCadastrar)
            {
                MessageBox.Show("Seu perfil não possui permissão para cadastrar contas.");
                return;
            }

            if (!ValidarConta(EdicaoConta))
                return;

            AplicarRegraTipoLancamento(EdicaoConta);
            if (ExibirCampoTerapiaManual)
                EdicaoConta.TerapiaMedicamentoNome = TerapiaManual.Trim();

            if (!string.IsNullOrWhiteSpace(EdicaoConta.Id) && Contas.Any(x => x.Id == EdicaoConta.Id))
                EdicaoConta.Id = Guid.NewGuid().ToString();

            ContasRascunho.Add(CloneConta(EdicaoConta));
            NovaConta();
        }

        [RelayCommand]
        private void RemoverRascunho(Conta? conta)
        {
            if (conta is null)
                return;

            ContasRascunho.Remove(conta);
        }

        [RelayCommand]
        private void LimparRascunhos()
        {
            ContasRascunho.Clear();
        }

        [RelayCommand]
        private void ConfirmarRascunhos()
        {
            if (!PodeCadastrar)
            {
                MessageBox.Show("Seu perfil não possui permissão para cadastrar contas.");
                return;
            }

            if (ContasRascunho.Count == 0)
            {
                MessageBox.Show("Não há lançamentos em rascunho para confirmar.");
                return;
            }

            foreach (var conta in ContasRascunho)
            {
                conta.ProcessoId = _processoId;
                _service.Inserir(conta);
                _auditService.Registrar(
                    "Conta.Criada",
                    "processo",
                    _processoId,
                    $"Nova conta {conta.TipoLancamento} em {conta.DataMovimentacao}: +{conta.ValorAlvara} -{conta.ValorConta}");
            }

            ContasRascunho.Clear();
            Carregar();
        }

        [RelayCommand]
        private void EditarConta()
        {
            if (ContaSelecionada == null)
                return;

            if (!PodeEditar)
            {
                MessageBox.Show("Seu perfil não possui permissão para editar contas.");
                return;
            }

            if (!ContaSelecionada.PodeEditar)
            {
                MessageBox.Show("Conta já fechada.");
                return;
            }

            EdicaoConta = CloneConta(ContaSelecionada);
            TipoLancamentoSelecionado = EdicaoConta.TipoLancamento;
        }

        [RelayCommand]
        private void SalvarEdicaoConta()
        {
            if (ContaSelecionada == null)
                return;

            if (!PodeEditar)
            {
                MessageBox.Show("Seu perfil não possui permissão para editar contas.");
                return;
            }

            if (!ValidarConta(EdicaoConta))
                return;

            AplicarRegraTipoLancamento(EdicaoConta);
            if (ExibirCampoTerapiaManual)
                EdicaoConta.TerapiaMedicamentoNome = TerapiaManual.Trim();

            _service.Atualizar(EdicaoConta);
            _auditService.Registrar(
                "Conta.Editada",
                "processo",
                _processoId,
                $"Conta {EdicaoConta.Id} atualizada");

            Carregar();
            NovaConta();
        }

        [RelayCommand]
        private void ExcluirConta()
        {
            if (ContaSelecionada == null)
                return;

            if (!PodeExcluir)
            {
                MessageBox.Show("Somente administradores podem excluir contas.");
                return;
            }

            if (MessageBox.Show("Excluir conta?", "Confirma", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _service.Excluir(ContaSelecionada.Id);
                _auditService.Registrar(
                    "Conta.Excluida",
                    "processo",
                    _processoId,
                    $"Conta {ContaSelecionada.Id} removida");
                Carregar();
            }
        }

        [RelayCommand]
        private void FecharConta()
        {
            if (ContaSelecionada == null)
                return;

            _service.FecharConta(ContaSelecionada.Id);
            _auditService.Registrar("Conta.Fechada", "processo", _processoId, $"Conta {ContaSelecionada.Id} fechada");
            Carregar();
        }

        private bool ValidarConta(Conta conta)
        {
            if (string.IsNullOrWhiteSpace(conta.TipoLancamento))
            {
                MessageBox.Show("Tipo de lançamento obrigatório.");
                return false;
            }

            if (!DateTime.TryParse(conta.DataMovimentacao, out _))
            {
                MessageBox.Show("Data da movimentação inválida.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(conta.Historico))
            {
                MessageBox.Show("Histórico obrigatório.");
                return false;
            }

            if (IsAlvara)
            {
                if (string.IsNullOrWhiteSpace(conta.MovProcesso))
                {
                    MessageBox.Show("Movimento processual é obrigatório para Alvará.");
                    return false;
                }

                if (conta.ValorAlvara <= 0)
                {
                    MessageBox.Show("Valor do Alvará deve ser maior que zero.");
                    return false;
                }
            }
            else
            {
                if (conta.ValorConta <= 0)
                {
                    MessageBox.Show("Valor da Conta deve ser maior que zero.");
                    return false;
                }
            }

            if (ExibirCampoTerapiaManual && string.IsNullOrWhiteSpace(TerapiaManual))
            {
                MessageBox.Show("Informe a terapia/medicamento manual.");
                return false;
            }

            return true;
        }

        private void AplicarRegraTipoLancamento(Conta conta)
        {
            if (string.Equals(conta.TipoLancamento, "Alvará", StringComparison.OrdinalIgnoreCase))
            {
                conta.ValorConta = 0m;
                conta.TerapiaMedicamentoNome = null;
                conta.Quantidade = null;
                conta.MesReferencia = null;
                conta.AnoReferencia = null;
            }
            else if (string.Equals(conta.TipoLancamento, "Tratamento", StringComparison.OrdinalIgnoreCase))
            {
                conta.ValorAlvara = 0m;
            }
            else if (string.Equals(conta.TipoLancamento, "Despesa Geral", StringComparison.OrdinalIgnoreCase))
            {
                conta.ValorAlvara = 0m;
                conta.TerapiaMedicamentoNome = null;
            }
        }

        public void AtualizarValorAlvaraTexto(string texto)
        {
            EdicaoConta.ValorAlvara = ParseMoeda(texto);
            OnPropertyChanged(nameof(ValorAlvaraTexto));
        }

        public void AtualizarValorContaTexto(string texto)
        {
            EdicaoConta.ValorConta = ParseMoeda(texto);
            OnPropertyChanged(nameof(ValorContaTexto));
        }

        public string ValorAlvaraTexto => FormatarMoeda(EdicaoConta.ValorAlvara);
        public string ValorContaTexto => FormatarMoeda(EdicaoConta.ValorConta);

        private static decimal ParseMoeda(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return 0m;

            var limpo = new string(texto.Where(c => char.IsDigit(c) || c == ',' || c == '.').ToArray());
            if (decimal.TryParse(limpo, NumberStyles.Any, CultureInfo.GetCultureInfo("pt-BR"), out var valor))
                return valor;

            return 0m;
        }

        private static string FormatarMoeda(decimal valor)
        {
            if (valor <= 0m)
                return string.Empty;

            return valor.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
        }

        private static DateTime? ParseData(string? valor)
        {
            if (DateTime.TryParse(valor, out var parsed))
                return parsed;

            return null;
        }

        private static Conta CloneConta(Conta origem)
        {
            return new Conta
            {
                Id = origem.Id,
                ProcessoId = origem.ProcessoId,
                TipoLancamento = origem.TipoLancamento,
                Historico = origem.Historico,
                DataMovimentacao = origem.DataMovimentacao,
                MovProcesso = origem.MovProcesso,
                NumNfAlvara = origem.NumNfAlvara,
                ValorAlvara = origem.ValorAlvara,
                ValorConta = origem.ValorConta,
                TerapiaMedicamentoNome = origem.TerapiaMedicamentoNome,
                Quantidade = origem.Quantidade,
                MesReferencia = origem.MesReferencia,
                AnoReferencia = origem.AnoReferencia,
                StatusConta = origem.StatusConta,
                Responsavel = origem.Responsavel,
                Observacoes = origem.Observacoes
            };
        }
    }

    public class ContaHistoricoLinha
    {
        public ContaHistoricoLinha(Conta conta, decimal saldoParcial)
        {
            Conta = conta;
            SaldoParcial = saldoParcial;
        }

        public Conta Conta { get; }
        public decimal SaldoParcial { get; }
    }
}
