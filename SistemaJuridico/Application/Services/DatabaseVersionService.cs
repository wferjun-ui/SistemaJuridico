using Dapper;
using Microsoft.Data.Sqlite;
using SistemaJuridico.Infrastructure;

namespace SistemaJuridico.Services
{
    public class DatabaseVersionService
    {
        private readonly DatabaseService _db;
        private readonly LoggerService _logger = new();

        public DatabaseVersionService(DatabaseService db)
        {
            _db = db;
        }

        public void GarantirAtualizacao()
        {
            using var conn = _db.GetConnection();
            conn.Open();

            CriarTabelaControle(conn);

            int versaoAtual = ObterVersao(conn);

            if (versaoAtual < 1)
                AtualizarParaV1(conn);

            if (versaoAtual < 2)
                AtualizarParaV2(conn);

            if (versaoAtual < 3)
                AtualizarParaV3_LockMultiusuario(conn);

            if (versaoAtual < 4)
                AtualizarParaV4_Auditoria(conn);
        }

        private void CriarTabelaControle(SqliteConnection conn)
        {
            conn.Execute(@"
CREATE TABLE IF NOT EXISTS schema_version (
    versao INTEGER
);
");

            var existe = conn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM schema_version");

            if (existe == 0)
                conn.Execute("INSERT INTO schema_version VALUES (0)");
        }

        private int ObterVersao(SqliteConnection conn)
        {
            return conn.ExecuteScalar<int>(
                "SELECT versao FROM schema_version LIMIT 1");
        }

        private void DefinirVersao(SqliteConnection conn, int versao)
        {
            conn.Execute(
                "UPDATE schema_version SET versao=@v",
                new { v = versao });
        }

        // =========================
        // V1
        // =========================

        private void AtualizarParaV1(SqliteConnection conn)
        {
            ExecutarAlterTableIgnorandoColunaExistente(
                conn,
                "ALTER TABLE processos ADD COLUMN situacao_rascunho TEXT DEFAULT 'Concluído';",
                "situacao_rascunho");

            ExecutarAlterTableIgnorandoColunaExistente(
                conn,
                "ALTER TABLE processos ADD COLUMN motivo_rascunho TEXT;",
                "motivo_rascunho");

            ExecutarAlterTableIgnorandoColunaExistente(
                conn,
                "ALTER TABLE processos ADD COLUMN usuario_rascunho TEXT;",
                "usuario_rascunho");

            DefinirVersao(conn, 1);
        }

        // =========================
        // V2
        // =========================

        private void AtualizarParaV2(SqliteConnection conn)
        {
            conn.Execute(@"
CREATE TABLE IF NOT EXISTS itens_saude (
    id TEXT PRIMARY KEY,
    processo_id TEXT,
    tipo TEXT,
    nome TEXT,
    qtd TEXT,
    frequencia TEXT,
    local TEXT,
    data_prescricao TEXT,
    is_desnecessario INTEGER,
    tem_bloqueio INTEGER
);");

            DefinirVersao(conn, 2);
        }

        // =========================
        // V3
        // =========================

        private void AtualizarParaV3_LockMultiusuario(SqliteConnection conn)
        {
            ExecutarAlterTableIgnorandoColunaExistente(
                conn,
                "ALTER TABLE processos ADD COLUMN lock_usuario TEXT;",
                "lock_usuario");

            ExecutarAlterTableIgnorandoColunaExistente(
                conn,
                "ALTER TABLE processos ADD COLUMN lock_timestamp TEXT;",
                "lock_timestamp");

            DefinirVersao(conn, 3);
        }

        private void ExecutarAlterTableIgnorandoColunaExistente(
            SqliteConnection conn,
            string sql,
            string coluna)
        {
            try
            {
                conn.Execute(sql);
            }
            catch (SqliteException ex) when (
                ex.Message.Contains("duplicate column name", StringComparison.OrdinalIgnoreCase))
            {
                _logger.Warn($"Coluna '{coluna}' já existe em 'processos'. Migração ignorada.");
            }
        }

        // =========================
        // V4 AUDITORIA
        // =========================

        private void AtualizarParaV4_Auditoria(SqliteConnection conn)
        {
            conn.Execute(@"
CREATE TABLE IF NOT EXISTS audit_log (
    id TEXT PRIMARY KEY,
    data_hora TEXT,
    usuario TEXT,
    acao TEXT,
    entidade TEXT,
    entidade_id TEXT,
    detalhes TEXT
);");

            DefinirVersao(conn, 4);
        }
    }
}
