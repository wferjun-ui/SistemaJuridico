using Dapper;
using System;

namespace SistemaJuridico.Services
{
    public class AutorizacaoService
    {
        private readonly DatabaseService _db;

        public AutorizacaoService(DatabaseService db)
        {
            _db = db;
        }

        public bool EmailAutorizado(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            using var conn = _db.GetConnection();

            var count = conn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM emails_autorizados WHERE lower(email)=lower(@e)",
                new { e = email.Trim() });

            return count > 0;
        }

        public bool AdicionarEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            using var conn = _db.GetConnection();

            var existe = conn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM emails_autorizados WHERE lower(email)=lower(@e)",
                new { e = email.Trim() });

            if (existe > 0)
                return false;

            var linhas = conn.Execute(@"
INSERT INTO emails_autorizados(id,email)
VALUES(@id,@e)",
                new
                {
                    id = Guid.NewGuid().ToString(),
                    e = email.Trim()
                });

            return linhas > 0;
        }

        public bool RemoverEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            using var conn = _db.GetConnection();

            var linhas = conn.Execute(
                "DELETE FROM emails_autorizados WHERE lower(email)=lower(@e)",
                new { e = email.Trim() });

            return linhas > 0;
        }

    }
}
