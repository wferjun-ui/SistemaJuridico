using Dapper;
using Microsoft.Data.Sqlite;
using SistemaJuridico.Infrastructure;
using SistemaJuridico.Models;
using System;
using System.Security.Cryptography;
using System.Text;

namespace SistemaJuridico.Services
{
    public class AuthService
    {
        private readonly string _connectionString;
        private readonly LoggerService _logger = new();

        public AuthService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Usuario? Login(string email)
        {
            using var conn = new SqliteConnection(_connectionString);

            var usuario = conn.QueryFirstOrDefault<Usuario>(
                "SELECT * FROM usuarios WHERE email = @email",
                new { email });

            if (usuario != null)
            {
                SessaoUsuarioService.Instance.IniciarSessao(usuario);
                _logger.Audit("Login realizado");
            }
            else
            {
                _logger.Warn($"Tentativa de login inválida: {email}");
            }

            return usuario;
        }

        public Usuario? Login(string usuarioOuEmail, string senha)
        {
            if (string.IsNullOrWhiteSpace(usuarioOuEmail) || string.IsNullOrWhiteSpace(senha))
                return null;

            using var conn = new SqliteConnection(_connectionString);

            var registro = conn.QueryFirstOrDefault<UsuarioLoginRegistro>(@"
SELECT id, username, email, perfil, password_hash AS PasswordHash, salt AS Salt
FROM usuarios
WHERE lower(username) = lower(@usuarioOuEmail)
   OR lower(email) = lower(@usuarioOuEmail)
LIMIT 1",
                new { usuarioOuEmail = usuarioOuEmail.Trim() });

            if (registro == null)
            {
                _logger.Warn($"Tentativa de login inválida: {usuarioOuEmail}");
                return null;
            }

            var hashInformado = HashSenha(senha, registro.Salt ?? string.Empty);
            if (!string.Equals(hashInformado, registro.PasswordHash, StringComparison.OrdinalIgnoreCase))
            {
                _logger.Warn($"Senha inválida para o usuário: {usuarioOuEmail}");
                return null;
            }

            var usuario = new Usuario
            {
                Id = registro.Id,
                Username = registro.Username,
                Email = registro.Email,
                Perfil = registro.Perfil
            };

            SessaoUsuarioService.Instance.IniciarSessao(usuario);
            _logger.Audit("Login realizado");

            return usuario;
        }

        public bool CriarUsuario(string username, string email, string senha, string perfil)
        {
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(senha) ||
                string.IsNullOrWhiteSpace(perfil))
                throw new InvalidOperationException("Todos os campos do usuário são obrigatórios.");

            using var conn = new SqliteConnection(_connectionString);
            var id = Guid.NewGuid().ToString();
            var salt = GerarSalt();
            var hash = HashSenha(senha, salt);

            try
            {
                var linhas = conn.Execute(@"INSERT INTO usuarios (id, username, email, perfil, password_hash, salt) VALUES (@id, @username, @email, @perfil, @hash, @salt)",
                    new
                    {
                        id,
                        username = username.Trim(),
                        email = email.Trim(),
                        perfil = perfil.Trim(),
                        hash,
                        salt
                    });

                return linhas > 0;
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                throw new InvalidOperationException("Usuário ou e-mail já cadastrado.");
            }
        }

        public List<Usuario> ListarUsuarios()
        {
            using var conn = new SqliteConnection(_connectionString);

            return conn.Query<Usuario>(@"
SELECT id, username, email, perfil
FROM usuarios
ORDER BY username").ToList();
        }

        public void AlterarPerfil(string usuarioId, string perfil)
        {
            if (string.IsNullOrWhiteSpace(usuarioId) || string.IsNullOrWhiteSpace(perfil))
                throw new InvalidOperationException("Usuário e perfil são obrigatórios.");

            using var conn = new SqliteConnection(_connectionString);
            conn.Execute("UPDATE usuarios SET perfil=@perfil WHERE id=@id", new { id = usuarioId, perfil = perfil.Trim() });
        }

        public void AlterarSenha(string usuarioId, string novaSenha)
        {
            if (string.IsNullOrWhiteSpace(usuarioId) || string.IsNullOrWhiteSpace(novaSenha))
                throw new InvalidOperationException("Usuário e senha são obrigatórios.");

            using var conn = new SqliteConnection(_connectionString);
            var salt = GerarSalt();
            var hash = HashSenha(novaSenha.Trim(), salt);

            conn.Execute(
                "UPDATE usuarios SET password_hash=@hash, salt=@salt WHERE id=@id",
                new { id = usuarioId, hash, salt });
        }

        public void ExcluirUsuario(string usuarioId)
        {
            if (string.IsNullOrWhiteSpace(usuarioId))
                throw new InvalidOperationException("Usuário inválido.");

            using var conn = new SqliteConnection(_connectionString);
            conn.Execute("DELETE FROM usuarios WHERE id=@id", new { id = usuarioId });
        }

        public void Logout()
        {
            var nome = SessaoUsuarioService.Instance.NomeUsuario;
            SessaoUsuarioService.Instance.EncerrarSessao();
            _logger.Audit($"Logout realizado por {nome}");
        }

        private static string GerarSalt()
            => Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

        private static string HashSenha(string senha, string salt)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(
                sha.ComputeHash(Encoding.UTF8.GetBytes(senha + salt)));
        }

        private sealed class UsuarioLoginRegistro
        {
            public string Id { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Perfil { get; set; } = string.Empty;
            public string PasswordHash { get; set; } = string.Empty;
            public string Salt { get; set; } = string.Empty;
        }
    }
}
