using Dapper;
using SistemaJuridico.Models;

namespace SistemaJuridico.Services
{
    public class ContaService
    {
        private readonly DatabaseService _db;

        public ContaService(DatabaseService db)
        {
            _db = db;
        }

        public List<Conta> ListarPorProcesso(string processoId)
        {
            using var conn = _db.GetConnection();

            return conn.Query<Conta>(@"
                SELECT * FROM contas
                WHERE processo_id=@id
                ORDER BY data_movimentacao DESC
            ", new { id = processoId }).ToList();
        }

        public void SalvarConta(Conta conta)
        {
            using var conn = _db.GetConnection();

            conn.Execute(@"
                INSERT INTO contas
                (id, processo_id, tipo, valor_alvara,
                 valor_conta, status_conta,
                 data_movimentacao, observacao)

                VALUES
                (@Id, @ProcessoId, @Tipo, @ValorAlvara,
                 @ValorConta, @StatusConta,
                 @DataMovimentacao, @Observacao)
            ", conta);
        }
    }
}

