using Dapper;
using SistemaJuridico.Models;

namespace SistemaJuridico.Services
{
    public class ProcessoCacheService
    {
        private readonly DatabaseService _db;
        private static readonly object _sync = new();
        private static DateTime _ultimoCarregamentoUtc = DateTime.MinValue;
        private static List<ProcessoResumoCacheItem> _cache = new();
        private static readonly TimeSpan _ttl = TimeSpan.FromMinutes(3);

        public ProcessoCacheService(DatabaseService db)
        {
            _db = db;
        }

        public List<ProcessoResumoCacheItem> ObterCacheLeve()
        {
            lock (_sync)
            {
                if (_cache.Count > 0 && DateTime.UtcNow - _ultimoCarregamentoUtc <= _ttl)
                    return _cache.Select(x => x.Clone()).ToList();
            }

            return AtualizarCache();
        }

        public List<ProcessoResumoCacheItem> AtualizarCache()
        {
            using var conn = _db.GetConnection();

            var itens = conn.Query<ProcessoResumoCacheItem>(@"
SELECT
    p.id as ProcessoId,
    p.numero as Numero,
    p.paciente as Paciente,
    p.representante as Genitor,
    p.juiz as Juiz,
    p.tipo_processo as TipoProcesso,
    p.status_fase as StatusProcesso,
    p.situacao_rascunho as SituacaoRascunho,
    p.motivo_rascunho as MotivoRascunho,
    p.cache_proximo_prazo as PrazoCalculado,
    (
        SELECT v.proximo_prazo_padrao
        FROM verificacoes v
        WHERE v.processo_id = p.id
        ORDER BY v.data_hora DESC
        LIMIT 1
    ) as PrazoVerificacao,
    (
        SELECT v.status_processo
        FROM verificacoes v
        WHERE v.processo_id = p.id
        ORDER BY v.data_hora DESC
        LIMIT 1
    ) as StatusCalculado,
    (
        SELECT v.responsavel
        FROM verificacoes v
        WHERE v.processo_id = p.id
        ORDER BY v.data_hora DESC
        LIMIT 1
    ) as ResponsavelUltimaVerificacao,
    (
        SELECT IFNULL(SUM(c.valor_conta), 0)
        FROM contas c
        WHERE c.processo_id = p.id
    ) as TotalDebito,
    (
        SELECT IFNULL(SUM(c.valor_alvara), 0)
        FROM contas c
        WHERE c.processo_id = p.id
    ) as TotalCredito
FROM processos p
").ToList();

            foreach (var item in itens)
            {
                item.PrazoFinal = !string.IsNullOrWhiteSpace(item.PrazoVerificacao)
                    ? item.PrazoVerificacao
                    : item.PrazoCalculado;
            }

            lock (_sync)
            {
                _cache = itens;
                _ultimoCarregamentoUtc = DateTime.UtcNow;
                return _cache.Select(x => x.Clone()).ToList();
            }
        }
    }

    public class ProcessoResumoCacheItem
    {
        public string ProcessoId { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string Paciente { get; set; } = string.Empty;
        public string? Genitor { get; set; }
        public string Juiz { get; set; } = string.Empty;
        public string? TipoProcesso { get; set; }
        public string StatusProcesso { get; set; } = string.Empty;
        public string? StatusCalculado { get; set; }
        public string? ResponsavelUltimaVerificacao { get; set; }
        public string? PrazoCalculado { get; set; }
        public string? PrazoVerificacao { get; set; }
        public string? PrazoFinal { get; set; }
        public string SituacaoRascunho { get; set; } = "Concluído";
        public string? MotivoRascunho { get; set; }
        public decimal TotalDebito { get; set; }
        public decimal TotalCredito { get; set; }

        public ProcessoResumoCacheItem Clone()
        {
            return (ProcessoResumoCacheItem)MemberwiseClone();
        }
    }
}
