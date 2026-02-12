using Dapper;
using SistemaJuridico.Models;
using System.Data;

namespace SistemaJuridico.Services
{
    public class ItemSaudeService
    {
        private readonly DatabaseService _database;

        public ItemSaudeService(DatabaseService database)
        {
            _database = database;
        }

        public List<ItemSaude> ListarPorProcesso(string processoId)
        {
            using var conn = _database.CreateConnection();

            return conn.Query<ItemSaude>(
                @"SELECT * 
                  FROM itens_saude 
                  WHERE processo_id = @ProcessoId
                  ORDER BY tipo, nome",
                new { ProcessoId = processoId }
            ).ToList();
        }

        public ItemSaude? ObterPorId(string id)
        {
            using var conn = _database.CreateConnection();

            return conn.QueryFirstOrDefault<ItemSaude>(
                @"SELECT * 
                  FROM itens_saude 
                  WHERE id = @Id",
                new { Id = id });
        }

        public void Inserir(ItemSaude item)
        {
            using var conn = _database.CreateConnection();

            if (string.IsNullOrWhiteSpace(item.Id))
                item.Id = Guid.NewGuid().ToString();

            conn.Execute(
                @"INSERT INTO itens_saude (
                        id,
                        processo_id,
                        tipo,
                        nome,
                        qtd,
                        frequencia,
                        local,
                        data_prescricao,
                        is_desnecessario,
                        tem_bloqueio
                  )
                  VALUES (
                        @Id,
                        @ProcessoId,
                        @Tipo,
                        @Nome,
                        @Qtd,
                        @Frequencia,
                        @Local,
                        @DataPrescricao,
                        @IsDesnecessario,
                        @TemBloqueio
                  )",
                item);
        }

        public void Atualizar(ItemSaude item)
        {
            using var conn = _database.CreateConnection();

            conn.Execute(
                @"UPDATE itens_saude SET
                        tipo = @Tipo,
                        nome = @Nome,
                        qtd = @Qtd,
                        frequencia = @Frequencia,
                        local = @Local,
                        data_prescricao = @DataPrescricao,
                        is_desnecessario = @IsDesnecessario,
                        tem_bloqueio = @TemBloqueio
                  WHERE id = @Id",
                item);
        }

        public void Excluir(string id)
        {
            using var conn = _database.CreateConnection();

            conn.Execute(
                @"DELETE FROM itens_saude 
                  WHERE id = @Id",
                new { Id = id });
        }

        public void MarcarDesnecessario(string id, bool valor)
        {
            using var conn = _database.CreateConnection();

            conn.Execute(
                @"UPDATE itens_saude 
                  SET is_desnecessario = @Valor
                  WHERE id = @Id",
                new { Id = id, Valor = valor ? 1 : 0 });
        }

        public void DefinirBloqueio(string id, bool bloqueado)
        {
            using var conn = _database.CreateConnection();

            conn.Execute(
                @"UPDATE itens_saude 
                  SET tem_bloqueio = @Valor
                  WHERE id = @Id",
                new { Id = id, Valor = bloqueado ? 1 : 0 });
        }

        public void SubstituirItensProcesso(string processoId, List<ItemSaude> itens)
        {
            using var conn = _database.CreateConnection();
            conn.Execute("DELETE FROM itens_saude WHERE processo_id = @ProcessoId", new { ProcessoId = processoId });

            foreach (var item in itens)
            {
                item.ProcessoId = processoId;
                Inserir(item);
            }
        }
    }
}
