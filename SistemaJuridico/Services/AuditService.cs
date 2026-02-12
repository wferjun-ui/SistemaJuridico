using System.Threading.Tasks;
using Dapper;
using SistemaJuridico.Models;

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
@@ -37,38 +38,45 @@ VALUES (
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

        // ⭐ NOVO MÉTODO — BLOCO 9
        public Task<List<AuditLog>> ObterLogsAsync(string? processoId = null)
        {
            if (string.IsNullOrWhiteSpace(processoId))
                return Task.FromResult(new List<AuditLog>());
            return Task.FromResult(ListarPorProcesso(processoId));
        }

        public List<AuditLog> ListarPorProcesso(string processoId)
        {
            using var conn = _db.GetConnection();

            return conn.Query<AuditLog>(@"
SELECT *
FROM audit_log
WHERE entidade_id = @processoId
ORDER BY data_hora DESC
", new { processoId }).ToList();
        }
    }
}
