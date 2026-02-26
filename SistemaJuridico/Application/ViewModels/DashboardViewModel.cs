using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuestPDF.Fluent;
using SistemaJuridico.Infrastructure;
using SistemaJuridico.Services;
using SistemaJuridico.Views;
using System.Windows;
using System.Collections.ObjectModel;
using System.Globalization;

namespace SistemaJuridico.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly ProcessService _service;
        private readonly ProcessoCacheService _cacheService;
        private readonly ActiveSessionService _activeSessionService;
        private readonly LoggerService _logger = new();

        private readonly List<ProcessoBuscaDashboardVM> _todosBusca = new();

        public ObservableCollection<ProcessoPrazoVM> ProcessosAtrasados { get; } = new();
        public ObservableCollection<ProcessoPrazoVM> ProcessosAAtrasar { get; } = new();
        public ObservableCollection<string> PainelAtrasadosResumo { get; } = new();
        public ObservableCollection<string> PainelAAtrasarResumo { get; } = new();
        public ObservableCollection<ActiveUserActivityVM> UsuariosAtivosRecentemente { get; } = new();
        public ObservableCollection<ProcessoBuscaDashboardVM> ProcessosBusca { get; } = new();
        public ObservableCollection<string> SugestoesBuscaRapida { get; } = new();

        [ObservableProperty]
        private int _totalAtrasados;

        [ObservableProperty]
        private int _totalAAtrasar;

        [ObservableProperty]
        private string _usuarioLogadoTexto = "";

        [ObservableProperty]
        private string _resumoRascunhosTexto = "Sem processos em rascunho ou edição pendente.";

        [ObservableProperty]
        private string _painelDetalhesProcessoTexto = "Nenhum processo selecionado. Pesquise ou selecione um processo para visualizar os detalhes abaixo.";

        [ObservableProperty]
        private bool _carregandoEmSegundoPlano;

        [ObservableProperty]
        private string _textoBusca = string.Empty;

        [ObservableProperty]
        private ProcessoBuscaDashboardVM? _processoBuscaSelecionado;

        [ObservableProperty]
        private int _totalBusca;

        [ObservableProperty]
        private bool _mostrarResultadosBusca;

        [ObservableProperty]
        private bool _buscaRapidaAtivada;

        [ObservableProperty]
        private ProcessoDetalhesViewModel? _processoDetalhesSelecionado;

        public bool DetalhesProcessoVisivel => ProcessoDetalhesSelecionado != null;

        public DashboardViewModel()
        {
            var db = new DatabaseService();
            _service = new ProcessService(db);
            _cacheService = new ProcessoCacheService(db);
            _activeSessionService = new ActiveSessionService(db);

            RegistrarAtividadeUsuarioAtual();
            Carregar();
        }

        partial void OnTextoBuscaChanged(string value)
        {
            AplicarSugestoesBuscaRapida();
            BuscaRapidaAtivada = !string.IsNullOrWhiteSpace(value) && value.Trim().Length >= 3;
            AplicarBuscaProcessos();
            MostrarResultadosBusca = BuscaRapidaAtivada && ProcessosBusca.Count > 0;
        }


        public void ExibirListaBuscaCompleta()
        {
            BuscaRapidaAtivada = true;
            AplicarBuscaProcessos();
            MostrarResultadosBusca = ProcessosBusca.Count > 0;
        }

        [RelayCommand]
        private void Carregar()
        {
            var cache = _cacheService.ObterCacheLeve();
            AplicarResumo(cache);

            CarregandoEmSegundoPlano = true;
            _ = Task.Run(() =>
            {
                var atualizado = _cacheService.AtualizarCache();
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    AplicarResumo(atualizado);
                    CarregandoEmSegundoPlano = false;
                });
            });
        }

        [RelayCommand]
        private void ExportarResumoPdf()
        {
            var save = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF|*.pdf",
                FileName = $"resumo_dashboard_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
            };

            if (save.ShowDialog() != true)
                return;

            var total = TotalAtrasados + TotalAAtrasar;
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(32);
                    page.Header().Text("Resumo Geral de Processos").Bold().FontSize(18);
                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Data: {DateTime.Now:dd/MM/yyyy HH:mm}");
                        col.Item().Text($"Total monitorados: {total}");
                        col.Item().Text($"Atrasados: {TotalAtrasados}");
                        col.Item().Text($"A atrasar: {TotalAAtrasar}");
                        col.Item().PaddingTop(10).Text("Processos atrasados").Bold();
                        foreach (var p in ProcessosAtrasados.Take(20))
                            col.Item().Text($"• {p.NumeroProcesso} - {p.Paciente} - prazo {p.PrazoTexto}");
                    });
                });
            }).GeneratePdf(save.FileName);

            System.Windows.MessageBox.Show("Resumo PDF exportado com sucesso.");
        }

        [RelayCommand]
        private void BuscarProcessos()
        {
            BuscaRapidaAtivada = !string.IsNullOrWhiteSpace(TextoBusca) && TextoBusca.Trim().Length >= 3;
            AplicarBuscaProcessos();
            MostrarResultadosBusca = BuscaRapidaAtivada && ProcessosBusca.Count > 0;
        }

        [RelayCommand]
        private void LimparBusca()
        {
            TextoBusca = string.Empty;
            BuscaRapidaAtivada = false;
            AplicarBuscaProcessos();
            AplicarSugestoesBuscaRapida();
            MostrarResultadosBusca = false;
        }

        [RelayCommand]
        private void NovoProcesso()
        {
            var window = new CadastroProcessoWindow();
            window.ShowDialog();
            Carregar();
        }

        [RelayCommand]
        private void AbrirProcessoBusca(ProcessoBuscaDashboardVM? processo)
        {
            if (processo == null || string.IsNullOrWhiteSpace(processo.Id))
                return;

            AbrirProcessoDetalhe(processo.Id, processo.Numero, processo.Paciente);
        }

        private void AplicarResumo(List<ProcessoResumoCacheItem> processos)
        {
            ProcessosAtrasados.Clear();
            ProcessosAAtrasar.Clear();
            PainelAtrasadosResumo.Clear();
            PainelAAtrasarResumo.Clear();
            UsuariosAtivosRecentemente.Clear();
            _todosBusca.Clear();

            int atrasados = 0;
            int aAtrasar = 0;
            int processosNaoConcluidos = 0;

            var hoje = DateTime.Today;
            var limiteAAtrasar = hoje.AddDays(7);
            var atual = App.Session.UsuarioAtual?.Email;
            UsuarioLogadoTexto = $"Logado como: {atual ?? "(não identificado)"}";

            foreach (var p in processos)
            {
                if (!string.Equals(p.SituacaoRascunho, "Concluído", StringComparison.OrdinalIgnoreCase))
                    processosNaoConcluidos++;

                _todosBusca.Add(new ProcessoBuscaDashboardVM
                {
                    Id = p.ProcessoId,
                    Numero = p.Numero,
                    Paciente = p.Paciente,
                    Genitor = p.Genitor,
                    Juiz = p.Juiz,
                    TipoProcesso = p.TipoProcesso,
                    StatusCalculado = p.StatusCalculado ?? p.StatusProcesso
                });

                if (!TryParsePrazo(p.PrazoFinal, out var prazo))
                    continue;

                var vmPrazo = new ProcessoPrazoVM
                {
                    ProcessoId = p.ProcessoId,
                    NumeroProcesso = p.Numero,
                    Paciente = p.Paciente,
                    Juiz = p.Juiz,
                    StatusCalculado = p.StatusCalculado ?? p.StatusProcesso,
                    Prazo = prazo,
                    SituacaoRascunho = string.IsNullOrWhiteSpace(p.SituacaoRascunho) ? "Concluído" : p.SituacaoRascunho
                };

                if (prazo.Date < hoje)
                {
                    atrasados++;
                    ProcessosAtrasados.Add(vmPrazo);
                }
                else if (prazo.Date <= limiteAAtrasar)
                {
                    aAtrasar++;
                    ProcessosAAtrasar.Add(vmPrazo);
                }
            }

            var atrasadosOrdenados = ProcessosAtrasados.OrderBy(p => p.Prazo).ThenBy(p => p.NumeroProcesso).ToList();
            var aAtrasarOrdenados = ProcessosAAtrasar.OrderBy(p => p.Prazo).ThenBy(p => p.NumeroProcesso).ToList();

            ProcessosAtrasados.Clear();
            foreach (var item in atrasadosOrdenados)
            {
                ProcessosAtrasados.Add(item);
                PainelAtrasadosResumo.Add($"{item.NumeroProcesso} — {item.Paciente} ({item.PrazoTexto})");
            }

            ProcessosAAtrasar.Clear();
            foreach (var item in aAtrasarOrdenados)
            {
                ProcessosAAtrasar.Add(item);
                PainelAAtrasarResumo.Add($"{item.NumeroProcesso} — {item.Paciente} ({item.PrazoTexto})");
            }

            foreach (var atividade in _activeSessionService.GetRecentUserActivity())
            {
                if (DateTimeOffset.TryParse(atividade.LastActivityTimestamp, out var dataAtividade))
                {
                    UsuariosAtivosRecentemente.Add(new ActiveUserActivityVM
                    {
                        NomeUsuario = atividade.UserName,
                        EmailUsuario = atividade.UserEmail,
                        UltimaAtividade = dataAtividade.LocalDateTime,
                        NumeroProcesso = atividade.LastProcessNumero,
                        PacienteProcesso = atividade.LastProcessPaciente
                    });
                }
            }

            TotalAtrasados = atrasados;
            TotalAAtrasar = aAtrasar;

            ResumoRascunhosTexto = processosNaoConcluidos == 0
                ? "Sem processos em rascunho ou edição pendente."
                : $"{processosNaoConcluidos} processo(s) com alterações não salvas (Rascunho/Em edição).";

            AplicarBuscaProcessos();
            AplicarSugestoesBuscaRapida();
            BuscaRapidaAtivada = false;
            MostrarResultadosBusca = false;
        }

        private void AplicarSugestoesBuscaRapida()
        {
            SugestoesBuscaRapida.Clear();

            if (string.IsNullOrWhiteSpace(TextoBusca))
                return;

            var termo = TextoBusca.Trim();
            var sugestoes = _todosBusca
                .Where(x =>
                    Contem(x.Numero, termo) ||
                    Contem(x.Paciente, termo) ||
                    Contem(x.Genitor, termo) ||
                    Contem(x.Juiz, termo) ||
                    Contem(x.TipoProcesso, termo) ||
                    Contem(x.StatusCalculado, termo))
                .Select(x => $"{x.Numero} — {x.Paciente}")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(12)
                .ToList();

            foreach (var item in sugestoes)
                SugestoesBuscaRapida.Add(item);
        }

        private void AplicarBuscaProcessos()
        {
            ProcessosBusca.Clear();

            if (!BuscaRapidaAtivada)
            {
                TotalBusca = 0;
                return;
            }

            IEnumerable<ProcessoBuscaDashboardVM> consulta = _todosBusca;

            if (!string.IsNullOrWhiteSpace(TextoBusca))
            {
                var termo = TextoBusca.Trim();
                consulta = consulta.Where(x =>
                    Contem(x.Numero, termo) ||
                    Contem(x.Paciente, termo) ||
                    Contem(x.Genitor, termo) ||
                    Contem(x.Juiz, termo) ||
                    Contem(x.TipoProcesso, termo) ||
                    Contem(x.StatusCalculado, termo));
            }

            var ordenados = consulta.OrderBy(x => x.Numero).Take(200).ToList();

            foreach (var item in ordenados)
                ProcessosBusca.Add(item);

            TotalBusca = consulta.Count();
        }

        private void RegistrarAtividadeUsuarioAtual()
        {
            var usuario = App.Session.UsuarioAtual;
            if (usuario == null)
                return;

            _activeSessionService.RecordUserActivity(usuario.Email, usuario.Nome, null, null, null);
        }

        private static bool TryParsePrazo(string? prazo, out DateTime data)
        {
            data = default;

            if (string.IsNullOrWhiteSpace(prazo))
                return false;

            return DateTime.TryParseExact(prazo, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out data)
                || DateTime.TryParseExact(prazo, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out data)
                || DateTime.TryParse(prazo, out data);
        }

        private static bool Contem(string? fonte, string termo)
            => !string.IsNullOrWhiteSpace(fonte) && fonte.Contains(termo, StringComparison.OrdinalIgnoreCase);

        [RelayCommand]
        private void AbrirProcesso(ProcessoPrazoVM? processoPrazo)
        {
            if (processoPrazo == null || string.IsNullOrWhiteSpace(processoPrazo.ProcessoId))
                return;

            AbrirProcessoDetalhe(processoPrazo.ProcessoId, processoPrazo.NumeroProcesso, processoPrazo.Paciente);
        }

        private void AbrirProcessoDetalhe(string processoId, string numeroProcesso, string paciente)
        {
            try
            {
                if (ProcessoDetalhesSelecionado != null)
                {
                    if (!ProcessoDetalhesSelecionado.PodeFechar())
                        return;

                    ProcessoDetalhesSelecionado.LiberarLock();
                }

                var usuario = App.Session.UsuarioAtual;
                if (usuario != null)
                {
                    _activeSessionService.RecordUserActivity(
                        usuario.Email,
                        usuario.Nome,
                        processoId,
                        numeroProcesso,
                        paciente);
                }

                _logger.Info($"Abrindo processo {processoId} ({numeroProcesso}) a partir do dashboard.");
                PainelDetalhesProcessoTexto =
                    $"Processo selecionado: {numeroProcesso} - {paciente}.{Environment.NewLine}{Environment.NewLine}" +
                    "As abas de verificação, contas e histórico foram carregadas abaixo no próprio dashboard.";

                ProcessoDetalhesSelecionado = new ProcessoDetalhesViewModel(processoId, _service);
                OnPropertyChanged(nameof(DetalhesProcessoVisivel));
            }
            catch (Exception ex)
            {
                _logger.Error($"Falha ao abrir detalhes do processo {processoId} pelo dashboard", ex);
                System.Windows.MessageBox.Show(
                    $"Não foi possível abrir os detalhes do processo.\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        public void FecharProcessoSelecionado()
        {
            if (ProcessoDetalhesSelecionado == null)
                return;

            ProcessoDetalhesSelecionado.LiberarLock();
            ProcessoDetalhesSelecionado = null;
            OnPropertyChanged(nameof(DetalhesProcessoVisivel));
        }
    }

    public class ProcessoPrazoVM
    {
        public string ProcessoId { get; set; } = string.Empty;
        public string NumeroProcesso { get; set; } = string.Empty;
        public string Paciente { get; set; } = string.Empty;
        public string Juiz { get; set; } = string.Empty;
        public string StatusCalculado { get; set; } = string.Empty;
        public DateTime Prazo { get; set; }
        public string SituacaoRascunho { get; set; } = "Concluído";

        public string PrazoTexto => Prazo.ToString("dd/MM/yyyy");

        public string StatusSalvamentoTexto => string.Equals(SituacaoRascunho, "Concluído", StringComparison.OrdinalIgnoreCase)
            ? "Salvo"
            : $"Não salvo ({SituacaoRascunho})";
    }

    public class ProcessoBuscaDashboardVM
    {
        public string Id { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string Paciente { get; set; } = string.Empty;
        public string? Genitor { get; set; }
        public string Juiz { get; set; } = string.Empty;
        public string? TipoProcesso { get; set; }
        public string? StatusCalculado { get; set; }
    }

    public class ActiveUserActivityVM
    {
        public string NomeUsuario { get; set; } = string.Empty;
        public string EmailUsuario { get; set; } = string.Empty;
        public DateTime UltimaAtividade { get; set; }
        public string? NumeroProcesso { get; set; }
        public string? PacienteProcesso { get; set; }

        public string UltimaAtividadeTexto => UltimaAtividade.ToString("dd/MM/yyyy HH:mm");

        public string UltimaAtividadeRelativaTexto
        {
            get
            {
                var diferenca = DateTime.Now - UltimaAtividade;

                if (diferenca.TotalMinutes < 1)
                    return "agora";

                if (diferenca.TotalHours < 1)
                    return $"há {Math.Floor(diferenca.TotalMinutes)} min";

                if (diferenca.TotalDays < 1)
                    return $"há {Math.Floor(diferenca.TotalHours)} h";

                if (diferenca.TotalDays < 30)
                    return $"há {Math.Floor(diferenca.TotalDays)} dias";

                return UltimaAtividadeTexto;
            }
        }

        public string UltimoProcessoTexto
        {
            get
            {
                if (string.IsNullOrWhiteSpace(NumeroProcesso) && string.IsNullOrWhiteSpace(PacienteProcesso))
                    return "Sem processo recente";

                if (string.IsNullOrWhiteSpace(PacienteProcesso))
                    return NumeroProcesso ?? string.Empty;

                if (string.IsNullOrWhiteSpace(NumeroProcesso))
                    return PacienteProcesso;

                return $"{NumeroProcesso} - {PacienteProcesso}";
            }
        }
    }
}
