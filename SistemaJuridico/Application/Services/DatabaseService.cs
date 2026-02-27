using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SistemaJuridico.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly string _dbPath;
        private readonly string _backupFolder;

        public string ConnectionString => _connectionString;

        public DatabaseService() : this(ObterPastaPadrao())
        {
        }

        private static string ObterPastaPadrao()
        {
            var caminhoConfigurado = ConfigService.ObterCaminhoBanco();

            if (!string.IsNullOrWhiteSpace(caminhoConfigurado))
            {
                var pastaConfigurada = Path.GetDirectoryName(caminhoConfigurado);
                if (!string.IsNullOrWhiteSpace(pastaConfigurada))
                    return pastaConfigurada;
            }

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SistemaJuridico");
        }

        public DatabaseService(string baseFolder)
        {
            if (!Directory.Exists(baseFolder))
                Directory.CreateDirectory(baseFolder);

            _dbPath = Path.Combine(baseFolder, "juridico.db");
            _backupFolder = Path.Combine(baseFolder, "Backups");

            _connectionString = $"Data Source={_dbPath}";
            DefaultTypeMap.MatchNamesWithUnderscores = true;
        }

        public SqliteConnection GetConnection()
            => new SqliteConnection(_connectionString);

        public SqliteConnection CreateConnection()
            => GetConnection();

        public void Initialize()
        {
            using var conn = GetConnection();
            conn.Open();

            conn.Execute("PRAGMA journal_mode=WAL;");
            conn.Execute("PRAGMA foreign_keys=ON;");

            CriarTabelas(conn);
            GarantirEstruturaAtualizada(conn);
            CriarAdminPadrao(conn);
        }

        private void CriarTabelas(SqliteConnection conn)
        {
            conn.Execute(@"
CREATE TABLE IF NOT EXISTS usuarios(
    id TEXT PRIMARY KEY,
    username TEXT UNIQUE,
    password_hash TEXT,
    salt TEXT,
    perfil TEXT,
    email TEXT UNIQUE
);

CREATE TABLE IF NOT EXISTS emails_autorizados(
    id TEXT PRIMARY KEY,
    email TEXT UNIQUE
);

CREATE TABLE IF NOT EXISTS processos(
    id TEXT PRIMARY KEY,
    numero TEXT,
    paciente TEXT,
    juiz TEXT,
    classificacao TEXT,
    status_fase TEXT,
    ultima_atualizacao TEXT,
    observacao_fixa TEXT,
    cache_proximo_prazo TEXT,
    situacao_rascunho TEXT DEFAULT 'Concluído',
    motivo_rascunho TEXT,
    usuario_rascunho TEXT,
    lock_usuario TEXT,
    lock_timestamp TEXT
);


CREATE TABLE IF NOT EXISTS processo_reus(
    id TEXT PRIMARY KEY,
    processo_id TEXT,
    nome_reu TEXT
);

CREATE TABLE IF NOT EXISTS catalogo_itens_saude(
    id TEXT PRIMARY KEY,
    tipo TEXT,
    nome TEXT,
    UNIQUE(tipo, nome)
);

CREATE TABLE IF NOT EXISTS contas(
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

CREATE TABLE IF NOT EXISTS verificacoes(
    id TEXT PRIMARY KEY,
    processo_id TEXT,
    data_hora TEXT,
    status_processo TEXT,
    responsavel TEXT,
    diligencia_pendente INTEGER,
    pendencias_descricao TEXT,
    diligencia_realizada INTEGER,
    diligencia_descricao TEXT,
    diligencia_status TEXT,
    prazo_diligencia TEXT,
    proximo_prazo_padrao TEXT,
    proxima_verificacao TEXT,
    data_notificacao TEXT,
    descricao_persistente TEXT,
    alteracoes_texto TEXT,
    itens_snapshot_json TEXT
);

CREATE TABLE IF NOT EXISTS itens_saude(
    id TEXT PRIMARY KEY,
    processo_id TEXT,
    tipo TEXT,
    nome TEXT,
    qtd TEXT,
    qtd_sus TEXT,
    qtd_particular TEXT,
    frequencia TEXT,
    local TEXT,
    data_prescricao TEXT,
    is_desnecessario INTEGER,
    tem_bloqueio INTEGER
);

CREATE TABLE IF NOT EXISTS diligencias(
    id TEXT PRIMARY KEY,
    processo_id TEXT,
    descricao TEXT,
    data_criacao TEXT,
    data_conclusao TEXT,
    concluida INTEGER,
    responsavel TEXT,
    prazo TEXT
);

CREATE TABLE IF NOT EXISTS historico(
    id TEXT PRIMARY KEY,
    processo_id TEXT,
    acao TEXT,
    usuario TEXT,
    data_hora TEXT,
    detalhes TEXT
);

CREATE TABLE IF NOT EXISTS audit_log(
    id TEXT PRIMARY KEY,
    data_hora TEXT,
    usuario TEXT,
    acao TEXT,
    entidade TEXT,
    entidade_id TEXT,
    detalhes TEXT
);

CREATE TABLE IF NOT EXISTS active_sessions(
    id TEXT PRIMARY KEY,
    user_email TEXT,
    user_name TEXT,
    last_activity_timestamp TEXT,
    last_process_id TEXT,
    last_process_numero TEXT,
    last_process_paciente TEXT
);

CREATE TABLE IF NOT EXISTS schema_version(
    versao INTEGER
);
");
        }


        private void GarantirEstruturaAtualizada(SqliteConnection conn)
        {
            AdicionarColunaSeNaoExistir(conn, "processos", "representante", "TEXT");
            AdicionarColunaSeNaoExistir(conn, "processos", "sem_representante", "INTEGER DEFAULT 0");
            AdicionarColunaSeNaoExistir(conn, "processos", "tipo_processo", "TEXT");

            AdicionarColunaSeNaoExistir(conn, "contas", "terapia_medicamento_nome", "TEXT");
            AdicionarColunaSeNaoExistir(conn, "contas", "quantidade", "TEXT");
            AdicionarColunaSeNaoExistir(conn, "contas", "mes_referencia", "TEXT");
            AdicionarColunaSeNaoExistir(conn, "contas", "ano_referencia", "TEXT");

            AdicionarColunaSeNaoExistir(conn, "verificacoes", "diligencia_status", "TEXT");
            AdicionarColunaSeNaoExistir(conn, "verificacoes", "proxima_verificacao", "TEXT");
            AdicionarColunaSeNaoExistir(conn, "verificacoes", "descricao_persistente", "TEXT");
            AdicionarColunaSeNaoExistir(conn, "itens_saude", "qtd_sus", "TEXT");
            AdicionarColunaSeNaoExistir(conn, "itens_saude", "qtd_particular", "TEXT");
        }

        private void AdicionarColunaSeNaoExistir(
            SqliteConnection conn,
            string tabela,
            string coluna,
            string definicao)
        {
            var colunas = conn.Query($"PRAGMA table_info({tabela});")
                .Select(c => (string)c.name)
                .ToList();

            if (colunas.Any(c => string.Equals(c, coluna, StringComparison.OrdinalIgnoreCase)))
                return;

            conn.Execute($"ALTER TABLE {tabela} ADD COLUMN {coluna} {definicao}");
        }

        private void CriarAdminPadrao(SqliteConnection conn)
        {
            int count = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM usuarios");

            if (count > 0)
                return;

            string salt = GerarSalt();
            string hash = HashSenha("admin", salt);

            conn.Execute(@"
INSERT INTO usuarios
(id, username, password_hash, salt, perfil, email)
VALUES
(@id,'admin',@h,@s,'Admin','admin@sistema.local')
",
                new
                {
                    id = Guid.NewGuid().ToString(),
                    h = hash,
                    s = salt
                });
        }

        public string GerarSalt()
            => Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

        public string HashSenha(string senha, string salt)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(
                sha.ComputeHash(Encoding.UTF8.GetBytes(senha + salt)));
        }

        public void PerformBackup()
        {
            try
            {
                if (!Directory.Exists(_backupFolder))
                    Directory.CreateDirectory(_backupFolder);

                var file = Path.Combine(
                    _backupFolder,
                    $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");

                using var source = GetConnection();
                using var dest = new SqliteConnection($"Data Source={file}");

                source.Open();
                dest.Open();

                source.BackupDatabase(dest);
            }
            catch
            {
            }
        }
    }
}
