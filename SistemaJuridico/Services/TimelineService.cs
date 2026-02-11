using SistemaJuridico.Models;

namespace SistemaJuridico.Services
{
    public class TimelineService
    {
        private readonly HistoricoService _historicoService;
        private readonly VerificacaoService _verificacaoService;
        private readonly DiligenciaService _diligenciaService;
        private readonly ContaService _contaService;
        private readonly AuditService _auditService;

        public TimelineService(
            HistoricoService historicoService,
            VerificacaoService verificacaoService,
            DiligenciaService diligenciaService,
            ContaService contaService,
            AuditService auditService)
        {
            _historicoService = historicoService;
            _verificacaoService = verificacaoService;
            _diligenciaService = diligenciaService;
            _contaService = contaService;
            _auditService = auditService;
        }

        public List<TimelineEventoDTO> ObterTimeline(string processoId)
        {
            var eventos = new List<TimelineEventoDTO>();

            // =========================
            // HISTÓRICO
            // =========================

            var historico = _historicoService.ListarPorProcesso(processoId);

            eventos.AddRange(
                historico.Select(h => new TimelineEventoDTO
                {
                    Tipo = "Histórico",
                    Titulo = h.Acao,
                    Descricao = h.Detalhes,
                    DataHora = DateTime.Parse(h.DataHora),
                    Usuario = h.Usuario
                }));

            // =========================
            // VERIFICAÇÕES
            // =========================

            var verificacoes = _verificacaoService.ListarPorProcesso(processoId);

            eventos.AddRange(
                verificacoes.Select(v => new TimelineEventoDTO
                {
                    Tipo = "Verificação",
                    Titulo = v.Status,
                    Descricao = v.Descricao,
                    DataHora = DateTime.Parse(v.DataHora),
                    Usuario = v.Responsavel,
                    ReferenciaId = v.Id
                }));

            // =========================
            // DILIGÊNCIAS
            // =========================

            var diligencias = _diligenciaService.ListarPorProcesso(processoId);

            eventos.AddRange(
                diligencias.Select(d => new TimelineEventoDTO
                {
                    Tipo = "Diligência",
                    Titulo = d.Descricao,
                    Descricao = d.Concluida ? "Concluída" : "Pendente",
                    DataHora = DateTime.Parse(d.DataCriacao),
                    ReferenciaId = d.Id
                }));

            // =========================
            // CONTAS
            // =========================

            var contas = _contaService.ListarPorProcesso(processoId);

            eventos.AddRange(
                contas.Select(c => new TimelineEventoDTO
                {
                    Tipo = "Conta",
                    Titulo = $"Conta: {c.ValorConta:C}",
                    Descricao = c.Status,
                    DataHora = DateTime.Parse(c.DataLancamento),
                    ReferenciaId = c.Id
                }));

            // =========================
            // AUDITORIA
            // =========================

            var audit = _auditService.ListarPorProcesso(processoId);

            eventos.AddRange(
                audit.Select(a => new TimelineEventoDTO
                {
                    Tipo = "Auditoria",
                    Titulo = a.Acao,
                    Descricao = a.Detalhes,
                    DataHora = DateTime.Parse(a.DataHora),
                    Usuario = a.Usuario
                }));

            return eventos
                .OrderByDescending(x => x.DataHora)
                .ToList();
        }
    }
}
