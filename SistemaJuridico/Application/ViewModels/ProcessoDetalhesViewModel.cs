using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaJuridico.Infrastructure;
using SistemaJuridico.Models;
using SistemaJuridico.Services;
using SistemaJuridico.Views;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SistemaJuridico.ViewModels
{
    public partial class ProcessoDetalhesViewModel : ObservableObject
    {
        private readonly ProcessService _processService;
        private readonly ContaService _contaService;
        private readonly ItemSaudeService _itemSaudeService;
        private readonly VerificacaoService _verificacaoService;
        private readonly HistoricoService _historicoService;
        private readonly LoggerService _logger = new();
        private readonly string _processoId;

        private System.Windows.Threading.DispatcherTimer? _lockTimer;
        private bool _lockAdquirido;
        private DateTime? _ultimaVerificacaoDate;
        private string _ultimaVerificacaoData = "Sem verificação";
        private string _ultimaVerificacaoResponsavel = "N/D";

        public Processo Processo { get; private set; }

        public ObservableCollection<Conta> Contas { get; } = new();
        public ObservableCollection<ItemSaude> ItensSaude { get; } = new();
        public ObservableCollection<Verificacao> Verificacoes { get; } = new();
        public ObservableCollection<Diligencia> Diligencias { get; } = new();
        public ObservableCollection<Historico> Historicos { get; } = new();

        [ObservableProperty]
        private bool _modoSomenteLeitura;

        [ObservableProperty]
        private string _usuarioEditandoTexto = "";

        public ProcessoDetalhesViewModel(string processoId, ProcessService processService)
        {
            _processService = processService;

            var db = new DatabaseService();
            _contaService = new ContaService(db);
            _itemSaudeService = new ItemSaudeService(db);
            _verificacaoService = new VerificacaoService(db);
            _historicoService = new HistoricoService(db);

            _processoId = processoId;

            Processo = _processService
                .ListarProcessos()
                .FirstOrDefault(x => x.Id == processoId)
                ?? new Processo { Id = processoId };

            ValidarLock();
            RecarregarTudo();
        }

        public decimal TotalContasAPrestar => Contas
            .Where(c => !string.Equals(c.StatusConta, "fechada", StringComparison.OrdinalIgnoreCase))
            .Sum(c => c.ValorConta);

        public string UltimaVerificacaoData => _ultimaVerificacaoData;

        public string UltimaVerificacaoResponsavel => _ultimaVerificacaoResponsavel;

        public string HashProcesso
        {
            get
            {
                var dados = $"{Processo.Id}|{Processo.Numero}|{Processo.Paciente}|{Processo.Juiz}|{Processo.StatusFase}";
                var hash = SHA256.HashData(Encoding.UTF8.GetBytes(dados));
                return Convert.ToHexString(hash)[..16];
            }
        }

        public string DataPrescricao => _ultimaVerificacaoDate?.AddDays(90).ToString("dd/MM/yyyy") ?? "Sem base";

        public string PrescricaoStatus
        {
            get
            {
                if (_ultimaVerificacaoDate is null)
                    return "Atualização pendente";

                var dias = (_ultimaVerificacaoDate.Value.AddDays(90).Date - DateTime.Today).Days;

                if (dias < 0)
                    return "Atualização vencida";

                if (dias <= 7)
                    return $"Atualizar em {dias} dia(s)";

                return "Em dia";
            }
        }

        private void RecarregarTudo()
        {
            CarregarContas();
            CarregarItensSaude();
            CarregarVerificacoes();
            CarregarDiligencias();
            CarregarHistorico();
        }

        private void ValidarLock()
        {
            var usuario = _processService.UsuarioEditando(_processoId);
            var atual = App.Session.UsuarioAtual?.Email;

            if (usuario != null && usuario != atual)
            {
                ModoSomenteLeitura = true;
                UsuarioEditandoTexto = $"Processo em edição por {usuario}";
                return;
            }

            var lockObtido = _processService.TentarLock(_processoId);
            if (!lockObtido)
            {
                ModoSomenteLeitura = true;
                UsuarioEditandoTexto = "Processo em edição por outro usuário.";
                return;
            }

            _lockAdquirido = true;
            IniciarHeartbeat();
        }

        private void IniciarHeartbeat()
        {
            _lockTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(5)
            };

            _lockTimer.Tick += (_, _) =>
            {
                try
                {
                    _processService.RenovarLock(_processoId);
                }
                catch (Exception ex)
                {
                    _logger.Error("Falha ao renovar lock do processo", ex);
                    ModoSomenteLeitura = true;
                    UsuarioEditandoTexto = "Não foi possível renovar o lock. Reabra o processo.";
                    _lockTimer?.Stop();
                }
            };

            _lockTimer.Start();
        }

        public void LiberarLock()
        {
            _lockTimer?.Stop();

            if (!_lockAdquirido)
                return;

            try
            {
                _processService.LiberarLock(_processoId);
                _lockAdquirido = false;
            }
            catch (Exception ex)
            {
                _logger.Error("Falha ao liberar lock do processo", ex);
            }
        }

        private void CarregarContas()
        {
            Contas.Clear();

            foreach (var c in _contaService.ListarPorProcesso(_processoId))
                Contas.Add(c);

            OnPropertyChanged(nameof(TotalContasAPrestar));
        }

        private void CarregarItensSaude()
        {
            ItensSaude.Clear();
            foreach (var item in _itemSaudeService.ListarPorProcesso(_processoId))
                ItensSaude.Add(item);
        }

        private void CarregarVerificacoes()
        {
            Verificacoes.Clear();

            var verificacoes = _verificacaoService
                .ListarPorProcesso(_processoId)
                .OrderByDescending(v => ParseData(v.DataHora) ?? DateTime.MinValue)
                .ToList();

            foreach (var v in verificacoes)
                Verificacoes.Add(v);
            }

            OnPropertyChanged(nameof(UltimaVerificacaoData));
            OnPropertyChanged(nameof(UltimaVerificacaoResponsavel));
            OnPropertyChanged(nameof(DataPrescricao));
            OnPropertyChanged(nameof(PrescricaoStatus));
        }

        private static DateTime? ParseData(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return null;

            if (DateTime.TryParse(valor, out var parsed))
                return parsed;

            var formatos = new[] { "dd/MM/yyyy", "dd/MM/yyyy HH:mm", "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss" };
            if (DateTime.TryParseExact(valor, formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exato))
                return exato;

            return null;
        }

        private void CarregarDiligencias()
        {
            Diligencias.Clear();
            var lista = new DiligenciaService(new DatabaseService()).ListarPorProcesso(_processoId);
            foreach (var d in lista)
                Diligencias.Add(d);
        }

        private void CarregarHistorico()
        {
            Historicos.Clear();

            foreach (var historico in _historicoService.ListarPorProcesso(_processoId))
                Historicos.Add(historico);

            OnPropertyChanged(nameof(HashProcesso));
        }

        public decimal TotalContasAPrestar => Contas.Where(c => !string.Equals(c.StatusConta, "fechada", StringComparison.OrdinalIgnoreCase)).Sum(c => c.ValorConta);

        public string UltimaVerificacaoData => Verificacoes.OrderByDescending(v => v.DataHora).FirstOrDefault()?.DataHora ?? "Sem verificação";

        public string UltimaVerificacaoResponsavel => Verificacoes.OrderByDescending(v => v.DataHora).FirstOrDefault()?.Responsavel ?? "N/D";

        public string HashProcesso => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{Processo.Id}|{Processo.Numero}|{Processo.Paciente}|{Processo.Juiz}"))).Substring(0, 16);

        public string DataPrescricao
        {
            get
            {
                var ultima = Verificacoes
                    .Select(v => DateTime.TryParse(v.DataHora, out var dt) ? dt : (DateTime?)null)
                    .Where(dt => dt.HasValue)
                    .Select(dt => dt!.Value)
                    .OrderByDescending(dt => dt)
                    .FirstOrDefault();

                if (ultima == default)
                    return "Sem base";

                return ultima.AddDays(90).ToString("dd/MM/yyyy");
            }
        }

        public string PrescricaoStatus
        {
            get
            {
                var ultima = Verificacoes
                    .Select(v => DateTime.TryParse(v.DataHora, out var dt) ? dt : (DateTime?)null)
                    .Where(dt => dt.HasValue)
                    .Select(dt => dt!.Value)
                    .OrderByDescending(dt => dt)
                    .FirstOrDefault();

                if (ultima == default)
                    return "Atualização pendente";

                var vencimento = ultima.AddDays(90);
                var dias = (vencimento.Date - DateTime.Today).Days;

                if (dias < 0)
                    return "Atualização vencida";

                if (dias <= 7)
                    return $"Atualizar em {dias} dia(s)";

                return "Em dia";
            }
        }
        public Task CarregarAsync(int processoId)
        {
            var processoEncontrado = _processService
                .ListarProcessos()
                .FirstOrDefault(x => x.Id == processoId.ToString());

            if (processoEncontrado != null)
                Processo = processoEncontrado;

            CarregarContas();
            CarregarItensSaude();
            CarregarVerificacoes();
            CarregarDiligencias();
            CarregarHistorico();

            return Task.CompletedTask;
        }

        public Task CarregarAsync(ProcessoCompletoDTO processo)
        {
            Processo = processo.Processo;

            Contas.Clear();
            foreach (var conta in processo.Contas)
                Contas.Add(conta);
            OnPropertyChanged(nameof(TotalContasAPrestar));

            ItensSaude.Clear();
            foreach (var item in processo.ItensSaude)
                ItensSaude.Add(item);

            Verificacoes.Clear();
            var verificacoes = processo.Verificacoes
                .OrderByDescending(v => ParseData(v.DataHora) ?? DateTime.MinValue)
                .ToList();

            foreach (var verificacao in verificacoes)
                Verificacoes.Add(verificacao);

            var ultima = verificacoes.FirstOrDefault();
            _ultimaVerificacaoData = ultima?.DataHora ?? "Sem verificação";
            _ultimaVerificacaoResponsavel = string.IsNullOrWhiteSpace(ultima?.Responsavel) ? "N/D" : ultima.Responsavel;
            _ultimaVerificacaoDate = ParseData(ultima?.DataHora);
            OnPropertyChanged(nameof(UltimaVerificacaoData));
            OnPropertyChanged(nameof(UltimaVerificacaoResponsavel));
            OnPropertyChanged(nameof(DataPrescricao));
            OnPropertyChanged(nameof(PrescricaoStatus));

            Diligencias.Clear();
            foreach (var diligencia in processo.Diligencias)
                Diligencias.Add(diligencia);

            CarregarHistorico();

            return Task.CompletedTask;
        }

        [RelayCommand]
        private void SalvarRascunho()
        {
            var motivo = Microsoft.VisualBasic.Interaction.InputBox("Informe o motivo do rascunho:");
            if (string.IsNullOrWhiteSpace(motivo))
                return;

            _processService.MarcarRascunho(_processoId, motivo);
            System.Windows.MessageBox.Show("Rascunho salvo.");
            CarregarHistorico();
        }

        [RelayCommand]
        private void ConcluirEdicao()
        {
            _processService.MarcarConcluido(_processoId);
            System.Windows.MessageBox.Show("Edição concluída.");
            CarregarHistorico();
        }

        [RelayCommand]
        private void NovaVerificacao()
        {
            if (ModoSomenteLeitura)
                return;

            var facade = new VerificacaoFacadeService();
            var status = Microsoft.VisualBasic.Interaction.InputBox("Informe o status do processo:");
            if (string.IsNullOrWhiteSpace(status))
                return;

            var descricao = Microsoft.VisualBasic.Interaction.InputBox("Descrição da verificação:");
            var responsavel = App.Session.UsuarioAtual?.Nome ?? "Sistema";

            facade.CriarVerificacao(_processoId, status, responsavel, descricao, ItensSaude.ToList());
            System.Windows.MessageBox.Show("Verificação registrada.");

            CarregarVerificacoes();
            CarregarItensSaude();
            OnPropertyChanged(nameof(UltimaVerificacaoData));
            OnPropertyChanged(nameof(UltimaVerificacaoResponsavel));
            OnPropertyChanged(nameof(DataPrescricao));
            OnPropertyChanged(nameof(PrescricaoStatus));
        }

        [RelayCommand]
        private void NovaDiligencia()
        {
            if (ModoSomenteLeitura)
                return;

            var vm = new DiligenciaEditorViewModel(_processoId, CriarFacade());
            var tela = new DiligenciaEditorWindow(vm)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            tela.ShowDialog();
            CarregarDiligencias();
            CarregarHistorico();
        }

        [RelayCommand]
        private void ConcluirDiligencia(Diligencia d)
        {
            if (d == null)
                return;

            CriarFacade().ConcluirDiligencia(d.Id, _processoId);
            CarregarDiligencias();
            CarregarHistorico();
        }

        [RelayCommand]
        private void ReabrirDiligencia(Diligencia d)
        {
            if (d == null)
                return;

            CriarFacade().ReabrirDiligencia(d.Id, _processoId);
            CarregarDiligencias();
            CarregarHistorico();
        }

        [RelayCommand]
        private void GerarRelatorio()
        {
            try
            {
                var db = new DatabaseService();
                var modelo = new RelatorioProcessoService(db).GerarModelo(_processoId);

                var salvar = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF|*.pdf",
                    FileName = $"Processo_{Processo.Numero}.pdf"
                };

                if (salvar.ShowDialog() != true)
                    return;

                new PdfRelatorioProcessoService().GerarPdf(modelo, salvar.FileName);
                System.Windows.MessageBox.Show("Relatório gerado com sucesso.");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Erro ao gerar relatório: " + ex.Message);
            }
        }

        private ProcessoFacadeService CriarFacade()
        {
            var db = new DatabaseService();
            return new ProcessoFacadeService(
                _processService,
                new ContaService(db),
                new DiligenciaService(db),
                new HistoricoService(db),
                new ItemSaudeService(db),
                new VerificacaoService(db),
                new AuditService(db));
        }

        public bool PodeFechar()
        {
            if (ModoSomenteLeitura)
                return true;

            var r = System.Windows.MessageBox.Show(
                "Deseja sair sem salvar?",
                "Confirmação",
                MessageBoxButton.YesNo);

            return r == MessageBoxResult.Yes;
        }
    }
}
