using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SistemaJuridico.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly string _dbPath;
        private readonly string _backupFolder;

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

        // =====================================================
        // INICIALIZAÇÃO
        // =====================================================

        public void Initialize()
        {
            using var conn = GetConnection();
            conn.Open();

            conn.Execute("PRAGMA journal_mode=WAL;");
            conn.Execute("PRAGMA foreign_keys=ON;");

            CriarTabelas(conn);
            CriarAdminPadrao(conn);
        }

        // =====================================================
        // CRIAÇÃO DE TABELAS
        // =====================================================

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
    situacao_rascunho TEXT,
    motivo_rascunho TEXT,
    usuario_rascunho TEXT,
    usuario_editando TEXT
);

CREATE TABLE IF NOT EXISTS processo_edicao_status(
    id TEXT PRIMARY KEY,
    processo_id TEXT,
    usuario_email TEXT,
    status TEXT,
    motivo TEXT,
    data_inicio TEXT,
    data_atualizacao TEXT
);

CREATE TABLE IF NOT EXISTS reus(
    id TEXT PRIMARY KEY,
    processo_id TEXT,
    nome TEXT
);

CREATE TABLE IF NOT EXISTS itens_saude(
    id TEXT PRIMARY KEY,
    processo_id TEXT,
    tipo TEXT,
    nome TEXT,
    qtd TEXT,
    frequencia TEXT,
    local TEXT,
    data_prescricao TEXT,
    is_desnecessario INTEGER DEFAULT 0,
    tem_bloqueio INTEGER DEFAULT 0
);

CREATE TABLE IF NOT EXISTS verificacoes(
    id TEXT PRIMARY KEY,
    processo_id TEXT,
    data_hora TEXT,
    status_processo TEXT,
    responsavel TEXT,
    diligencia_realizada INTEGER,
    diligencia_descricao TEXT,
    diligencia_pendente INTEGER,
    pendencias_descricao TEXT,
    prazo_diligencia TEXT,
    proximo_prazo_padrao TEXT,
    data_notificacao TEXT,
    alteracoes_texto TEXT,
    itens_snapshot_json TEXT
);

CREATE TABLE IF NOT EXISTS contas(
    id TEXT PRIMARY KEY,
    processo_id TEXT,
    tipo_lancamento TEXT,
    historico TEXT,
    data_movimentacao TEXT,
    mov_processo TEXT,
    num_nf_alvara TEXT,
    valor_alvara REAL,
    valor_conta REAL,
    status_conta TEXT,
    responsavel TEXT,
    observacoes TEXT
);

CREATE TABLE IF NOT EXISTS diligencias(
    id TEXT PRIMARY KEY,
    processo_id TEXT,
    descricao TEXT,
    data_criacao TEXT,
    data_conclusao TEXT,
    concluida INTEGER,
    responsavel TEXT
);

CREATE TABLE IF NOT EXISTS historico(
    id TEXT PRIMARY KEY,
    processo_id TEXT,
    acao TEXT,
    usuario TEXT,
    data_hora TEXT,
    detalhes TEXT
);

");
        }

        // =====================================================
        // ADMIN PADRÃO
        // =====================================================

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

        // =====================================================
        // SEGURANÇA
        // =====================================================

        public string GerarSalt()
            => Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

        public string HashSenha(string senha, string salt)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(
                sha.ComputeHash(Encoding.UTF8.GetBytes(senha + salt)));
        }

        // =====================================================
        // BACKUP AUTOMÁTICO
        // =====================================================

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
                // evita crash em produção
            }
        }
    }
}
