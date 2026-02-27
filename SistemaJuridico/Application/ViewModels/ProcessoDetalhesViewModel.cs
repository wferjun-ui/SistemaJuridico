using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaJuridico.Infrastructure;
using SistemaJuridico.Models;
using SistemaJuridico.Services;
using SistemaJuridico.Views;
using System.Collections.ObjectModel;
using System.Globalization;
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
        private readonly AuditService _auditService;
        private readonly VerificacaoFacadeService _verificacaoFacade;
        private readonly LoggerService _logger = new();
        private readonly string _processoId;
        private readonly AppStateViewModel _appState = AppStateViewModel.Instance;

        private System.Windows.Threading.DispatcherTimer? _lockTimer;
        private bool _lockAdquirido;
        private DateTime? _ultimaVerificacaoDate;
        private string _ultimaVerificacaoData = "Sem verificação";
        private string _ultimaVerificacaoResponsavel = "N/D";
        private string _statusVerificacao = "";
        private string _descricaoVerificacao = "";
        private string _descricaoDiligencia = "";
        private string _descricaoPendencias = "";
        private string _prazoDiligencia = "";
        private string _proximaVerificacao = "";
        private string _diligenciaStatus = "Não realizada";
        private string _descricaoPersistente = "";
        private string _responsavelVerificacao = "";
        private bool _diligenciaRealizada;
        private bool _possuiPendencias;

        public IReadOnlyList<string> StatusProcessoOpcoes { get; } = new[]
        {
            "Cumprimento de Sentença",
            "Cumprimento Provisório de Sentença",
            "Conhecimento",
            "Recurso Inominado",
            "Apelação",
            "Agravo",
            "Suspenso",
            "Arquivado",
            "Cumprimento Extinto",
            "Desistência da Parte"
        };

        public IReadOnlyList<string> DiligenciaOpcoes { get; } = new[]
        {
            "Não realizada",
            "Pendente",
            "Concluída"
        };

        public bool IsStatusFinalizado => new[] { "Arquivado", "Desistência da Parte", "Cumprimento Extinto" }
            .Contains(StatusVerificacao);

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

        [ObservableProperty]
        private bool _isEditDialogOpen;

        [ObservableProperty]
        private bool _isDeleteDialogOpen;

        [ObservableProperty]
        private string _numeroEdicao = "";

        [ObservableProperty]
        private string _pacienteEdicao = "";

        [ObservableProperty]
        private string _representanteEdicao = "";

        [ObservableProperty]
        private string _juizEdicao = "";

        [ObservableProperty]
        private string _statusFaseEdicao = "";

        public string StatusVerificacao
        {
            get => _statusVerificacao;
            set
            {
                if (!SetProperty(ref _statusVerificacao, value))
                    return;

                if (IsStatusFinalizado)
                {
                    DescricaoVerificacao = string.Empty;
                    DescricaoDiligencia = string.Empty;
                    DescricaoPendencias = string.Empty;
                    PrazoDiligencia = string.Empty;
                    DiligenciaRealizada = false;
                    PossuiPendencias = false;
                }

                OnPropertyChanged(nameof(IsStatusFinalizado));
            }
        }

        public string DescricaoVerificacao
        {
            get => _descricaoVerificacao;
            set => SetProperty(ref _descricaoVerificacao, value);
        }

        public string DescricaoDiligencia
        {
            get => _descricaoDiligencia;
            set => SetProperty(ref _descricaoDiligencia, value);
        }

        public string DescricaoPendencias
        {
            get => _descricaoPendencias;
            set => SetProperty(ref _descricaoPendencias, value);
        }

        public string PrazoDiligencia
        {
            get => _prazoDiligencia;
            set => SetProperty(ref _prazoDiligencia, value);
        }

        public string ProximaVerificacao
        {
            get => _proximaVerificacao;
            set => SetProperty(ref _proximaVerificacao, FormatarDataDigitada(value));
        }

        public string DiligenciaStatus
        {
            get => _diligenciaStatus;
            set => SetProperty(ref _diligenciaStatus, value);
        }

        public string DescricaoPersistente
        {
            get => _descricaoPersistente;
            set => SetProperty(ref _descricaoPersistente, value);
        }

        public string ResponsavelVerificacao
        {
            get => _responsavelVerificacao;
            set => SetProperty(ref _responsavelVerificacao, value);
        }

        public bool DiligenciaRealizada
        {
            get => _diligenciaRealizada;
            set => SetProperty(ref _diligenciaRealizada, value);
        }

        public bool PossuiPendencias
        {
            get => _possuiPendencias;
            set => SetProperty(ref _possuiPendencias, value);
        }

        partial void OnModoSomenteLeituraChanged(bool value)
        {
            OnPropertyChanged(nameof(PodeEditarProcesso));
        }

        public ProcessoDetalhesViewModel(string processoId, ProcessService processService)
        {
            _processService = processService;

            var db = new DatabaseService();
            _contaService = new ContaService(db);
            _itemSaudeService = new ItemSaudeService(db);
            _verificacaoService = new VerificacaoService(db);
            _historicoService = new HistoricoService(db);
            _auditService = new AuditService(db);
            _verificacaoFacade = new VerificacaoFacadeService();

            _processoId = processoId;

            Processo = _processService
                .ListarProcessos()
                .FirstOrDefault(x => x.Id == processoId)
                ?? new Processo { Id = processoId };

            _appState.DefinirContexto(App.Session.UsuarioAtual, Processo);
            ResponsavelVerificacao = App.Session.UsuarioAtual?.Nome ?? "Sistema";
            ValidarLock();
            RecarregarTudo();
        }

        public bool PodeAcessarAbaVerificacao => _appState.PodeAcessarVerificacao;
        public bool PodeEditarProcesso => _appState.PodeEditarProcesso && !ModoSomenteLeitura;

        public decimal TotalContasAPrestar => Contas
            .Where(c => !string.Equals(c.StatusConta, "fechada", StringComparison.OrdinalIgnoreCase))
            .Sum(c => c.ValorConta);

        public string UltimaVerificacaoData => _ultimaVerificacaoData;

        public string UltimaVerificacaoResponsavel => _ultimaVerificacaoResponsavel;

        public string HashProcesso => Processo.Id;

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

        public string StatusAtualResumo
        {
            get
            {
                var ultima = Verificacoes.FirstOrDefault();
                if (ultima == null)
                    return "Sem verificação";

                var dataBase = ParseData(ultima.ProximaVerificacao) ?? ParseData(ultima.ProximoPrazo);
                var pendencia = ultima.DiligenciaPendente ? "com pendência" : "sem pendência";

                if (dataBase is null)
                    return $"Sem prazo ({pendencia})";

                var dias = (dataBase.Value.Date - DateTime.Today).Days;
                if (dias < 0)
                    return $"Atrasado ({pendencia})";
                if (dias <= 7)
                    return $"Para atrasar ({pendencia})";

                return $"Em dia ({pendencia})";
            }
        }

        public bool PodeDesfazerUltimaVerificacao
            => _appState.PodeDesfazerVerificacao &&
               Verificacoes.Count(v => !new[]
               {
                   "Lançamento Contábil de Lote",
                   "Edição de Conta Individual",
                   "Exclusão de Conta Individual",
                   "Edição de Conta Antiga",
                   "Exclusão de Conta Antiga",
                   "Lote de Contas Desfeito",
                   "Verificação Desfeita"
               }.Contains(v.StatusProcesso)) > 1;

        private void RecarregarTudo()
        {
            CarregarContas();
            CarregarItensSaude();
            CarregarVerificacoes();
            CarregarDiligencias();
            CarregarHistorico();
            if (string.IsNullOrWhiteSpace(StatusVerificacao))
                StatusVerificacao = Processo.StatusFase;
        }

        private void ValidarLock()
        {
            var usuario = _processService.UsuarioEditando(_processoId);
            var atual = App.Session.UsuarioAtual?.Email;

            if (usuario != null && usuario != atual)
            {
                ModoSomenteLeitura = true;
                UsuarioEditandoTexto = $"Processo em edição por {usuario}";
                OnPropertyChanged(nameof(PodeEditarProcesso));
                return;
            }

            var lockObtido = _processService.TentarLock(_processoId);
            if (!lockObtido)
            {
                ModoSomenteLeitura = true;
                UsuarioEditandoTexto = "Processo em edição por outro usuário.";
                OnPropertyChanged(nameof(PodeEditarProcesso));
                return;
            }

            _lockAdquirido = true;
            OnPropertyChanged(nameof(PodeEditarProcesso));
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

            _appState.AtualizarContas(Contas);
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

            _appState.AtualizarVerificacoes(verificacoes);

            var ultima = verificacoes.FirstOrDefault();
            _ultimaVerificacaoData = FormatarDataHoraVerificacao(ultima?.DataHora);
            _ultimaVerificacaoResponsavel = string.IsNullOrWhiteSpace(ultima?.Responsavel) ? "N/D" : ultima.Responsavel;
            _ultimaVerificacaoDate = ParseData(ultima?.DataHora);
            ProximaVerificacao = ultima?.ProximaVerificacao ?? ultima?.ProximoPrazo ?? string.Empty;
            DiligenciaStatus = ultima?.DiligenciaStatus ?? (ultima?.DiligenciaPendente == true ? "Pendente" : ultima?.DiligenciaRealizada == true ? "Concluída" : "Não realizada");
            DescricaoPersistente = ultima?.DescricaoPersistente ?? string.Empty;

            OnPropertyChanged(nameof(UltimaVerificacaoData));
            OnPropertyChanged(nameof(UltimaVerificacaoResponsavel));
            OnPropertyChanged(nameof(DataPrescricao));
            OnPropertyChanged(nameof(PrescricaoStatus));
            OnPropertyChanged(nameof(StatusAtualResumo));
            OnPropertyChanged(nameof(PodeDesfazerUltimaVerificacao));
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

        private static string FormatarDataHoraVerificacao(string? valor)
        {
            var data = ParseData(valor);
            return data?.ToString("dd/MM/yyyy HH:mm") ?? "Sem verificação";
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
            _ultimaVerificacaoData = FormatarDataHoraVerificacao(ultima?.DataHora);
            _ultimaVerificacaoResponsavel = string.IsNullOrWhiteSpace(ultima?.Responsavel) ? "N/D" : ultima.Responsavel;
            _ultimaVerificacaoDate = ParseData(ultima?.DataHora);
            ProximaVerificacao = ultima?.ProximaVerificacao ?? ultima?.ProximoPrazo ?? string.Empty;
            DiligenciaStatus = ultima?.DiligenciaStatus ?? (ultima?.DiligenciaPendente == true ? "Pendente" : ultima?.DiligenciaRealizada == true ? "Concluída" : "Não realizada");
            DescricaoPersistente = ultima?.DescricaoPersistente ?? string.Empty;
            OnPropertyChanged(nameof(UltimaVerificacaoData));
            OnPropertyChanged(nameof(UltimaVerificacaoResponsavel));
            OnPropertyChanged(nameof(DataPrescricao));
            OnPropertyChanged(nameof(PrescricaoStatus));
            OnPropertyChanged(nameof(StatusAtualResumo));

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
            if (ModoSomenteLeitura || !_appState.PodeAcessarVerificacao)
                return;

            if (string.IsNullOrWhiteSpace(StatusVerificacao))
            {
                System.Windows.MessageBox.Show("Informe o status do processo.");
                return;
            }

            if (!IsStatusFinalizado && string.IsNullOrWhiteSpace(DescricaoDiligencia))
            {
                System.Windows.MessageBox.Show("Informe a diligência.");
                return;
            }

            if (!IsStatusFinalizado && string.IsNullOrWhiteSpace(DescricaoVerificacao))
            {
                System.Windows.MessageBox.Show("Descreva o que foi feito na verificação.");
                return;
            }

            var facade = new VerificacaoFacadeService();
            facade.CriarVerificacaoCompleta(
                processoId: _processoId,
                statusProcesso: StatusVerificacao,
                responsavel: string.IsNullOrWhiteSpace(ResponsavelVerificacao) ? (App.Session.UsuarioAtual?.Nome ?? "Sistema") : ResponsavelVerificacao,
                descricao: DescricaoVerificacao,
                diligenciaRealizada: DiligenciaStatus == "Concluída",
                descricaoDiligencia: DescricaoDiligencia,
                possuiPendencias: DiligenciaStatus == "Pendente" || PossuiPendencias,
                descricaoPendencias: DescricaoPendencias,
                prazoDiligencia: PrazoDiligencia,
                proximoPrazoPadrao: ProximaVerificacao,
                dataNotificacao: string.Empty,
                diligenciaStatus: DiligenciaStatus,
                descricaoPersistente: DescricaoPersistente,
                itensSnapshot: ItensSaude.ToList());

            System.Windows.MessageBox.Show("Verificação registrada.");

            DescricaoVerificacao = string.Empty;
            DiligenciaRealizada = false;
            DiligenciaStatus = "Não realizada";
            DescricaoDiligencia = string.Empty;
            PossuiPendencias = false;
            DescricaoPendencias = string.Empty;
            PrazoDiligencia = string.Empty;

            CarregarVerificacoes();
            CarregarItensSaude();
            CarregarHistorico();
            OnPropertyChanged(nameof(UltimaVerificacaoData));
            OnPropertyChanged(nameof(UltimaVerificacaoResponsavel));
            OnPropertyChanged(nameof(DataPrescricao));
            OnPropertyChanged(nameof(PrescricaoStatus));
            OnPropertyChanged(nameof(StatusAtualResumo));
        }


        [RelayCommand]
        private void DesfazerUltimaVerificacao()
        {
            if (!_appState.PodeDesfazerVerificacao)
                return;

            var confirmar = System.Windows.MessageBox.Show(
                "Deseja realmente desfazer a última verificação geral deste processo?",
                "Confirmação",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmar != MessageBoxResult.Yes)
                return;

            try
            {
                _verificacaoFacade.DesfazerUltimaVerificacaoGeral(_processoId);
                CarregarVerificacoes();
                CarregarItensSaude();
                CarregarHistorico();
                OnPropertyChanged(nameof(UltimaVerificacaoData));
                OnPropertyChanged(nameof(UltimaVerificacaoResponsavel));
                OnPropertyChanged(nameof(DataPrescricao));
                OnPropertyChanged(nameof(PrescricaoStatus));
            OnPropertyChanged(nameof(StatusAtualResumo));
                System.Windows.MessageBox.Show("Última verificação desfeita com sucesso.");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Erro ao desfazer", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
        private void SalvarProcesso()
        {
            if (!PodeEditarProcesso)
                return;

            _processService.AtualizarProcesso(Processo);
            _historicoService.Registrar(_processoId, "Processo atualizado", $"Status: {Processo.StatusFase}");
            _auditService.Registrar("Processo.Editado", "processo", _processoId, "Dados cadastrais atualizados");
            System.Windows.MessageBox.Show("Processo atualizado com sucesso.");
        }

        [RelayCommand]
        private void AbrirEdicaoProcesso()
        {
            NumeroEdicao = Processo.Numero;
            PacienteEdicao = Processo.Paciente;
            RepresentanteEdicao = Processo.Representante;
            JuizEdicao = Processo.Juiz;
            StatusFaseEdicao = Processo.StatusFase;
            IsEditDialogOpen = true;
        }

        [RelayCommand]
        private void CancelarEdicaoProcesso() => IsEditDialogOpen = false;

        [RelayCommand]
        private void ConfirmarEdicaoProcesso()
        {
            if (!PodeEditarProcesso)
                return;

            Processo.Numero = NumeroEdicao;
            Processo.Paciente = PacienteEdicao;
            Processo.Representante = RepresentanteEdicao;
            Processo.Juiz = JuizEdicao;
            Processo.StatusFase = StatusFaseEdicao;

            SalvarProcesso();
            OnPropertyChanged(nameof(Processo));
            IsEditDialogOpen = false;
        }

        [RelayCommand]
        private void AbrirConfirmacaoExclusao() => IsDeleteDialogOpen = true;

        [RelayCommand]
        private void CancelarExclusao() => IsDeleteDialogOpen = false;

        [RelayCommand]
        private void ExcluirProcesso()
        {
            if (!_appState.IsAdministrador)
                return;

            _processService.ExcluirProcesso(_processoId);
            _auditService.Registrar("Processo.Excluido", "processo", _processoId, "Exclusão completa de processo");
            IsDeleteDialogOpen = false;
            System.Windows.MessageBox.Show("Processo excluído com sucesso.");
            LiberarLock();

            if (System.Windows.Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this) is Window w)
                w.Close();
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

        private static string FormatarDataDigitada(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return string.Empty;

            if (DateTime.TryParse(valor, out var data))
                return data.ToString("dd/MM/yyyy");

            var digits = new string(valor.Where(char.IsDigit).Take(8).ToArray());
            if (digits.Length <= 2)
                return digits;
            if (digits.Length <= 4)
                return $"{digits[..2]}/{digits[2..]}";
            return $"{digits[..2]}/{digits[2..4]}/{digits[4..]}";
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
