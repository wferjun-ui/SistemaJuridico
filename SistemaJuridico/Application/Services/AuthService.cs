using Dapper;
using Microsoft.Data.Sqlite;
using SistemaJuridico.Infrastructure;
using SistemaJuridico.Models;


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
            return Login(usuarioOuEmail);
        }

        public bool CriarUsuario(string username, string email, string senha, string perfil)
        {
            using var conn = new SqliteConnection(_connectionString);
            var id = Guid.NewGuid().ToString();
            conn.Execute(@"INSERT INTO usuarios (id, username, email, perfil, password_hash, salt) VALUES (@id, @username, @email, @perfil, '', '')",
                new { id, username, email, perfil });
            return true;
        }

        public void Logout()
        {
            var nome = SessaoUsuarioService.Instance.NomeUsuario;
            SessaoUsuarioService.Instance.EncerrarSessao();
            _logger.Audit($"Logout realizado por {nome}");
        }
    }
}
