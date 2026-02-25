using SistemaJuridico.Infrastructure;
using SistemaJuridico.Models;
using System.Globalization;

namespace SistemaJuridico.Services
{
    /// <summary>
    /// Camada de paridade com o contrato funcional do antigo backend Google Apps Script
    /// (codigo.gs.txt), adaptada para o domínio em C#.
    /// </summary>
    public class AppsScriptParityService
    {
        private readonly ProcessoCacheService _processoCacheService;
        private readonly ProcessoFacadeService _processoFacadeService;
        private readonly VerificacaoFacadeService _verificacaoFacadeService;
        private readonly RelatorioProcessoService _relatorioProcessoService;
        private readonly ActiveSessionService _activeSessionService;

        public AppsScriptParityService(DatabaseService db)
        {
            _processoCacheService = new ProcessoCacheService(db);
            _activeSessionService = new ActiveSessionService(db);
            _relatorioProcessoService = new RelatorioProcessoService(db);

            // Reusa os serviços já registrados no ServiceLocator para manter comportamento atual.
            _processoFacadeService = new ProcessoFacadeService(
                ServiceLocator.ProcessService,
                ServiceLocator.ContaService,
                ServiceLocator.DiligenciaService,
                ServiceLocator.HistoricoService,
                ServiceLocator.ItemSaudeService,
                ServiceLocator.VerificacaoService,
                ServiceLocator.AuditService);

            _verificacaoFacadeService = new VerificacaoFacadeService();
        }

        public DashboardInitialDataDto GetInitialDashboardData()
        {
            var allProcesses = _processoCacheService.ObterCacheLeve();
            var hoje = DateTime.Today;
            var limiteAAtrasar = hoje.AddDays(7);

            var atrasados = allProcesses
                .Where(p => TryParseData(p.PrazoFinal, out var prazo) && prazo < hoje)
                .OrderBy(p => p.PrazoFinal)
                .Select(ToDashboardItem)
                .ToList();

            var paraAtrasar = allProcesses
                .Where(p => TryParseData(p.PrazoFinal, out var prazo) && prazo >= hoje && prazo <= limiteAAtrasar)
                .OrderBy(p => p.PrazoFinal)
                .Select(ToDashboardItem)
                .ToList();

            return new DashboardInitialDataDto
            {
                AllProcesses = allProcesses,
                PainelAtrasados = atrasados,
                PainelParaAtrasar = paraAtrasar
            };
        }

        public ProcessoCompletoDTO GetProcessDetails(string processoId)
        {
            if (string.IsNullOrWhiteSpace(processoId))
                throw new InvalidOperationException("ID do processo é obrigatório.");

            return _processoFacadeService.CarregarProcessoCompleto(processoId);
        }

        public AppConfigurationDto GetAppConfiguration()
        {
            return new AppConfigurationDto
            {
                InstitutionDomain = "mppr.mp.br",
                MinProcessNumberLength = 25,
                AllowedProfiles = new[] { "admin", "editor", "leitura" },
                DatabasePath = ConfigService.ObterCaminhoBanco()
            };
        }

        public ProcessReportSummaryDto GenerateProcessReportSummary(string processoId)
        {
            var modelo = _relatorioProcessoService.GerarModelo(processoId);

            return new ProcessReportSummaryDto
            {
                ProcessoId = modelo.Processo.Id,
                NumeroProcesso = modelo.Processo.Numero,
                Paciente = modelo.Processo.Paciente,
                Juiz = modelo.Processo.Juiz,
                Status = modelo.Processo.StatusFase,
                TotalVerificacoes = modelo.Verificacoes.Count,
                TotalDiligencias = modelo.Diligencias.Count,
                TotalContas = modelo.Contas.Count,
                TotalItensSaude = modelo.ItensSaude.Count,
                GeradoEm = DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
            };
        }

        public string GetChangesSummary(IEnumerable<ItemSaude> anteriores, IEnumerable<ItemSaude> atuais)
        {
            return ItemSaudeChangesSummaryService.GerarResumo(anteriores, atuais);
        }

        public void UndoLastVerification(string processoId)
        {
            _verificacaoFacadeService.DesfazerUltimaVerificacaoGeral(processoId);
        }

        public void RecordUserActivity(string userEmail, string userName, string? processId = null, string? processNumero = null, string? processPaciente = null)
        {
            _activeSessionService.RecordUserActivity(userEmail, userName, processId, processNumero, processPaciente);
        }

        public List<ActiveSession> GetRecentUserActivity(int maxMinutes = 20)
        {
            return _activeSessionService.GetRecentUserActivity(maxMinutes);
        }

        private static DashboardProcessItemDto ToDashboardItem(ProcessoResumoCacheItem process)
        {
            return new DashboardProcessItemDto
            {
                Id = process.ProcessoId,
                Numero = process.Numero,
                Paciente = process.Paciente,
                Juiz = process.Juiz,
                StatusCalculado = process.StatusCalculado ?? process.StatusProcesso,
                Prazo = process.PrazoFinal
            };
        }

        private static bool TryParseData(string? valor, out DateTime data)
        {
            data = default;

            if (string.IsNullOrWhiteSpace(valor))
                return false;

            return DateTime.TryParseExact(valor, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out data)
                || DateTime.TryParseExact(valor, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out data)
                || DateTime.TryParse(valor, out data);
        }
    }

    public class DashboardInitialDataDto
    {
        public List<ProcessoResumoCacheItem> AllProcesses { get; set; } = new();
        public List<DashboardProcessItemDto> PainelAtrasados { get; set; } = new();
        public List<DashboardProcessItemDto> PainelParaAtrasar { get; set; } = new();
    }

    public class DashboardProcessItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string Paciente { get; set; } = string.Empty;
        public string Juiz { get; set; } = string.Empty;
        public string? StatusCalculado { get; set; }
        public string? Prazo { get; set; }
    }

    public class AppConfigurationDto
    {
        public string InstitutionDomain { get; set; } = string.Empty;
        public int MinProcessNumberLength { get; set; }
        public string[] AllowedProfiles { get; set; } = Array.Empty<string>();
        public string DatabasePath { get; set; } = string.Empty;
    }

    public class ProcessReportSummaryDto
    {
        public string ProcessoId { get; set; } = string.Empty;
        public string NumeroProcesso { get; set; } = string.Empty;
        public string Paciente { get; set; } = string.Empty;
        public string Juiz { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int TotalVerificacoes { get; set; }
        public int TotalDiligencias { get; set; }
        public int TotalContas { get; set; }
        public int TotalItensSaude { get; set; }
        public string GeradoEm { get; set; } = string.Empty;
    }
}
