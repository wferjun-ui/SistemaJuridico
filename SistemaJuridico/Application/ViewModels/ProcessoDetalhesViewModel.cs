using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaJuridico.Infrastructure;
using SistemaJuridico.Models;
using SistemaJuridico.Services;
using SistemaJuridico.Views;
using System.Collections.ObjectModel;
using System.Windows;
using System.Threading.Tasks;

namespace SistemaJuridico.ViewModels
{
    public partial class ProcessoDetalhesViewModel : ObservableObject
    {
        private readonly ProcessService _processService;
        private readonly ContaService _contaService;
        private readonly ItemSaudeService _itemSaudeService;
        private readonly VerificacaoService _verificacaoService;
        private readonly LoggerService _logger = new();

        private readonly string _processoId;

        private System.Windows.Threading.DispatcherTimer? _lockTimer;
        private bool _lockAdquirido;

        public Processo Processo { get; private set; }

        public ObservableCollection<Conta> Contas { get; } = new();
        public ObservableCollection<ItemSaude> ItensSaude { get; } = new();
        public ObservableCollection<Verificacao> Verificacoes { get; } = new();
        public ObservableCollection<Diligencia> Diligencias { get; } = new();

        [ObservableProperty]
        private bool _modoSomenteLeitura;

        [ObservableProperty]
        private string _usuarioEditandoTexto = "";

        public ProcessoDetalhesViewModel(
            string processoId,
            ProcessService processService)
        {
            _processService = processService;

            var db = new DatabaseService();

            _contaService = new ContaService(db);
            _itemSaudeService = new ItemSaudeService(db);
            _verificacaoService = new VerificacaoService(db);

            _processoId = processoId;

            Processo = _processService
                .ListarProcessos()
                .FirstOrDefault(x => x.Id == processoId)
                ?? new Processo { Id = processoId };

            ValidarLock();

            CarregarContas();
            CarregarItensSaude();
            CarregarVerificacoes();
            CarregarDiligencias();
        }

        // ========================
        // LOCK MULTIUSUÁRIO
        // ========================

        private void ValidarLock()
        {
            var usuario = _processService.UsuarioEditando(_processoId);
            var atual = App.Session.UsuarioAtual?.Email;

            if (usuario != null && usuario != atual)
            {
                ModoSomenteLeitura = true;
                UsuarioEditandoTexto =
                    $"Processo em edição por {usuario}";
            }
            else
            {
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

            TentarLiberarLock();
        }

        private void TentarLiberarLock()
        {
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

        // ========================
        // LOADERS
        // ========================

        private void CarregarContas()
        {
            Contas.Clear();

            foreach (var c in _contaService.ListarPorProcesso(_processoId))
                Contas.Add(c);
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

            foreach (var v in _verificacaoService
                         .ListarPorProcesso(_processoId)
                         .OrderByDescending(x => x.DataHora))
            {
                Verificacoes.Add(v);
            }
        }

        private void CarregarDiligencias()
        {
            Diligencias.Clear();

            var lista = new DiligenciaService(new DatabaseService())
                .ListarPorProcesso(_processoId);

            foreach (var d in lista)
                Diligencias.Add(d);
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

            return Task.CompletedTask;
        }

        public Task CarregarAsync(ProcessoCompletoDTO processo)
        {
            Processo = processo.Processo;

            Contas.Clear();
            foreach (var conta in processo.Contas)
                Contas.Add(conta);

            ItensSaude.Clear();
            foreach (var item in processo.ItensSaude)
                ItensSaude.Add(item);

            Verificacoes.Clear();
            foreach (var verificacao in processo.Verificacoes.OrderByDescending(x => x.DataHora))
                Verificacoes.Add(verificacao);

            Diligencias.Clear();
            foreach (var diligencia in processo.Diligencias)
                Diligencias.Add(diligencia);

            return Task.CompletedTask;
        }
        // ========================
        // COMANDOS
        // ========================

        [RelayCommand]
        private void SalvarRascunho()
        {
            var motivo = Microsoft.VisualBasic.Interaction.InputBox(
                "Informe o motivo do rascunho:");

            if (string.IsNullOrWhiteSpace(motivo))
                return;

            _processService.MarcarRascunho(_processoId, motivo);

            MessageBox.Show("Rascunho salvo.");
        }

        [RelayCommand]
        private void ConcluirEdicao()
        {
            _processService.MarcarConcluido(_processoId);

            MessageBox.Show("Edição concluída.");
        }

        // ========================
        // VERIFICAÇÕES
        // ========================

        [RelayCommand]
        private void NovaVerificacao()
        {
            if (ModoSomenteLeitura)
                return;

            var facade = new VerificacaoFacadeService();

            var status = Microsoft.VisualBasic.Interaction.InputBox(
                "Informe o status do processo:");

            if (string.IsNullOrWhiteSpace(status))
                return;

            var descricao = Microsoft.VisualBasic.Interaction.InputBox(
                "Descrição da verificação:");

            var responsavel = App.Session.UsuarioAtual?.Nome ?? "Sistema";

            facade.CriarVerificacao(
                _processoId,
                status,
                responsavel,
                descricao,
                ItensSaude.ToList());

            MessageBox.Show("Verificação registrada.");

            CarregarVerificacoes();
            CarregarItensSaude();
        }

        // ========================
        // DILIGÊNCIAS
        // ========================

        [RelayCommand]
        private void NovaDiligencia()
        {
            if (ModoSomenteLeitura)
                return;

            var facade = CriarFacade();

            var vm = new DiligenciaEditorViewModel(
                _processoId,
                facade);

            var tela = new DiligenciaEditorWindow(vm)
            {
                Owner = Application.Current.MainWindow
            };

            tela.ShowDialog();

            CarregarDiligencias();
        }

        [RelayCommand]
        private void ConcluirDiligencia(Diligencia d)
        {
            if (d == null) return;

            var facade = CriarFacade();

            facade.ConcluirDiligencia(d.Id, _processoId);

            CarregarDiligencias();
        }

        [RelayCommand]
        private void ReabrirDiligencia(Diligencia d)
        {
            if (d == null) return;

            var facade = CriarFacade();

            facade.ReabrirDiligencia(d.Id, _processoId);

            CarregarDiligencias();
        }

        // ========================
        // RELATÓRIO PDF
        // ========================

        [RelayCommand]
        private void GerarRelatorio()
        {
            try
            {
                var db = new DatabaseService();

                var modelo = new RelatorioProcessoService(db)
                    .GerarModelo(_processoId);

                var salvar = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF|*.pdf",
                    FileName = $"Processo_{Processo.Numero}.pdf"
                };

                if (salvar.ShowDialog() != true)
                    return;

                new PdfRelatorioProcessoService()
                    .GerarPdf(modelo, salvar.FileName);

                MessageBox.Show("Relatório gerado com sucesso.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao gerar relatório: " + ex.Message);
            }
        }

        // ========================
        // FACADE
        // ========================

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
        new AuditService(db) // ⭐ NOVO — auditoria
    );
}

        // ========================
        // CONTROLE FECHAMENTO
        // ========================

        public bool PodeFechar()
        {
            if (ModoSomenteLeitura)
                return true;

            var r = MessageBox.Show(
                "Deseja sair sem salvar?",
                "Confirmação",
                MessageBoxButton.YesNo);

            return r == MessageBoxResult.Yes;
        }
    }
}
