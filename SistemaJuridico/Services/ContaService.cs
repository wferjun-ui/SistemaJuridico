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

        public void Inserir(Conta conta)
        {
            using var conn = _db.GetConnection();

            conn.Execute(@"
                INSERT INTO contas
                VALUES
                (@Id,@ProcessoId,@Tipo,@ValorAlvara,
                 @ValorConta,@StatusConta,
                 @DataMovimentacao,@Observacao)
            ", conta);
        }

        public void Atualizar(Conta conta)
        {
            using var conn = _db.GetConnection();

            conn.Execute(@"
                UPDATE contas SET
                    tipo=@Tipo,
                    valor_alvara=@ValorAlvara,
                    valor_conta=@ValorConta,
                    observacao=@Observacao
                WHERE id=@Id
            ", conta);
        }

        public void Excluir(string id)
        {
            using var conn = _db.GetConnection();

            conn.Execute("DELETE FROM contas WHERE id=@id",
                new { id });
        }

        public void FecharConta(string id)
        {
            using var conn = _db.GetConnection();

            conn.Execute(@"
                UPDATE contas
                SET status_conta='fechada'
                WHERE id=@id
            ", new { id });
        }
    }
}
