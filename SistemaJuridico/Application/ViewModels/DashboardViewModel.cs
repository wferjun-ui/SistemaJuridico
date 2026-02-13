using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuestPDF.Fluent;
using SistemaJuridico.Services;
using SistemaJuridico.Views;
using System.Collections.ObjectModel;
using System.Globalization;

namespace SistemaJuridico.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly ProcessService _service;
        private readonly ProcessoCacheService _cacheService;

        public ObservableCollection<ProcessoPrazoVM> ProcessosAtrasados { get; } = new();
        public ObservableCollection<ProcessoPrazoVM> ProcessosAAtrasar { get; } = new();
        public ObservableCollection<string> ProcessosBloqueados { get; } = new();

        [ObservableProperty]
        private int _totalAtrasados;

        [ObservableProperty]
        private int _totalAAtrasar;

        [ObservableProperty]
        private int _totalBloqueados;

        [ObservableProperty]
        private string _usuarioLogadoTexto = "";

        [ObservableProperty]
        private string _resumoRascunhosTexto = "Sem processos em rascunho ou edição pendente.";

        [ObservableProperty]
        private bool _carregandoEmSegundoPlano;

        public DashboardViewModel()
        {
            var db = new DatabaseService();
            _service = new ProcessService(db);
            _cacheService = new ProcessoCacheService(db);

            Carregar();
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
                        col.Item().Text($"Bloqueados: {TotalBloqueados}");
                        col.Item().PaddingTop(10).Text("Processos atrasados").Bold();
                        foreach (var p in ProcessosAtrasados.Take(20))
                            col.Item().Text($"• {p.NumeroProcesso} - {p.Paciente} - prazo {p.PrazoTexto}");
                    });
                });
            }).GeneratePdf(save.FileName);

            System.Windows.MessageBox.Show("Resumo PDF exportado com sucesso.");
        }

        private void AplicarResumo(List<ProcessoResumoCacheItem> processos)
        {
            ProcessosAtrasados.Clear();
            ProcessosAAtrasar.Clear();
            ProcessosBloqueados.Clear();

            int atrasados = 0;
            int aAtrasar = 0;
            int bloqueados = 0;
            int processosNaoConcluidos = 0;

            var hoje = DateTime.Today;
            var limiteAAtrasar = hoje.AddDays(7);
            var atual = App.Session.UsuarioAtual?.Email;
            UsuarioLogadoTexto = $"Logado como: {atual ?? "(não identificado)"}";

            foreach (var p in processos)
            {
                if (!string.Equals(p.SituacaoRascunho, "Concluído", StringComparison.OrdinalIgnoreCase))
                    processosNaoConcluidos++;

                var usuarioLock = _service.UsuarioEditando(p.ProcessoId);
                if (!string.IsNullOrWhiteSpace(usuarioLock) && usuarioLock != atual)
                {
                    bloqueados++;
                    ProcessosBloqueados.Add($"{p.Numero} - {usuarioLock}");
                }

                if (!TryParsePrazo(p.PrazoFinal, out var prazo))
                    continue;

                var vmPrazo = new ProcessoPrazoVM
                {
                    ProcessoId = p.ProcessoId,
                    NumeroProcesso = p.Numero,
                    Paciente = p.Paciente,
                    Juiz = p.Juiz,
                    StatusCalculado = p.StatusCalculado ?? p.StatusProcesso,
                    DescricaoDiligencia = "Prazo da última verificação",
                    Prazo = prazo,
                    SituacaoRascunho = string.IsNullOrWhiteSpace(p.SituacaoRascunho) ? "Concluído" : p.SituacaoRascunho,
                    MotivoRascunho = p.MotivoRascunho
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

            TotalAtrasados = atrasados;
            TotalAAtrasar = aAtrasar;
            TotalBloqueados = bloqueados;

            ResumoRascunhosTexto = processosNaoConcluidos == 0
                ? "Sem processos em rascunho ou edição pendente."
                : $"{processosNaoConcluidos} processo(s) com alterações não salvas (Rascunho/Em edição).";
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

        [RelayCommand]
        private void AbrirProcesso(ProcessoPrazoVM? processoPrazo)
        {
            if (processoPrazo == null || string.IsNullOrWhiteSpace(processoPrazo.ProcessoId))
                return;

            var tela = new ProcessoDetalhesWindow(processoPrazo.ProcessoId);
            tela.ShowDialog();

            Carregar();
        }
    }

    public class ProcessoPrazoVM
    {
        public string ProcessoId { get; set; } = string.Empty;
        public string NumeroProcesso { get; set; } = string.Empty;
        public string Paciente { get; set; } = string.Empty;
        public string Juiz { get; set; } = string.Empty;
        public string StatusCalculado { get; set; } = string.Empty;
        public string DescricaoDiligencia { get; set; } = string.Empty;
        public DateTime Prazo { get; set; }
        public string SituacaoRascunho { get; set; } = "Concluído";
        public string? MotivoRascunho { get; set; }

        public string PrazoTexto => Prazo.ToString("dd/MM/yyyy");

        public string StatusSalvamentoTexto => string.Equals(SituacaoRascunho, "Concluído", StringComparison.OrdinalIgnoreCase)
            ? "Salvo"
            : $"Não salvo ({SituacaoRascunho})";
    }
}
