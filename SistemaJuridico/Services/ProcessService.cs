using Dapper;
using SistemaJuridico.Models;

namespace SistemaJuridico.Services
{
    public class ProcessService
    {
        private readonly DatabaseService _db;
        private readonly TimeSpan _timeout = TimeSpan.FromMinutes(30);

        public ProcessService(DatabaseService db)
        {
            _db = db;
        }

        // ========================
        // LOCK
        // ========================

        public bool TentarLock(string processoId)
        {
            using var conn = _db.GetConnection();

            var processo = conn.QueryFirstOrDefault<Processo>(
                "SELECT * FROM processos WHERE id = @id",
                new { id = processoId });

            var usuarioAtual = App.Session.UsuarioAtual?.Email;

            if (processo == null || usuarioAtual == null)
                return false;

            if (LockExpirado(processo) ||
                processo.LockUsuario == null ||
                processo.LockUsuario == usuarioAtual)
            {
                conn.Execute("""
                    UPDATE processos
                    SET lock_usuario = @usuario,
                        lock_timestamp = @data
                    WHERE id = @id
                """,
                new
                {
                    usuario = usuarioAtual,
                    data = DateTime.UtcNow.ToString("o"),
                    id = processoId
                });

                return true;
            }

            return false;
        }

        public void RenovarLock(string processoId)
        {
            using var conn = _db.GetConnection();

            conn.Execute("""
                UPDATE processos
                SET lock_timestamp = @data
                WHERE id = @id
            """,
            new
            {
                data = DateTime.UtcNow.ToString("o"),
                id = processoId
            });
        }

        public void LiberarLock(string processoId)
        {
            using var conn = _db.GetConnection();

            conn.Execute("""
                UPDATE processos
                SET lock_usuario = NULL,
                    lock_timestamp = NULL
                WHERE id = @id
            """,
            new { id = processoId });
        }

        public string? UsuarioEditando(string processoId)
        {
            using var conn = _db.GetConnection();

            var processo = conn.QueryFirstOrDefault<Processo>(
                "SELECT * FROM processos WHERE id = @id",
                new { id = processoId });

            if (processo == null)
                return null;

            if (LockExpirado(processo))
            {
                LiberarLock(processoId);
                return null;
            }

            return processo.LockUsuario;
        }

        private bool LockExpirado(Processo p)
        {
            if (string.IsNullOrWhiteSpace(p.LockTimestamp))
                return true;

            if (!DateTime.TryParse(p.LockTimestamp, out var data))
                return true;

            return DateTime.UtcNow - data > _timeout;
        }

        // ========================
        // RASCUNHO
        // ========================

        public void MarcarRascunho(string processoId, string motivo)
        {
            using var conn = _db.GetConnection();

            conn.Execute("""
                UPDATE processos
                SET situacao_rascunho = 'Rascunho',
                    motivo_rascunho = @motivo
                WHERE id = @id
            """,
            new { motivo, id = processoId });
        }

        public void MarcarConcluido(string processoId)
        {
            using var conn = _db.GetConnection();

            conn.Execute("""
                UPDATE processos
                SET situacao_rascunho = 'Concluído',
                    motivo_rascunho = NULL
                WHERE id = @id
            """,
            new { id = processoId });

            LiberarLock(processoId);
        }

        // ========================
        // EXISTENTES (mantidos)
        // ========================

        public List<Processo> ListarProcessos()
        {
            using var conn = _db.GetConnection();
            return conn.Query<Processo>("SELECT * FROM processos").ToList();
        }

        public (decimal saldoPendente, bool diligenciaPendente, string? dataUltLanc)
            ObterResumo(string processoId)
        {
            using var conn = _db.GetConnection();

            var saldo = conn.ExecuteScalar<decimal?>("""
                SELECT SUM(valor_conta)
                FROM contas
                WHERE processo_id = @id
                AND status_conta != 'fechado'
            """, new { id = processoId }) ?? 0;

            var diligencia = conn.ExecuteScalar<int>("""
                SELECT COUNT(*)
                FROM diligencias
                WHERE processo_id = @id
                AND concluida = 0
            """, new { id = processoId }) > 0;

            var data = conn.ExecuteScalar<string>("""
                SELECT MAX(data_movimentacao)
                FROM contas
                WHERE processo_id = @id
            """, new { id = processoId });

            return (saldo, diligencia, data);
        }
    }
}
