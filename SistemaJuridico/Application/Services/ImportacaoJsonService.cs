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

        public void ImportarArquivo(string caminhoJson)
        {
            Importar(caminhoJson);
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
