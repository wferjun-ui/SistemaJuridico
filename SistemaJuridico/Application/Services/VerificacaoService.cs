using Dapper;
using SistemaJuridico.Models;

namespace SistemaJuridico.Services
{
    public class VerificacaoService
    {
        private readonly DatabaseService _db;

        public VerificacaoService(DatabaseService db)
        {
            _db = db;
        }

        public List<Verificacao> ListarPorProcesso(string processoId)
        {
            using var conn = _db.GetConnection();
            return conn.Query<Verificacao>(@"
SELECT
  id as Id,
  processo_id as ProcessoId,
  data_hora as DataHora,
  status_processo as StatusProcesso,
  responsavel as Responsavel,
  diligencia_pendente as DiligenciaPendente,
  pendencias_descricao as PendenciaDescricao,
  diligencia_realizada as DiligenciaRealizada,
  diligencia_status as DiligenciaStatus,
  prazo_diligencia as PrazoDiligencia,
  proximo_prazo_padrao as ProximoPrazo,
  proxima_verificacao as ProximaVerificacao,
  data_notificacao as DataNotificacao,
  descricao_persistente as DescricaoPersistente,
  alteracoes_texto as AlteracoesTexto,
  diligencia_descricao as DiligenciaDescricao,
  itens_snapshot_json as ItensSnapshotJson
FROM verificacoes
WHERE processo_id = @processoId
ORDER BY data_hora DESC
", new { processoId }).ToList();
        }

        public void Inserir(Verificacao verificacao)
        {
            using var conn = _db.GetConnection();
            conn.Execute(@"
INSERT INTO verificacoes
(id, processo_id, data_hora, status_processo, responsavel, diligencia_pendente, pendencias_descricao, diligencia_realizada, diligencia_descricao, diligencia_status, prazo_diligencia, proximo_prazo_padrao, proxima_verificacao, data_notificacao, descricao_persistente, alteracoes_texto, itens_snapshot_json)
VALUES
(@Id, @ProcessoId, @DataHora, @StatusProcesso, @Responsavel, @DiligenciaPendente, @PendenciaDescricao, @DiligenciaRealizada, @DiligenciaDescricao, @DiligenciaStatus, @PrazoDiligencia, @ProximoPrazo, @ProximaVerificacao, @DataNotificacao, @DescricaoPersistente, @AlteracoesTexto, @ItensSnapshotJson)", verificacao);
        }

        public void Excluir(string verificacaoId)
        {
            if (string.IsNullOrWhiteSpace(verificacaoId))
                return;

            using var conn = _db.GetConnection();
            conn.Execute("DELETE FROM verificacoes WHERE id = @id", new { id = verificacaoId });
        }

    }
}
