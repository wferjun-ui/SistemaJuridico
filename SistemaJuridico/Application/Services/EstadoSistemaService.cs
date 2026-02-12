using Dapper;

namespace SistemaJuridico.Services
{
    public class EstadoSistemaService
    {
        private readonly DatabaseService _databaseService;

        public EstadoSistemaService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public bool SistemaPossuiDados()
        {
            using var conn = _databaseService.GetConnection();

            var totalProcessos = conn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM processos");

            return totalProcessos > 0;
        }
    }
}
