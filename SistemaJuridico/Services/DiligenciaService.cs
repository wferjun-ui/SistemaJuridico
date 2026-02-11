using Dapper;
using SistemaJuridico.Models;

namespace SistemaJuridico.Services
{
    public class DiligenciaService
    {
        private readonly DatabaseService _db;

        public DiligenciaService(DatabaseService db)
        {
            _db = db;
        }

        // =========================
        // LISTAR POR PROCESSO
        // =========================

        public List<Diligencia> ListarPorProcesso(string processoId)
        {
            using var conn = _db.GetConnection();

            return conn.Query<Diligencia>(@"
                SELECT
                    id as Id,
                    processo_id as ProcessoId,
                    descricao as Descricao,
                    data_criacao as DataCriacao,
                    data_conclusao as DataConclusao,
                    concluida as Concluida,
                    responsavel as Responsavel
                FROM diligencias
                WHERE processo_id=@id
                ORDER BY data_criacao DESC
            ", new { id = processoId }).ToList();
        }

        // =========================
        // INSERIR
        // =========================

        public void Inserir(Diligencia diligencia)
        {
            using var conn = _db.GetConnection();

            conn.Execute(@"
                INSERT INTO diligencias (
                    id,
                    processo_id,
                    descricao,
                    data_criacao,
                    concluida,
                    responsavel
                )
                VALUES (
                    @Id,
                    @ProcessoId,
                    @Descricao,
                    @DataCriacao,
                    @Concluida,
                    @Responsavel
                )
            ", diligencia);
        }

        // =========================
        // CONCLUIR DILIGÊNCIA
        // =========================

        public void Concluir(string id)
        {
            using var conn = _db.GetConnection();

            conn.Execute(@"
                UPDATE diligencias
                SET concluida = 1,
                    data_conclusao = @data
                WHERE id=@id
            ",
            new
            {
                id,
                data = DateTime.Now.ToString("dd/MM/yyyy")
            });
        }

        // =========================
        // REABRIR
        // =========================

        public void Reabrir(string id)
        {
            using var conn = _db.GetConnection();

            conn.Execute(@"
                UPDATE diligencias
                SET concluida = 0,
                    data_conclusao = NULL
                WHERE id=@id
            ", new { id });
        }

        // =========================
        // EXCLUIR
        // =========================

        public void Excluir(string id)
        {
            using var conn = _db.GetConnection();

            conn.Execute(
                "DELETE FROM diligencias WHERE id=@id",
                new { id });
        }

        // =========================
        // VERIFICAR SE EXISTE PENDÊNCIA
        // =========================

        public bool ExistePendencia(string processoId)
        {
            using var conn = _db.GetConnection();

            var total = conn.ExecuteScalar<int>(@"
                SELECT COUNT(*)
                FROM diligencias
                WHERE processo_id=@id
                AND concluida = 0
            ", new { id = processoId });

            return total > 0;
        }
    }
}
