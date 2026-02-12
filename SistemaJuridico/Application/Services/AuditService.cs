using Dapper;
using SistemaJuridico.Models;
using System.Threading.Tasks;

namespace SistemaJuridico.Services
{
    public class AuditService
    {
        private readonly DatabaseService _db;

        public AuditService(DatabaseService db)
        {
            _db = db;
        }

        public void Registrar(
            string acao,
            string entidade,
            string? entidadeId,
            string? detalhes = null)
        {
            try
            {
                using var conn = _db.GetConnection();

                conn.Execute(@"
INSERT INTO audit_log (
    id,
    data_hora,
    usuario,
    acao,
    entidade,
    entidade_id,
    detalhes
)
VALUES (
    @id,
    @data,
    @usuario,
    @acao,
    @entidade,
    @entidadeId,
    @detalhes
)",
                new
                {
                    id = Guid.NewGuid().ToString(),
                    data = DateTime.UtcNow.ToString("o"),
                    usuario = App.Session.UsuarioAtual?.Email ?? "Sistema",
                    acao,
                    entidade,
                    entidadeId,
                    detalhes
                });
            }
            catch
            {
                // auditoria nunca pode quebrar sistema
            }
        }

        public Task<List<AuditLog>> ObterLogsAsync(string? processoId = null)
        {
            if (string.IsNullOrWhiteSpace(processoId))
            {
                using var conn = _db.GetConnection();

                var logs = conn.Query<AuditLog>(@"
SELECT
    id as Id,
    data_hora as DataHora,
    usuario as Usuario,
    acao as Acao,
    entidade as Entidade,
    entidade_id as EntidadeId,
    detalhes as Detalhes
FROM audit_log
ORDER BY data_hora DESC").ToList();

                return Task.FromResult(logs);
            }

            return Task.FromResult(ListarPorProcesso(processoId));
        }

        public Task<List<AuditLog>> ObterLogsAsync(
            DateTime? dataInicial,
            DateTime? dataFinal,
            string? usuario,
            string? processoId)
        {
            using var conn = _db.GetConnection();

            var sql = @"
SELECT
    id as Id,
    data_hora as DataHora,
    usuario as Usuario,
    acao as Acao,
    entidade as Entidade,
    entidade_id as EntidadeId,
    detalhes as Detalhes
FROM audit_log
WHERE (@dataInicial IS NULL OR data_hora >= @dataInicial)
  AND (@dataFinal IS NULL OR data_hora <= @dataFinal)
  AND (@usuario IS NULL OR @usuario = '' OR lower(usuario) LIKE '%' || lower(@usuario) || '%')
  AND (@processoId IS NULL OR entidade_id = @processoIdTexto)
ORDER BY data_hora DESC";

            var logs = conn.Query<AuditLog>(sql, new
            {
                dataInicial = dataInicial?.ToString("o"),
                dataFinal = dataFinal?.ToString("o"),
                usuario,
                processoId,
                processoIdTexto = processoId
            }).ToList();

            return Task.FromResult(logs);
        }

        public List<AuditLog> ListarPorProcesso(string processoId)
        {
            using var conn = _db.GetConnection();

            return conn.Query<AuditLog>(@"
SELECT
    id as Id,
    data_hora as DataHora,
    usuario as Usuario,
    acao as Acao,
    entidade as Entidade,
    entidade_id as EntidadeId,
    detalhes as Detalhes
FROM audit_log
WHERE entidade_id = @processoId
ORDER BY data_hora DESC
", new { processoId }).ToList();
        }
    }
}
