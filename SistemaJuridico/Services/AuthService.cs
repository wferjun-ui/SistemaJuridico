using Dapper;
using SistemaJuridico.Models;

namespace SistemaJuridico.Services
{
    public class AuthService
    {
        private readonly DatabaseService _db;

        public AuthService(DatabaseService db)
        {
            _db = db;
        }

        public Usuario? Login(string login, string senha)
        {
            using var conn = _db.GetConnection();

            var u = conn.QueryFirstOrDefault<dynamic>(
                "SELECT * FROM usuarios WHERE lower(username)=lower(@l) OR lower(email)=lower(@l)",
                new { l = login });

            if (u == null)
                return null;

            string hash = _db.HashSenha(senha, u.salt ?? "");

            if (hash != u.password_hash)
                return null;

            return new Usuario
            {
                Id = u.id,
                Username = u.username,
                Email = u.email,
                Perfil = u.perfil
            };
        }

        public void CriarUsuario(string username, string email, string senha, string perfil)
        {
            using var conn = _db.GetConnection();

            string salt = _db.GerarSalt();
            string hash = _db.HashSenha(senha, salt);

            conn.Execute(@"
INSERT INTO usuarios(id,username,password_hash,salt,perfil,email)
VALUES(@id,@u,@h,@s,@p,@e)",
                new
                {
                    id = System.Guid.NewGuid().ToString(),
                    u = username,
                    h = hash,
                    s = salt,
                    p = perfil,
                    e = email
                });
        }
    }
}

