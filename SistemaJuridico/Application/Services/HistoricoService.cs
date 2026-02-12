using Dapper;
using SistemaJuridico.Models;

namespace SistemaJuridico.Services
{
    public class HistoricoService
    {
        private readonly DatabaseService _db;

        public HistoricoService(DatabaseService db)
        {
            _db = db;
        }

        // =========================
        // REGISTRAR EVENTO
        // =========================

        public void Registrar(
            string processoId,
            string acao,
            string? detalhes = null)
        {
            using var conn = _db.GetConnection();

            var historico = new Historico
            {
                ProcessoId = processoId,
                Acao = acao,
                Usuario = App.Session.UsuarioAtual?.Email ?? "Sistema",
                Detalhes = detalhes
            };

            conn.Execute(@"
                INSERT INTO historico (
                    id,
                    processo_id,
                    acao,
                    usuario,
                    data_hora,
                    detalhes
                )
                VALUES (
                    @Id,
                    @ProcessoId,
                    @Acao,
                    @Usuario,
                    @DataHora,
                    @Detalhes
                )
            ", historico);
        }

        // =========================
        // LISTAR HISTÓRICO DO PROCESSO
        // =========================

        public List<Historico> ListarPorProcesso(string processoId)
        {
            using var conn = _db.GetConnection();

            return conn.Query<Historico>(@"
                SELECT
                    id as Id,
                    processo_id as ProcessoId,
                    acao as Acao,
                    usuario as Usuario,
                    data_hora as DataHora,
                    detalhes as Detalhes
                FROM historico
                WHERE processo_id=@id
                ORDER BY data_hora DESC
            ", new { id = processoId }).ToList();
        }
    }
}
