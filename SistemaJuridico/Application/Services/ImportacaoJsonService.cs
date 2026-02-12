using Dapper;
using Newtonsoft.Json;
using SistemaJuridico.Models;

namespace SistemaJuridico.Services
{
    public class ImportacaoJsonService
    {
        private readonly DatabaseService _databaseService;

        public ImportacaoJsonService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public void ImportarArquivo(string caminhoJson)
        {
            Importar(caminhoJson);
        }

        public void Importar(string caminhoJson)
        {
            var json = File.ReadAllText(caminhoJson);
            var root = JsonConvert.DeserializeObject<MigracaoRoot>(json)
                       ?? throw new InvalidOperationException("Arquivo de migração inválido.");

            using var conn = _databaseService.GetConnection();
            conn.Open();
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

        private static void ImportarUsuarios(Microsoft.Data.Sqlite.SqliteConnection conn, List<MigracaoUsuario>? usuarios)
        {
            if (usuarios is null || usuarios.Count == 0) return;

            const string sql = @"
INSERT OR REPLACE INTO usuarios(id, username, email, perfil, password_hash, salt)
VALUES(@id, @username, @email, @perfil, @password_hash, @salt);";

            conn.Execute(sql, usuarios.Select(u => new
            {
                u.id,
                u.username,
                u.email,
                perfil = u.is_admin == 1 ? "Admin" : "Usuario",
                u.password_hash,
                u.salt
            }));
        }

        private static void ImportarProcessos(Microsoft.Data.Sqlite.SqliteConnection conn, List<MigracaoProcesso>? processos)
        {
            if (processos is null || processos.Count == 0) return;

            const string sql = @"
INSERT OR REPLACE INTO processos(id, numero, paciente, juiz, classificacao, status_fase, ultima_atualizacao, observacao_fixa)
VALUES(@id, @numero, @paciente, @juiz, @classificacao, @status_fase, @ultima_atualizacao, @observacao_fixa);";

            conn.Execute(sql, processos);
        }

        private static void ImportarItensSaude(Microsoft.Data.Sqlite.SqliteConnection conn, List<MigracaoItemSaude>? itens)
        {
            if (itens is null || itens.Count == 0) return;

            const string sql = @"
INSERT OR REPLACE INTO itens_saude(id, processo_id, tipo, nome, qtd, frequencia, local, data_prescricao, is_desnecessario, tem_bloqueio)
VALUES(@id, @processo_id, @tipo, @nome, @qtd, @frequencia, @local, @data_prescricao, @is_desnecessario, @tem_bloqueio);";

            conn.Execute(sql, itens);
        }

        private static void ImportarVerificacoes(Microsoft.Data.Sqlite.SqliteConnection conn, List<MigracaoVerificacao>? verificacoes)
        {
            if (verificacoes is null || verificacoes.Count == 0) return;

            const string sql = @"
INSERT OR REPLACE INTO verificacoes(
    id, processo_id, data_hora, status_processo, responsavel, diligencia_pendente,
    pendencias_descricao, diligencia_realizada, diligencia_descricao, prazo_diligencia,
    proximo_prazo_padrao, data_notificacao, alteracoes_texto, itens_snapshot_json)
VALUES(
    @id, @processo_id, @data_hora, @status_processo, @responsavel, @diligencia_pendente,
    @pendencias_descricao, @diligencia_realizada, @diligencia_descricao, @prazo_diligencia,
    @proximo_prazo_padrao, @data_notificacao, @alteracoes_texto, @itens_snapshot_json);";

            conn.Execute(sql, verificacoes);
        }

        private static void ImportarContas(Microsoft.Data.Sqlite.SqliteConnection conn, List<MigracaoConta>? contas)
        {
            if (contas is null || contas.Count == 0) return;

            const string sql = @"
INSERT OR REPLACE INTO contas(
    id, processo_id, data_movimentacao, tipo_lancamento, historico, valor_conta,
    responsavel, status_conta, mov_processo, num_nf_alvara, valor_alvara,
    terapia_medicamento_nome, quantidade, mes_referencia, ano_referencia, observacoes)
VALUES(
    @id, @processo_id, @data_movimentacao, @tipo_lancamento, @historico, @valor_conta,
    @responsavel, @status_conta, @mov_processo, @num_nf_alvara, @valor_alvara,
    @terapia_medicamento_nome, @quantidade, @mes_referencia, @ano_referencia, @observacoes);";

            conn.Execute(sql, contas);
        }
    }
}
