using Dapper;
using SistemaJuridico.Models;

namespace SistemaJuridico.Services
{
    public class ProcessService
    {
        private readonly DatabaseService _db;

        public ProcessService(DatabaseService db)
        {
            _db = db;
        }

        // =========================
        // LISTAR PROCESSOS
        // =========================

        public List<Processo> ListarProcessos()
        {
            using var conn = _db.GetConnection();

            return conn.Query<Processo>(
                "SELECT * FROM processos ORDER BY numero"
            ).ToList();
        }

        // =========================
        // RESUMO PARA DASHBOARD
        // =========================

        public (decimal saldoPendente, bool diligenciaPendente, string? dataUltLanc)
            ObterResumo(string processoId)
        {
            using var conn = _db.GetConnection();

            decimal saldo = conn.ExecuteScalar<decimal>(@"
                SELECT IFNULL(SUM(valor_conta),0)
                FROM contas
                WHERE processo_id=@id
                AND status_conta='rascunho'
            ", new { id = processoId });

            bool diligencia = conn.ExecuteScalar<int>(@"
                SELECT COUNT(*)
                FROM verificacoes
                WHERE processo_id=@id
                AND diligencia_pendente=1
            ", new { id = processoId }) > 0;

            string? data = conn.ExecuteScalar<string?>(@"
                SELECT data_movimentacao
                FROM contas
                WHERE processo_id=@id
                ORDER BY data_movimentacao DESC
                LIMIT 1
            ", new { id = processoId });

            return (saldo, diligencia, data);
        }

        // =========================
        // SALVAR RASCUNHO
        // =========================

        public void MarcarRascunho(string processoId, string motivo)
        {
            using var conn = _db.GetConnection();

            conn.Execute(@"
                UPDATE processos
                SET SituacaoRascunho='Rascunho',
                    MotivoRascunho=@m,
                    UsuarioRascunho=@u
                WHERE id=@id
            ",
            new
            {
                id = processoId,
                m = motivo,
                u = App.Session.UsuarioAtual?.Email
            });
        }

        public void MarcarConcluido(string processoId)
        {
            using var conn = _db.GetConnection();

            conn.Execute(@"
                UPDATE processos
                SET SituacaoRascunho='Concluído',
                    MotivoRascunho=NULL,
                    UsuarioRascunho=NULL
                WHERE id=@id
            ", new { id = processoId });
        }

        // =========================
        // LOCK MULTIUSUÁRIO
        // =========================

        public bool TentarLock(string processoId)
        {
            using var conn = _db.GetConnection();

            var usuarioAtual = App.Session.UsuarioAtual?.Email;

            var usuarioLock = conn.ExecuteScalar<string?>(@"
                SELECT UsuarioRascunho
                FROM processos
                WHERE id=@id
            ", new { id = processoId });

            if (string.IsNullOrEmpty(usuarioLock))
            {
                conn.Execute(@"
                    UPDATE processos
                    SET UsuarioRascunho=@u
                    WHERE id=@id
                ",
                new { id = processoId, u = usuarioAtual });

                return true;
            }

            return usuarioLock == usuarioAtual;
        }

        public void LiberarLock(string processoId)
        {
            using var conn = _db.GetConnection();

            conn.Execute(@"
                UPDATE processos
                SET UsuarioRascunho=NULL
                WHERE id=@id
            ", new { id = processoId });
        }

        // =========================
        // STATUS DE EDIÇÃO
        // =========================

        public string? UsuarioEditando(string processoId)
        {
            using var conn = _db.GetConnection();

            return conn.ExecuteScalar<string?>(@"
                SELECT UsuarioRascunho
                FROM processos
                WHERE id=@id
            ", new { id = processoId });
        }
    }
}
