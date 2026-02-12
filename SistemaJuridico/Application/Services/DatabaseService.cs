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

        public string ConnectionString => _connectionString;

        public DatabaseService() : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SistemaJuridico"))
        {
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
@@ -143,97 +146,84 @@ CREATE TABLE IF NOT EXISTS contas(
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
