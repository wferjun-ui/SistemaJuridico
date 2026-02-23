using Dapper;
using SistemaJuridico.Models;
using System.Globalization;

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

            var usuarioAtual = App.Session.UsuarioAtual?.Email;
            if (string.IsNullOrWhiteSpace(usuarioAtual))
                return;

            conn.Execute("""
                UPDATE processos
                SET lock_timestamp = @data
                WHERE id = @id
                  AND lock_usuario = @usuario
            """,
            new
            {
                data = DateTime.UtcNow.ToString("o"),
                id = processoId,
                usuario = usuarioAtual
            });
        }

        public void LiberarLock(string processoId, bool forcar = false)
        {
            using var conn = _db.GetConnection();

            if (forcar)
            {
                conn.Execute("""
                    UPDATE processos
                    SET lock_usuario = NULL,
                        lock_timestamp = NULL
                    WHERE id = @id
                """,
                new { id = processoId });

                return;
            }

            var usuarioAtual = App.Session.UsuarioAtual?.Email;
            if (string.IsNullOrWhiteSpace(usuarioAtual))
                return;

            conn.Execute("""
                UPDATE processos
                SET lock_usuario = NULL,
                    lock_timestamp = NULL
                WHERE id = @id
                  AND lock_usuario = @usuario
            """,
            new { id = processoId, usuario = usuarioAtual });
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
                LiberarLock(processoId, forcar: true);
                return null;
            }

            return processo.LockUsuario;
        }

        private bool LockExpirado(Processo p)
        {
            if (string.IsNullOrWhiteSpace(p.LockTimestamp))
                return true;

            if (!DateTimeOffset.TryParse(
                    p.LockTimestamp,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal,
                    out var data))
                return true;

            return DateTimeOffset.UtcNow - data > _timeout;
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

        public void CriarProcesso(Processo processo)
        {
            using var conn = _db.GetConnection();
            conn.Execute("""
                INSERT INTO processos (
                    id, numero, paciente, representante, sem_representante, juiz, tipo_processo, classificacao, status_fase, ultima_atualizacao
                ) VALUES (
                    @Id, @Numero, @Paciente, @Representante, @SemRepresentante, @Juiz, @TipoProcesso, @Classificacao, @StatusFase, @UltimaAtualizacao
                )
                """, processo);
        }


        public void SubstituirReus(string processoId, List<string> reus)
        {
            using var conn = _db.GetConnection();

            conn.Execute("DELETE FROM processo_reus WHERE processo_id = @ProcessoId", new { ProcessoId = processoId });

            foreach (var reu in reus.Where(r => !string.IsNullOrWhiteSpace(r)))
            {
                conn.Execute("""
                    INSERT INTO processo_reus (id, processo_id, nome_reu)
                    VALUES (@Id, @ProcessoId, @NomeReu)
                """,
                new
                {
                    Id = Guid.NewGuid().ToString(),
                    ProcessoId = processoId,
                    NomeReu = reu.Trim()
                });
            }
        }

        public void AtualizarStatus(string processoId, string status)
        {
            using var conn = _db.GetConnection();
            conn.Execute("UPDATE processos SET status_fase=@status WHERE id=@processoId", new { processoId, status });
        }

        public void AtualizarProcesso(Processo processo)
        {
            using var conn = _db.GetConnection();
            conn.Execute("""
                UPDATE processos
                SET numero = @Numero,
                    paciente = @Paciente,
                    representante = @Representante,
                    sem_representante = @SemRepresentante,
                    juiz = @Juiz,
                    tipo_processo = @TipoProcesso,
                    classificacao = @Classificacao,
                    status_fase = @StatusFase,
                    observacao_fixa = @ObservacaoFixa,
                    ultima_atualizacao = @UltimaAtualizacao
                WHERE id = @Id
            """, new
            {
                processo.Id,
                processo.Numero,
                processo.Paciente,
                processo.Representante,
                processo.SemRepresentante,
                processo.Juiz,
                processo.TipoProcesso,
                processo.Classificacao,
                processo.StatusFase,
                processo.ObservacaoFixa,
                UltimaAtualizacao = DateTime.Now.ToString("o")
            });
        }

        public void ExcluirProcesso(string processoId)
        {
            using var conn = _db.GetConnection();

            conn.Execute("DELETE FROM contas WHERE processo_id = @id", new { id = processoId });
            conn.Execute("DELETE FROM diligencias WHERE processo_id = @id", new { id = processoId });
            conn.Execute("DELETE FROM verificacoes WHERE processo_id = @id", new { id = processoId });
            conn.Execute("DELETE FROM itens_saude WHERE processo_id = @id", new { id = processoId });
            conn.Execute("DELETE FROM historico WHERE processo_id = @id", new { id = processoId });
            conn.Execute("DELETE FROM processo_reus WHERE processo_id = @id", new { id = processoId });
            conn.Execute("DELETE FROM processos WHERE id = @id", new { id = processoId });
        }

        // ========================
        // EXISTENTES (mantidos)
        // ========================

        public List<Processo> ListarProcessos()
        {
            using var conn = _db.GetConnection();
            return conn.Query<Processo>("SELECT * FROM processos").ToList();
        }

        public List<string> ListarValoresDistintosCadastro(string campo)
        {
            var coluna = campo?.Trim().ToLowerInvariant() switch
            {
                "numero" => "numero",
                "paciente" => "paciente",
                "representante" => "representante",
                "juiz" => "juiz",
                _ => string.Empty
            };

            if (string.IsNullOrWhiteSpace(coluna))
                return new List<string>();

            using var conn = _db.GetConnection();
            return conn.Query<string>($"""
                SELECT DISTINCT TRIM({coluna})
                FROM processos
                WHERE {coluna} IS NOT NULL
                  AND TRIM({coluna}) <> ''
                ORDER BY TRIM({coluna})
            """).ToList();
        }

        public List<string> ListarReusDistintosCadastro()
        {
            using var conn = _db.GetConnection();
            return conn.Query<string>("""
                SELECT DISTINCT TRIM(nome_reu)
                FROM processo_reus
                WHERE nome_reu IS NOT NULL
                  AND TRIM(nome_reu) <> ''
                ORDER BY TRIM(nome_reu)
            """).ToList();
        }

        public (decimal saldoPendente, bool diligenciaPendente, string? dataUltLanc)
            ObterResumo(string processoId)
        {
            using var conn = _db.GetConnection();

            var saldo = conn.ExecuteScalar<decimal?>("""
                SELECT CAST(COALESCE(SUM(valor_conta), 0.0) AS REAL)
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
