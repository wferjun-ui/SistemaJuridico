using Dapper;
using Newtonsoft.Json;
using SistemaJuridico.Models;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Text;

namespace SistemaJuridico.Services
{
    public class ValidacaoMigracaoService
    {
        private readonly DatabaseService _databaseService;

        public ValidacaoMigracaoService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public string Validar(string caminhoJson)
        {
            var relatorio = new StringBuilder();

            var json = File.ReadAllText(caminhoJson);
            var root = JsonConvert.DeserializeObject<MigracaoRoot>(json) ?? new MigracaoRoot();

            using var conn = _databaseService.GetConnection();

            relatorio.AppendLine("===== VALIDAÇÃO DE MIGRAÇÃO =====");
            relatorio.AppendLine();

            ValidarContagem(conn, root, relatorio);
            ValidarIntegridade(conn, relatorio);
            ValidarCamposCriticos(conn, relatorio);

            relatorio.AppendLine();
            relatorio.AppendLine("===== FIM DA VALIDAÇÃO =====");

            return relatorio.ToString();
        }

        private void ValidarContagem(SqliteConnection conn, MigracaoRoot root, StringBuilder relatorio)
        {
            relatorio.AppendLine(">> Contagem de Registros");

            ValidarTabela(conn, "usuarios", root.usuarios?.Count ?? 0, relatorio);
            ValidarTabela(conn, "processos", root.processos?.Count ?? 0, relatorio);
            ValidarTabela(conn, "itens_saude", root.itens_saude?.Count ?? 0, relatorio);
            ValidarTabela(conn, "verificacoes", root.verificacoes?.Count ?? 0, relatorio);
            ValidarTabela(conn, "contas", root.contas?.Count ?? 0, relatorio);

            relatorio.AppendLine();
        }

        private void ValidarTabela(SqliteConnection conn, string tabela, int esperado, StringBuilder relatorio)
        {
            var atual = conn.ExecuteScalar<int>($"SELECT COUNT(*) FROM {tabela}");

            if (esperado == atual)
                relatorio.AppendLine($"✔ {tabela}: OK ({atual})");
            else
                relatorio.AppendLine($"❌ {tabela}: JSON={esperado} / DB={atual}");
        }

        private void ValidarIntegridade(SqliteConnection conn, StringBuilder relatorio)
        {
            relatorio.AppendLine(">> Integridade Relacional");

            var verificacoesOrfas = conn.ExecuteScalar<int>(@"
                SELECT COUNT(*) FROM verificacoes v
                LEFT JOIN processos p ON p.id = v.processo_id
                WHERE p.id IS NULL");

            var contasOrfas = conn.ExecuteScalar<int>(@"
                SELECT COUNT(*) FROM contas c
                LEFT JOIN processos p ON p.id = c.processo_id
                WHERE p.id IS NULL");

            var itensOrfas = conn.ExecuteScalar<int>(@"
                SELECT COUNT(*) FROM itens_saude i
                LEFT JOIN processos p ON p.id = i.processo_id
                WHERE p.id IS NULL");

            relatorio.AppendLine(verificacoesOrfas == 0
                ? "✔ Verificações sem órfãos"
                : $"❌ Verificações órfãs: {verificacoesOrfas}");

            relatorio.AppendLine(contasOrfas == 0
                ? "✔ Contas sem órfãos"
                : $"❌ Contas órfãs: {contasOrfas}");

            relatorio.AppendLine(itensOrfas == 0
                ? "✔ Itens saúde sem órfãos"
                : $"❌ Itens saúde órfãos: {itensOrfas}");

            relatorio.AppendLine();
        }

        private void ValidarCamposCriticos(SqliteConnection conn, StringBuilder relatorio)
        {
            relatorio.AppendLine(">> Campos Críticos");

            var processosSemNumero = conn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM processos WHERE numero IS NULL OR numero = ''");

            var verificacoesSemData = conn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM verificacoes WHERE data_hora IS NULL OR data_hora = ''");

            var contasValorInvalido = conn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM contas WHERE valor_conta < 0");

            relatorio.AppendLine(processosSemNumero == 0
                ? "✔ Processos com número válido"
                : $"❌ Processos sem número: {processosSemNumero}");

            relatorio.AppendLine(verificacoesSemData == 0
                ? "✔ Verificações com data"
                : $"❌ Verificações sem data: {verificacoesSemData}");

            relatorio.AppendLine(contasValorInvalido == 0
                ? "✔ Contas com valores válidos"
                : $"❌ Contas com valor inválido: {contasValorInvalido}");

            relatorio.AppendLine();
        }
    }
}
