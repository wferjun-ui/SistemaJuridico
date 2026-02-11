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

CREATE TABLE IF NOT EXISTS processos(
    id TEXT PRIMARY KEY,
    numero TEXT,
    paciente TEXT,
    juiz TEXT,
    classificacao TEXT,
    status_fase TEXT,
    ultima_atualizacao TEXT
    UsuarioEditando TEXT
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

CREATE TABLE IF NOT EXISTS contas(
    id TEXT PRIMARY KEY,
    processo_id TEXT,
    descricao TEXT,
    valor REAL,
    data_lancamento TEXT
);

CREATE TABLE IF NOT EXISTS diligencias(
    id TEXT PRIMARY KEY,
    processo_id TEXT,
    descricao TEXT,
    pendente INTEGER
);

CREATE TABLE IF NOT EXISTS emails_autorizados(
    id TEXT PRIMARY KEY,
    email TEXT UNIQUE
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
    }
}

