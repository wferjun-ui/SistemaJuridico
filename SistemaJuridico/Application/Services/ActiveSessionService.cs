using Dapper;
using SistemaJuridico.Models;
using System.Globalization;

namespace SistemaJuridico.Services
{
    public class ActiveSessionService
    {
        private readonly DatabaseService _db;

        public ActiveSessionService(DatabaseService db)
        {
            _db = db;
        }

        public void RecordUserActivity(string userEmail, string userName, string? processId = null, string? processNumero = null, string? processPaciente = null)
        {
            if (string.IsNullOrWhiteSpace(userEmail) || string.IsNullOrWhiteSpace(userName))
                return;

            using var conn = _db.GetConnection();
            var now = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

            var existing = conn.QueryFirstOrDefault<ActiveSession>(
                "SELECT * FROM active_sessions WHERE lower(user_email) = lower(@e)",
                new { e = userEmail.Trim() });

            if (existing == null)
            {
                conn.Execute(@"
INSERT INTO active_sessions(
    id, user_email, user_name, last_activity_timestamp,
    last_process_id, last_process_numero, last_process_paciente)
VALUES(
    @id, @userEmail, @userName, @lastActivityTimestamp,
    @lastProcessId, @lastProcessNumero, @lastProcessPaciente)",
                    new
                    {
                        id = Guid.NewGuid().ToString(),
                        userEmail = userEmail.Trim(),
                        userName = userName.Trim(),
                        lastActivityTimestamp = now,
                        lastProcessId = processId,
                        lastProcessNumero = processNumero,
                        lastProcessPaciente = processPaciente
                    });
                return;
            }

            conn.Execute(@"
UPDATE active_sessions
SET user_name = @userName,
    last_activity_timestamp = @lastActivityTimestamp,
    last_process_id = @lastProcessId,
    last_process_numero = @lastProcessNumero,
    last_process_paciente = @lastProcessPaciente
WHERE id = @id",
                new
                {
                    id = existing.Id,
                    userName = userName.Trim(),
                    lastActivityTimestamp = now,
                    lastProcessId = processId,
                    lastProcessNumero = processNumero,
                    lastProcessPaciente = processPaciente
                });
        }

        public List<ActiveSession> GetRecentUserActivity(int maxMinutes = 20)
        {
            if (maxMinutes <= 0)
                maxMinutes = 20;

            var threshold = DateTime.UtcNow.AddMinutes(-maxMinutes).ToString("o", CultureInfo.InvariantCulture);

            using var conn = _db.GetConnection();
            var result = conn.Query<ActiveSession>(@"
SELECT *
FROM active_sessions
WHERE last_activity_timestamp >= @threshold
ORDER BY last_activity_timestamp DESC", new { threshold });

            return result.ToList();
        }
    }
}
