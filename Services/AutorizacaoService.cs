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
            using var conn = _db.GetConnection();

            var count = conn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM emails_autorizados WHERE lower(email)=lower(@e)",
                new { e = email });

            return count > 0;
        }

        public void AdicionarEmail(string email)
        {
            using var conn = _db.GetConnection();

            conn.Execute(@"
INSERT INTO emails_autorizados(id,email)
VALUES(@id,@e)",
                new
                {
                    id = Guid.NewGuid().ToString(),
                    e = email
                });
        }
    }
}

