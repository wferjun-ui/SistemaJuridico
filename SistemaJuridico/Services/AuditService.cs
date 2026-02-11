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

        // ⭐ NOVO MÉTODO — BLOCO 9
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
