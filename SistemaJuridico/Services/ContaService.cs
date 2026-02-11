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
                SELECT
                    id as Id,
                    processo_id as ProcessoId,
                    tipo_lancamento as TipoLancamento,
                    historico as Historico,
                    data_movimentacao as DataMovimentacao,
                    mov_processo as MovProcesso,
                    num_nf_alvara as NumNfAlvara,
                    valor_alvara as ValorAlvara,
                    valor_conta as ValorConta,
                    status_conta as StatusConta,
                    responsavel as Responsavel,
                    observacoes as Observacoes
                FROM contas
                WHERE processo_id=@id
                ORDER BY data_movimentacao
            ", new { id = processoId }).ToList();
        }

        public void Inserir(Conta conta)
        {
            using var conn = _db.GetConnection();

            conn.Execute(@"
                INSERT INTO contas (
                    id, processo_id, tipo_lancamento,
                    historico, data_movimentacao,
                    mov_processo, num_nf_alvara,
                    valor_alvara, valor_conta,
                    status_conta, responsavel, observacoes
                )
                VALUES (
                    @Id, @ProcessoId, @TipoLancamento,
                    @Historico, @DataMovimentacao,
                    @MovProcesso, @NumNfAlvara,
                    @ValorAlvara, @ValorConta,
                    @StatusConta, @Responsavel, @Observacoes
                )
            ", conta);
        }

        public void Atualizar(Conta conta)
        {
            using var conn = _db.GetConnection();

            conn.Execute(@"
                UPDATE contas SET
                    tipo_lancamento=@TipoLancamento,
                    historico=@Historico,
                    data_movimentacao=@DataMovimentacao,
                    mov_processo=@MovProcesso,
                    num_nf_alvara=@NumNfAlvara,
                    valor_alvara=@ValorAlvara,
                    valor_conta=@ValorConta,
                    observacoes=@Observacoes
                WHERE id=@Id
            ", conta);
        }

        public void Excluir(string id)
        {
            using var conn = _db.GetConnection();

            conn.Execute(
                "DELETE FROM contas WHERE id=@id",
                new { id });
        }

        public void FecharConta(string id)
        {
            using var conn = _db.GetConnection();

            conn.Execute(@"
                UPDATE contas
                SET status_conta='lancado'
                WHERE id=@id
            ", new { id });
        }
    }
}
