using Dapper;
using Newtonsoft.Json;
using SistemaJuridico.Models;
using System.Data.SQLite;

namespace SistemaJuridico.Services
{
    public class ImportacaoJsonService
    {
        private readonly DatabaseService _databaseService;

        public ImportacaoJsonService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public void Importar(string caminhoJson)
        {
            var json = File.ReadAllText(caminhoJson);
            var root = JsonConvert.DeserializeObject<MigracaoRoot>(json);

            using var conn = _databaseService.GetConnection();
            using var trans = conn.BeginTransaction();

            try
            {
                ImportarUsuarios(conn, root.usuarios);
                ImportarProcessos(conn, root.processos);
                ImportarItensSaude(conn, root.itens_saude);
                ImportarVerificacoes(conn, root.verificacoes);
                ImportarContas(conn, root.contas);

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        private void ImportarUsuarios(SQLiteConnection conn, List<MigracaoUsuario> lista)
        {
            foreach (var u in lista)
            {
                var username = string.IsNullOrWhiteSpace(u.username)
                    ? u.email.Split('@')[0]
                    : u.username;

                conn.Execute(@"
                    INSERT OR IGNORE INTO usuarios
                    (id, username, email, password_hash, salt, is_admin)
                    VALUES (@id, @username, @email, @password_hash, @salt, @is_admin)",
                    new
                    {
                        u.id,
                        username,
                        u.email,
                        u.password_hash,
                        u.salt,
                        is_admin = u.is_admin == 1 ? 1 : 0
                    });
            }
        }

        private void ImportarProcessos(SQLiteConnection conn, List<MigracaoProcesso> lista)
        {
            foreach (var p in lista)
            {
                conn.Execute(@"
                    INSERT OR IGNORE INTO processos
                    (id, numero, paciente, is_antigo, juiz, genitor_rep_nome,
                     genitor_rep_tipo, classificacao, status_fase,
                     ultima_atualizacao, observacao_fixa)
                    VALUES (@id, @numero, @paciente, @is_antigo, @juiz,
                            @genitor_rep_nome, 'Genitor', @classificacao,
                            @status_fase, @ultima_atualizacao, @observacao_fixa)",
                    p);
            }
        }

        private void ImportarItensSaude(SQLiteConnection conn, List<MigracaoItemSaude> lista)
        {
            foreach (var i in lista)
            {
                conn.Execute(@"
                    INSERT OR IGNORE INTO itens_saude
                    (id, processo_id, tipo, nome, qtd, frequencia, local,
                     data_prescricao, is_desnecessario, tem_bloqueio)
                    VALUES (@id, @processo_id, @tipo, @nome, @qtd,
                            @frequencia, @local, @data_prescricao,
                            @is_desnecessario, @tem_bloqueio)", i);
            }
        }

        private void ImportarVerificacoes(SQLiteConnection conn, List<MigracaoVerificacao> lista)
        {
            foreach (var v in lista)
            {
                var diligenciaRealizada = v.diligencia_realizada == 1 ? 1 : 0;
                var diligenciaPendente = v.diligencia_pendente == 1 ? 1 : 0;

                var alteracoes = string.IsNullOrWhiteSpace(v.alteracoes_texto)
                    ? "Registro de histórico."
                    : v.alteracoes_texto;

                conn.Execute(@"
                    INSERT OR IGNORE INTO verificacoes
                    (id, processo_id, data_hora, status_processo,
                     responsavel, diligencia_realizada,
                     diligencia_descricao, diligencia_pendente,
                     pendencias_descricao, prazo_diligencia,
                     proximo_prazo_padrao, data_notificacao,
                     alteracoes_texto, itens_snapshot_json)
                    VALUES (@id, @processo_id, @data_hora, @status_processo,
                            @responsavel, @diligencia_realizada,
                            @diligencia_descricao, @diligencia_pendente,
                            @pendencias_descricao, @prazo_diligencia,
                            @proximo_prazo_padrao, @data_notificacao,
                            @alteracoes_texto, @itens_snapshot_json)",
                    new
                    {
                        v.id,
                        v.processo_id,
                        v.data_hora,
                        v.status_processo,
                        v.responsavel,
                        diligencia_realizada = diligenciaRealizada,
                        v.diligencia_descricao,
                        diligencia_pendente = diligenciaPendente,
                        v.pendencias_descricao,
                        v.prazo_diligencia,
                        v.proximo_prazo_padrao,
                        v.data_notificacao,
                        alteracoes_texto = alteracoes,
                        v.itens_snapshot_json
                    });
            }
        }

        private void ImportarContas(SQLiteConnection conn, List<MigracaoConta> lista)
        {
            foreach (var c in lista)
            {
                var movProcesso = (c.mov_processo ?? "").Replace("'", "");
                var numAlvara = (c.num_nf_alvara ?? "").Replace("'", "");

                conn.Execute(@"
                    INSERT OR IGNORE INTO contas
                    (id, processo_id, data_movimentacao, tipo_lancamento,
                     historico, mov_processo, num_nf_alvara,
                     valor_alvara, valor_conta, terapia_medicamento_nome,
                     quantidade, mes_referencia, ano_referencia,
                     observacoes, responsavel, status_conta)
                    VALUES (@id, @processo_id, @data_movimentacao,
                            @tipo_lancamento, @historico, @mov_processo,
                            @num_nf_alvara, @valor_alvara, @valor_conta,
                            @terapia_medicamento_nome, @quantidade,
                            @mes_referencia, @ano_referencia,
                            @observacoes, @responsavel, 'lancado')",
                    new
                    {
                        c.id,
                        c.processo_id,
                        c.data_movimentacao,
                        c.tipo_lancamento,
                        c.historico,
                        mov_processo = movProcesso,
                        num_nf_alvara = numAlvara,
                        c.valor_alvara,
                        c.valor_conta,
                        c.terapia_medicamento_nome,
                        c.quantidade,
                        c.mes_referencia,
                        c.ano_referencia,
                        c.observacoes,
                        c.responsavel
                    });
            }
        }
    }
}
