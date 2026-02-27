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

        private static readonly HashSet<string> FinalizedStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Arquivado", "Desistência da Parte", "Cumprimento Extinto"
        };

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

        // ========================
        // DASHBOARD
        // ========================

        /// <summary>
        /// Legacy parity: getInitialDashboardData().
        /// Returns lightweight process list plus pre-calculated atrasados/paraAtrasar panels.
        /// </summary>
        public DashboardInitialDataDto GetInitialDashboardData()
        {
            var allProcesses = _processoCacheService.ObterCacheLeve();
            var hoje = DateTime.Today;

            var atrasados = new List<DashboardProcessItemDto>();
            var paraAtrasar = new List<DashboardProcessItemDto>();

            foreach (var p in allProcesses)
            {
                var status = CalculateProcessDerivedStatus(p, hoje);

                if (status.Text == "Atrasado")
                {
                    atrasados.Add(ToDashboardItem(p, status));
                }
                else if (status.Text.Contains("Vence em") || status.Text == "Vence hoje")
                {
                    paraAtrasar.Add(ToDashboardItem(p, status));
                }
            }

            return new DashboardInitialDataDto
            {
                AllProcesses = allProcesses,
                PainelAtrasados = atrasados,
                PainelParaAtrasar = paraAtrasar
            };
        }

        // ========================
        // PROCESS DETAILS
        // ========================

        /// <summary>
        /// Legacy parity: getProcessDetails(processoId).
        /// Loads full process with verificacoes, contas, and latest item states.
        /// </summary>
        public ProcessoCompletoDTO GetProcessDetails(string processoId)
        {
            if (string.IsNullOrWhiteSpace(processoId))
                throw new InvalidOperationException("ID do processo é obrigatório.");

            return _processoFacadeService.CarregarProcessoCompleto(processoId);
        }

        // ========================
        // PROCESS CRUD
        // ========================

        /// <summary>
        /// Legacy parity: createProcessoInSheet(payload).
        /// Creates a new process and its initial verification in a single operation.
        /// </summary>
        public ProcessoCompletoDTO CreateProcesso(Processo processo, string statusInicial, List<ItemSaude> itens)
        {
            if (processo == null) throw new InvalidOperationException("Dados do processo são obrigatórios.");
            if (string.IsNullOrWhiteSpace(processo.Numero) || processo.Numero.Trim().Length < 15)
                throw new InvalidOperationException("Número do processo é obrigatório e deve ter no mínimo 15 caracteres.");
            if (string.IsNullOrWhiteSpace(processo.Paciente))
                throw new InvalidOperationException("Nome do paciente é obrigatório.");
            if (string.IsNullOrWhiteSpace(processo.Juiz))
                throw new InvalidOperationException("Nome do Juiz(a) é obrigatório.");

            processo.Id = Guid.NewGuid().ToString();
            processo.StatusFase = statusInicial ?? "Não iniciado";
            processo.UltimaAtualizacao = DateTime.Now.ToString("o");

            ServiceLocator.ProcessService.CriarProcesso(processo);

            if (itens != null && itens.Count > 0)
            {
                foreach (var item in itens)
                {
                    item.ProcessoId = processo.Id;
                }
                ServiceLocator.ItemSaudeService.SubstituirItensProcesso(processo.Id, itens);
            }

            var responsavel = App.Session?.UsuarioAtual?.Nome ?? "Sistema";

            _verificacaoFacadeService.CriarVerificacao(
                processo.Id,
                statusInicial ?? "Não iniciado",
                responsavel,
                "Registro inicial do processo.",
                itens ?? new List<ItemSaude>());

            ServiceLocator.HistoricoService.Registrar(processo.Id, "Processo Criado", processo.Numero);
            _processoCacheService.AtualizarCache();

            return GetProcessDetails(processo.Id);
        }

        /// <summary>
        /// Legacy parity: updateProcessoInSheet(payload).
        /// Updates process data and creates a tracking verification for the changes.
        /// </summary>
        public ProcessoCompletoDTO UpdateProcesso(Processo processo, string? statusInicial, List<ItemSaude> itens)
        {
            if (processo == null || string.IsNullOrWhiteSpace(processo.Id))
                throw new InvalidOperationException("ID do processo inválido para atualização.");

            var itensAnteriores = ServiceLocator.ItemSaudeService.ListarPorProcesso(processo.Id);
            processo.UltimaAtualizacao = DateTime.Now.ToString("o");
            ServiceLocator.ProcessService.AtualizarProcesso(processo);

            if (itens != null)
            {
                foreach (var item in itens)
                    item.ProcessoId = processo.Id;
                ServiceLocator.ItemSaudeService.SubstituirItensProcesso(processo.Id, itens);
            }

            if (!string.IsNullOrWhiteSpace(statusInicial))
                ServiceLocator.ProcessService.AtualizarStatus(processo.Id, statusInicial);

            var responsavel = App.Session?.UsuarioAtual?.Nome ?? "Sistema";
            var alteracoes = ItemSaudeChangesSummaryService.GerarResumo(itensAnteriores, itens ?? new List<ItemSaude>());

            _verificacaoFacadeService.CriarVerificacao(
                processo.Id,
                statusInicial ?? processo.StatusFase,
                responsavel,
                $"Cadastro do processo atualizado. {alteracoes}",
                itens ?? new List<ItemSaude>());

            ServiceLocator.HistoricoService.Registrar(processo.Id, "Cadastro do Processo Alterado", alteracoes);
            _processoCacheService.AtualizarCache();

            return GetProcessDetails(processo.Id);
        }

        /// <summary>
        /// Legacy parity: deleteProcessoInSheet(idToDelete).
        /// Deletes process and all related data (verificacoes, contas, itens).
        /// </summary>
        public string DeleteProcesso(string processoId)
        {
            if (string.IsNullOrWhiteSpace(processoId))
                throw new InvalidOperationException("ID do processo é obrigatório para exclusão.");

            ServiceLocator.ProcessService.ExcluirProcesso(processoId);
            ServiceLocator.HistoricoService.Registrar(processoId, "Processo Excluído (Total)", processoId);
            _processoCacheService.AtualizarCache();

            return $"Processo com ID {processoId} e todos os dados relacionados excluídos com sucesso.";
        }

        // ========================
        // VERIFICAÇÕES
        // ========================

        /// <summary>
        /// Legacy parity: salvarNovaVerificacao(processoId, payload).
        /// Creates a new verification with all fields from the verification form.
        /// </summary>
        public ProcessoCompletoDTO SalvarNovaVerificacao(
            string processoId,
            string statusProcesso,
            string diligenciaStatus,
            string descricao,
            string pendencias,
            string? prazoDiligencia,
            List<ItemSaude> itensSnapshot)
        {
            if (string.IsNullOrWhiteSpace(processoId))
                throw new InvalidOperationException("ID do processo não fornecido para salvar verificação.");

            var responsavel = App.Session?.UsuarioAtual?.Nome ?? "Sistema";
            var todayStr = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var (proximoPrazo, dataNotificacao) = VerificacaoFacadeService.CalcularPrazosVerificacao(
                DateTime.Today, prazoDiligencia);

            _verificacaoFacadeService.CriarVerificacaoCompleta(
                processoId,
                statusProcesso,
                responsavel,
                descricao,
                diligenciaRealizada: diligenciaStatus == "Concluída",
                descricaoDiligencia: descricao,
                possuiPendencias: !string.IsNullOrWhiteSpace(pendencias),
                descricaoPendencias: pendencias,
                prazoDiligencia: prazoDiligencia ?? string.Empty,
                proximoPrazoPadrao: proximoPrazo,
                dataNotificacao: dataNotificacao,
                diligenciaStatus: diligenciaStatus,
                descricaoPersistente: string.Empty,
                itensSnapshot: itensSnapshot ?? new List<ItemSaude>());

            _processoCacheService.AtualizarCache();
            return GetProcessDetails(processoId);
        }

        /// <summary>
        /// Legacy parity: undoLastVerification(processId).
        /// </summary>
        public ProcessoCompletoDTO UndoLastVerification(string processoId)
        {
            _verificacaoFacadeService.DesfazerUltimaVerificacaoGeral(processoId);
            _processoCacheService.AtualizarCache();
            return GetProcessDetails(processoId);
        }

        // ========================
        // CONTAS (Prestação de Contas)
        // ========================

        /// <summary>
        /// Legacy parity: salvarLoteDeContas(processoId, contasParaSalvar).
        /// Saves a batch of account entries for a process.
        /// </summary>
        public ProcessoCompletoDTO SalvarLoteDeContas(string processoId, List<Conta> contas)
        {
            if (string.IsNullOrWhiteSpace(processoId) || contas == null || contas.Count == 0)
                throw new InvalidOperationException("Dados inválidos para salvar o lote de contas.");

            var responsavel = App.Session?.UsuarioAtual?.Nome ?? "Sistema";

            foreach (var conta in contas)
            {
                conta.Id = Guid.NewGuid().ToString();
                conta.ProcessoId = processoId;
                conta.Responsavel = responsavel;
                _processoFacadeService.InserirConta(conta);
            }

            ServiceLocator.HistoricoService.Registrar(
                processoId,
                "Lote de Prestações de Contas Salvo",
                $"Total: {contas.Count} lançamentos");

            _processoCacheService.AtualizarCache();
            return GetProcessDetails(processoId);
        }

        /// <summary>
        /// Legacy parity: updateIndividualConta(processId, contaId, updatedContaData).
        /// </summary>
        public ProcessoCompletoDTO UpdateIndividualConta(string processoId, string contaId, Conta contaAtualizada)
        {
            if (string.IsNullOrWhiteSpace(contaId))
                throw new InvalidOperationException("ID do lançamento contábil não encontrado para atualização.");

            contaAtualizada.Id = contaId;
            contaAtualizada.ProcessoId = processoId;
            _processoFacadeService.AtualizarConta(contaAtualizada);

            _processoCacheService.AtualizarCache();
            return GetProcessDetails(processoId);
        }

        /// <summary>
        /// Legacy parity: deleteIndividualConta(processId, contaId).
        /// </summary>
        public ProcessoCompletoDTO DeleteIndividualConta(string processoId, string contaId)
        {
            if (string.IsNullOrWhiteSpace(contaId))
                throw new InvalidOperationException("ID do lançamento contábil não encontrado para exclusão.");

            _processoFacadeService.ExcluirConta(contaId, processoId);

            _processoCacheService.AtualizarCache();
            return GetProcessDetails(processoId);
        }

        // ========================
        // CONFIGURAÇÃO
        // ========================

        /// <summary>
        /// Legacy parity: getAppConfiguration().
        /// Returns all config lists needed by the frontend.
        /// </summary>
        public AppConfigurationDto GetAppConfiguration()
        {
            return new AppConfigurationDto
            {
                InstitutionDomain = "mppr.mp.br",
                MinProcessNumberLength = 15,
                AllowedProfiles = new[] { "admin", "editor", "leitura" },
                DatabasePath = ConfigService.ObterCaminhoBanco(),
                TiposDeProcesso = new[]
                {
                    "Ação Coletiva", "Mandado de Segurança", "Indenizatória",
                    "Tributária", "Previdenciária", "Cível (Geral)", "Saúde"
                },
                StatusDeProcesso = new[]
                {
                    "Cumprimento de Sentença", "Cumprimento Provisório de Sentença",
                    "Conhecimento", "Recurso Inominado", "Apelação", "Agravo",
                    "Suspenso", "Arquivado", "Cumprimento Extinto", "Desistência da Parte"
                },
                StatusInicialDeProcesso = new[]
                {
                    "Não iniciado", "Cumprimento de Sentença", "Cumprimento Provisório de Sentença",
                    "Conhecimento", "Recurso Inominado", "Apelação", "Agravo",
                    "Suspenso", "Arquivado", "Cumprimento Extinto", "Desistência da Parte"
                },
                Diligencias = new[] { "Concluída", "Pendente", "Infrutífera" },
                TiposDeTerapia = new[]
                {
                    "Consulta neuro", "Equoterapia", "Fisioterapia",
                    "Fonoaudiologia ABA", "Fonoaudiologia", "Musicoterapia",
                    "Neurofuncional", "Psicologia/Intervenção ABA",
                    "Psicologia/Intervenção DENVER", "Psicomotricidade",
                    "Psicopedagogia", "Psicoterapia", "Terapia Ocupacional",
                    "Terapia Ocupacional com integração sensorial",
                    "Terapia Cognitivo Comportamental"
                },
                StatusDeCirurgia = new[] { "Agendada", "Realizada", "Cancelada" }
            };
        }

        // ========================
        // RELATÓRIOS
        // ========================

        /// <summary>
        /// Legacy parity: generateProcessReportSummary() (global, not per-process).
        /// Returns counts by status and by type across all processes.
        /// </summary>
        public GlobalProcessReportSummaryDto GenerateGlobalProcessReportSummary()
        {
            var allProcesses = _processoCacheService.ObterCacheLeve();
            var byStatus = new Dictionary<string, int>();
            var byType = new Dictionary<string, int>();

            foreach (var p in allProcesses)
            {
                var status = p.StatusCalculado ?? p.StatusProcesso ?? "Não Iniciado";
                var type = p.TipoProcesso ?? "Desconhecido";

                byStatus[status] = byStatus.TryGetValue(status, out var sc) ? sc + 1 : 1;
                byType[type] = byType.TryGetValue(type, out var tc) ? tc + 1 : 1;
            }

            return new GlobalProcessReportSummaryDto
            {
                TotalProcesses = allProcesses.Count,
                ByStatus = byStatus,
                ByType = byType
            };
        }

        /// <summary>
        /// Per-process report summary (kept for backwards compatibility).
        /// </summary>
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

        // ========================
        // CHANGES SUMMARY
        // ========================

        /// <summary>
        /// Legacy parity: getChangesSummary().
        /// </summary>
        public string GetChangesSummary(IEnumerable<ItemSaude> anteriores, IEnumerable<ItemSaude> atuais)
        {
            return ItemSaudeChangesSummaryService.GerarResumo(anteriores, atuais);
        }

        // ========================
        // USER ACTIVITY / SESSIONS
        // ========================

        public void RecordUserActivity(string userEmail, string userName, string? processId = null, string? processNumero = null, string? processPaciente = null)
        {
            _activeSessionService.RecordUserActivity(userEmail, userName, processId, processNumero, processPaciente);
        }

        public List<ActiveSession> GetRecentUserActivity(int maxMinutes = 20)
        {
            return _activeSessionService.GetRecentUserActivity(maxMinutes);
        }

        // ========================
        // USER MANAGEMENT
        // ========================

        /// <summary>
        /// Legacy parity: getAllUsers().
        /// </summary>
        public List<Usuario> GetAllUsers()
        {
            return UserManagementService.GetAllUsers();
        }

        /// <summary>
        /// Legacy parity: registerNewUser(email, name).
        /// </summary>
        public Usuario RegisterNewUser(string email, string name)
        {
            return UserManagementService.RegisterNewUser(email, name);
        }

        /// <summary>
        /// Legacy parity: toggleUserAdminStatus(userId, newIsAdminStatus).
        /// </summary>
        public Usuario ToggleUserAdminStatus(string userId, bool newIsAdminStatus)
        {
            return UserManagementService.ToggleUserAdminStatus(userId, newIsAdminStatus);
        }

        /// <summary>
        /// Legacy parity: deleteUser(userId).
        /// </summary>
        public string DeleteUser(string userId)
        {
            return UserManagementService.DeleteUser(userId);
        }

        // ========================
        // PRIVATE: Status Calculation
        // ========================

        /// <summary>
        /// C# port of the legacy _calculateProcessDerivedData() function.
        /// Computes situacao (text, color, icon), prazoTipo, prazoData, prazoCor.
        /// </summary>
        internal static CalculatedStatusDto CalculateProcessDerivedStatus(ProcessoResumoCacheItem processo, DateTime today)
        {
            var status = new CalculatedStatusDto();
            var statusProcesso = processo.StatusCalculado ?? processo.StatusProcesso;

            if (FinalizedStatuses.Contains(statusProcesso ?? string.Empty))
            {
                status.Text = statusProcesso ?? "Tratamentos Concluídos";
                status.Color = "text-gray-600";
                status.Icon = "fa-archive";
                return status;
            }

            string? prazoString = processo.PrazoFinal;
            string prazoTipo = string.Empty;
            string prazoCor = string.Empty;

            // Determine which deadline type is active
            if (!string.IsNullOrWhiteSpace(processo.PrazoVerificacao))
            {
                // Check if this came from prazoDiligencia or proximoPrazoPadrao
                // The cache stores the final calculated prazo; we check PrazoCalculado vs PrazoVerificacao
                if (!string.IsNullOrWhiteSpace(processo.PrazoCalculado) &&
                    processo.PrazoCalculado != processo.PrazoVerificacao)
                {
                    prazoTipo = "Prazo Diligência";
                    prazoCor = "font-bold text-emerald-600";
                }
                else
                {
                    prazoTipo = "Próx. Prazo Padrão";
                    prazoCor = "font-bold text-blue-600";
                }
            }

            if (string.IsNullOrWhiteSpace(prazoString) || !TryParseData(prazoString, out var prazoDate))
            {
                return status; // Default: "Não iniciado / Prazo Indefinido"
            }

            var diffDays = (int)Math.Ceiling((prazoDate.Date - today.Date).TotalDays);

            if (diffDays < 0)
            {
                status.Text = "Atrasado";
                status.Color = "text-red-500";
                status.Icon = "fa-times-circle";
                prazoCor = prazoTipo == "Prazo Diligência"
                    ? "font-bold text-violet-800"
                    : "font-bold text-red-600";
            }
            else if (diffDays == 0)
            {
                status.Text = "Vence hoje";
                status.Color = "text-yellow-500";
                status.Icon = "fa-exclamation-triangle";
            }
            else if (diffDays <= 7)
            {
                status.Text = $"Vence em {diffDays} dia(s)";
                status.Color = "text-yellow-500";
                status.Icon = "fa-exclamation-triangle";
            }
            else
            {
                status.Text = "Em dia";
                status.Color = "text-green-500";
                status.Icon = "fa-check-circle";
            }

            status.PrazoData = prazoDate.ToString("dd/MM/yyyy");
            status.PrazoTipo = prazoTipo;
            status.PrazoCor = prazoCor;

            return status;
        }

        private static DashboardProcessItemDto ToDashboardItem(ProcessoResumoCacheItem process, CalculatedStatusDto status)
        {
            return new DashboardProcessItemDto
            {
                Id = process.ProcessoId,
                Numero = process.Numero,
                Paciente = process.Paciente,
                Juiz = process.Juiz,
                StatusCalculado = status.Text,
                Prazo = process.PrazoFinal,
                PrazoTipo = status.PrazoTipo ?? "N/A",
                PrazoData = status.PrazoData ?? "N/A",
                PrazoCor = status.PrazoCor ?? string.Empty
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

    // ========================
    // DTOs
    // ========================

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
        public string? PrazoTipo { get; set; }
        public string? PrazoData { get; set; }
        public string? PrazoCor { get; set; }
    }

    /// <summary>
    /// Mirrors the legacy calculatedStatus object from _calculateProcessDerivedData().
    /// </summary>
    public class CalculatedStatusDto
    {
        public string Text { get; set; } = "Não iniciado / Prazo Indefinido";
        public string Color { get; set; } = "text-gray-950";
        public string Icon { get; set; } = "";
        public string? PrazoData { get; set; }
        public string? PrazoTipo { get; set; }
        public string? PrazoCor { get; set; }
    }

    public class AppConfigurationDto
    {
        public string InstitutionDomain { get; set; } = string.Empty;
        public int MinProcessNumberLength { get; set; }
        public string[] AllowedProfiles { get; set; } = Array.Empty<string>();
        public string DatabasePath { get; set; } = string.Empty;

        // Legacy parity: exact config lists from codigo.gs getAppConfiguration()
        public string[] TiposDeProcesso { get; set; } = Array.Empty<string>();
        public string[] StatusDeProcesso { get; set; } = Array.Empty<string>();
        public string[] StatusInicialDeProcesso { get; set; } = Array.Empty<string>();
        public string[] Diligencias { get; set; } = Array.Empty<string>();
        public string[] TiposDeTerapia { get; set; } = Array.Empty<string>();
        public string[] StatusDeCirurgia { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Legacy parity: generateProcessReportSummary() global result.
    /// </summary>
    public class GlobalProcessReportSummaryDto
    {
        public int TotalProcesses { get; set; }
        public Dictionary<string, int> ByStatus { get; set; } = new();
        public Dictionary<string, int> ByType { get; set; } = new();
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
