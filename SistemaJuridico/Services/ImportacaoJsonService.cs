using Dapper;
using Newtonsoft.Json;
using SistemaJuridico.Models;

namespace SistemaJuridico.Services
{
    public class ImportacaoJsonService
    {
        private readonly DatabaseService _db;

        public ImportacaoJsonService(DatabaseService db)
        {
            _db = db;
        }

        public void ImportarArquivo(string caminhoJson)
        {
            var json = File.ReadAllText(caminhoJson);

            var dados = JsonConvert.DeserializeObject<MigrationRoot>(json);

            using var conn = _db.GetConnection();
            using var trx = conn.BeginTransaction();

            try
            {
                ImportarUsuarios(conn, trx, dados.usuarios);
                ImportarProcessos(conn, trx, dados.processos);
                ImportarContas(conn, trx, dados.contas);
                ImportarVerificacoes(conn, trx, dados.verificacoes);

                trx.Commit();
            }
            catch
            {
                trx.Rollback();
                throw;
            }
        }

        private void ImportarUsuarios(IDbConnection conn, IDbTransaction trx, List<UsuarioMigration> lista)
        {
            if (lista == null) return;

            foreach (var u in lista)
            {
                conn.Execute(@"
                    INSERT OR IGNORE INTO usuarios
                    (id, email, username, is_admin)
                    VALUES (@id, @email, @username, @is_admin)
                ", u, trx);
            }
        }

        private void ImportarProcessos(IDbConnection conn, IDbTransaction trx, List<ProcessoMigration> lista)
        {
            if (lista == null) return;

            foreach (var p in lista)
            {
                conn.Execute(@"
                    INSERT OR IGNORE INTO processos
                    (id, numero, paciente, juiz, classificacao,
                     status_fase, ultima_atualizacao, observacao_fixa)
                    VALUES
                    (@id, @numero, @paciente, @juiz, @classificacao,
                     @status_fase, @ultima_atualizacao, @observacao_fixa)
                ", p, trx);
            }
        }

        private void ImportarContas(IDbConnection conn, IDbTransaction trx, List<ContaMigration> lista)
        {
            if (lista == null) return;

            foreach (var c in lista)
            {
                conn.Execute(@"
                    INSERT OR IGNORE INTO contas
                    (id, processo_id, data_movimentacao, tipo_lancamento,
                     historico, mov_processo, num_nf_alvara,
                     valor_alvara, valor_conta, observacoes,
                     responsavel, status_conta)
                    VALUES
                    (@id, @processo_id, @data_movimentacao, @tipo_lancamento,
                     @historico, @mov_processo, @num_nf_alvara,
                     @valor_alvara, @valor_conta, @observacoes,
                     @responsavel, @status_conta)
                ", c, trx);
            }
        }

        private void ImportarVerificacoes(IDbConnection conn, IDbTransaction trx, List<VerificacaoMigration> lista)
        {
            if (lista == null) return;

            foreach (var v in lista)
            {
                conn.Execute(@"
                    INSERT OR IGNORE INTO verificacoes
                    (id, processo_id, data_hora, status_processo,
                     responsavel, diligencia_pendente,
                     pendencias_descricao)
                    VALUES
                    (@id, @processo_id, @data_hora, @status_processo,
                     @responsavel, @diligencia_pendente,
                     @pendencias_descricao)
                ", v, trx);
            }
        }
    }
}
