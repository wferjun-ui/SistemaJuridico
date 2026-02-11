using System.Windows.Forms;
using Dapper;
using SistemaJuridico.Models;

namespace SistemaJuridico.Services
{
    public class InicializacaoSistemaService
    {
        public string Inicializar()
        {
            var caminhoDb = ConfigService.ObterCaminhoBanco();

            if (string.IsNullOrEmpty(caminhoDb))
            {
                caminhoDb = SelecionarPastaBanco();

                if (string.IsNullOrEmpty(caminhoDb))
                    throw new Exception("Banco não configurado.");

                ConfigService.SalvarCaminhoBanco(caminhoDb);
            }

            CriarEstruturaBanco();

            ImportarSeExistirJson();

            CriarAdminPadrao();

            return caminhoDb;
        }

        // =========================
        // SELECIONAR PASTA
        // =========================

        private string SelecionarPastaBanco()
        {
            using var dialog = new FolderBrowserDialog();

            dialog.Description =
                "Selecione a pasta onde ficará o banco do sistema";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                return Path.Combine(dialog.SelectedPath, "juridico.db");
            }

            return "";
        }

        // =========================
        // CRIAR TABELAS
        // =========================

        private void CriarEstruturaBanco()
        {
            var db = new DatabaseService();
            using var conn = db.GetConnection();

            conn.Execute(@"
                CREATE TABLE IF NOT EXISTS processos (
                    id TEXT PRIMARY KEY,
                    numero TEXT,
                    paciente TEXT,
                    juiz TEXT,
                    classificacao TEXT,
                    status_fase TEXT,
                    ultima_atualizacao TEXT,
                    observacao_fixa TEXT
                );
            ");

            conn.Execute(@"
                CREATE TABLE IF NOT EXISTS contas (
                    id TEXT PRIMARY KEY,
                    processo_id TEXT,
                    data_movimentacao TEXT,
                    tipo_lancamento TEXT,
                    historico TEXT,
                    mov_processo TEXT,
                    num_nf_alvara TEXT,
                    valor_alvara REAL,
                    valor_conta REAL,
                    observacoes TEXT,
                    responsavel TEXT,
                    status_conta TEXT
                );
            ");

            conn.Execute(@"
                CREATE TABLE IF NOT EXISTS usuarios (
                    id TEXT PRIMARY KEY,
                    email TEXT,
                    username TEXT,
                    is_admin INTEGER
                );
            ");

            conn.Execute(@"
                CREATE TABLE IF NOT EXISTS verificacoes (
                    id TEXT PRIMARY KEY,
                    processo_id TEXT,
                    data_hora TEXT,
                    status_processo TEXT,
                    responsavel TEXT,
                    diligencia_pendente INTEGER,
                    pendencias_descricao TEXT
                );
            ");

            conn.Execute(@"
                CREATE TABLE IF NOT EXISTS historico (
                    id TEXT PRIMARY KEY,
                    processo_id TEXT,
                    acao TEXT,
                    usuario TEXT,
                    data_hora TEXT,
                    detalhes TEXT
                );
            ");
        }

        // =========================
        // IMPORTAÇÃO AUTOMÁTICA
        // =========================

        private void ImportarSeExistirJson()
        {
            var pasta = Path.GetDirectoryName(
                ConfigService.ObterCaminhoBanco());

            var jsonPath =
                Path.Combine(pasta!, "MIGRACAO_COMPLETA_JURIDICO.json");

            if (!File.Exists(jsonPath))
                return;

            var db = new DatabaseService();
            var importer = new ImportacaoJsonService(db);

            importer.ImportarArquivo(jsonPath);
        }

        // =========================
        // ADMIN PADRÃO
        // =========================

        private void CriarAdminPadrao()
        {
            var db = new DatabaseService();
            using var conn = db.GetConnection();

            var total = conn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM usuarios");

            if (total > 0) return;

            conn.Execute(@"
                INSERT INTO usuarios
                (id, email, username, is_admin)
                VALUES
                (@id, 'admin', 'admin', 1)
            ",
            new { id = Guid.NewGuid().ToString() });
        }
    }
}
