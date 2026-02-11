using Dapper;
using Newtonsoft.Json;
using SistemaJuridico.Models;
using System.Data;

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
            conn.Open();

            using var trx = conn.BeginTransaction();

            try
            {
                ImportarUsuarios(conn, trx, dados.usuarios);
                ImportarProcessos(conn, trx, dados.processos);
                ImportarContas(conn, trx, dados.contas);
                ImportarVerificacoes(conn, trx, dados.verificacoes);
                ImportarItensSaude(conn, trx, dados.itens_saude);
                ImportarReus(conn, trx, dados.reus);

                trx.Commit();
            }
            catch
            {
                trx.Rollback();
                throw;
            }
        }

        // =========================
        // USUÁRIOS
        // =========================

        private void ImportarUsuarios(IDbConnection conn, IDbTransaction trx, List<UsuarioMigration>? lista)
        {
            if (lista == null) return;

            foreach (var u in lista)
            {
                string salt = _db.GerarSalt();
                string hash = _db.HashSenha("123456", salt);

                conn.Execute(@"
INSERT OR IGNORE INTO usuarios
(id, email, username, password_hash, salt, perfil)
VALUES
(@id, @email, @username, @hash, @salt, @perfil)
",
                new
                {
                    u.id,
                    u.email,
                    u.username,
                    hash,
                    salt,
                    perfil = u.is_admin == 1 ? "Admin" : "Operador"
                }, trx);
            }
        }

        // =========================
        // PROCESSOS
        // =========================

        private void ImportarProcessos(IDbConnection conn, IDbTransaction trx, List<ProcessoMigration>? lista)
        {
            if (lista == null) return;

            foreach (var p in lista)
            {
                conn.Execute(@"
INSERT OR IGNORE INTO processos
(id, numero, paciente, juiz, classificacao,
 status_fase, ultima_atualizacao, observacao_fixa,
 situacao_rascunho)
VALUES
(@id, @numero, @paciente, @juiz, @classificacao,
 @status_fase, @ultima_atualizacao, @observacao_fixa,
 'Concluído')
",
                p, trx);
            }
        }

        // =========================
        // CONTAS
        // =========================

        private void ImportarContas(IDbConnection conn, IDbTransaction trx, List<ContaMigration>? lista)
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
",
                c, trx);
            }
        }

        // =========================
        // VERIFICAÇÕES
        // =========================

        private void ImportarVerificacoes(IDbConnection conn, IDbTransaction trx, List<VerificacaoMigration>? lista)
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
",
                v, trx);
            }
        }

        // =========================
        // ITENS DE SAÚDE
        // =========================

        private void ImportarItensSaude(IDbConnection conn, IDbTransaction trx, List<ItemSaudeMigration>? lista)
        {
            if (lista == null) return;

            foreach (var i in lista)
            {
                conn.Execute(@"
INSERT OR IGNORE INTO itens_saude
(id, processo_id, tipo, nome, qtd,
 frequencia, local, data_prescricao,
 is_desnecessario, tem_bloqueio)
VALUES
(@id, @processo_id, @tipo, @nome, @qtd,
 @frequencia, @local, @data_prescricao,
 @is_desnecessario, @tem_bloqueio)
",
                i, trx);
            }
        }

        // =========================
        // RÉUS
        // =========================

        private void ImportarReus(IDbConnection conn, IDbTransaction trx, List<ReuMigration>? lista)
        {
            if (lista == null) return;

            foreach (var r in lista)
            {
                conn.Execute(@"
INSERT OR IGNORE INTO reus
(id, processo_id, nome)
VALUES
(@id, @processo_id, @nome)
",
                r, trx);
            }
        }
    }
}
