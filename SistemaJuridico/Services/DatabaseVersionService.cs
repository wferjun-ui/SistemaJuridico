using Dapper;
using Microsoft.Data.Sqlite;

namespace SistemaJuridico.Services
{
    public class DatabaseVersionService
    {
        private readonly DatabaseService _db;

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
        // VERSÃO 1 — RASCUNHO
        // =========================

        private void AtualizarParaV1(SqliteConnection conn)
        {
            conn.Execute(@"
ALTER TABLE processos ADD COLUMN situacao_rascunho TEXT DEFAULT 'Concluído';
");

            conn.Execute(@"
ALTER TABLE processos ADD COLUMN motivo_rascunho TEXT;
");

            conn.Execute(@"
ALTER TABLE processos ADD COLUMN usuario_rascunho TEXT;
");

            DefinirVersao(conn, 1);
        }

        // =========================
        // VERSÃO 2 — ITENS SAÚDE
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
);
");

            DefinirVersao(conn, 2);
        }

        // =========================
        // VERSÃO 3 — LOCK MULTIUSUÁRIO
        // =========================

        private void AtualizarParaV3_LockMultiusuario(SqliteConnection conn)
        {
            // SQLite não tem IF NOT EXISTS para coluna
            // então usamos try/catch silencioso

            try
            {
                conn.Execute(@"
ALTER TABLE processos ADD COLUMN lock_usuario TEXT;
");
            }
            catch { }

            try
            {
                conn.Execute(@"
ALTER TABLE processos ADD COLUMN lock_timestamp TEXT;
");
            }
            catch { }

            DefinirVersao(conn, 3);
        }
    }
}
