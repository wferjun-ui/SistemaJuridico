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
            using var conn = new SqliteConnection(_connectionString);

            var registro = conn.QueryFirstOrDefault<UsuarioLoginRegistro>(@"
SELECT id, username, email, perfil, password_hash AS PasswordHash, salt AS Salt
FROM usuarios
WHERE lower(username) = lower(@usuarioOuEmail)
   OR lower(email) = lower(@usuarioOuEmail)
LIMIT 1",
                new { usuarioOuEmail });

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
            using var conn = new SqliteConnection(_connectionString);
            var id = Guid.NewGuid().ToString();
            var salt = GerarSalt();
            var hash = HashSenha(senha, salt);

            conn.Execute(@"INSERT INTO usuarios (id, username, email, perfil, password_hash, salt) VALUES (@id, @username, @email, @perfil, @hash, @salt)",
                new { id, username, email, perfil, hash, salt });
            return true;
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
