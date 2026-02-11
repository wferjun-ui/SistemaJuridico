using Dapper;

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
        // VERSÃO 1
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
        // VERSÃO 2
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
    }
}
