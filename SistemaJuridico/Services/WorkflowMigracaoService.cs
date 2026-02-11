using System.Text;

namespace SistemaJuridico.Services
{
    public class ResultadoMigracao
    {
        public bool Sucesso { get; set; }
        public string Relatorio { get; set; } = "";
        public string MensagemErro { get; set; } = "";
    }

    public class WorkflowMigracaoService
    {
        private readonly ImportacaoJsonService _importador;
        private readonly ValidacaoMigracaoService _validador;

        public WorkflowMigracaoService(
            ImportacaoJsonService importador,
            ValidacaoMigracaoService validador)
        {
            _importador = importador;
            _validador = validador;
        }

        public ResultadoMigracao ExecutarMigracao(string caminhoJson)
        {
            var resultado = new ResultadoMigracao();
            var relatorio = new StringBuilder();

            try
            {
                relatorio.AppendLine("===== INÍCIO DA MIGRAÇÃO =====");
                relatorio.AppendLine();

                relatorio.AppendLine("Importando dados...");
                _importador.Importar(caminhoJson);
                relatorio.AppendLine("✔ Importação concluída");
                relatorio.AppendLine();

                relatorio.AppendLine("Executando validação...");
                var validacao = _validador.Validar(caminhoJson);

                relatorio.AppendLine(validacao);

                resultado.Sucesso = !validacao.Contains("❌");
                resultado.Relatorio = relatorio.ToString();
            }
            catch (Exception ex)
            {
                resultado.Sucesso = false;
                resultado.MensagemErro = ex.Message;

                relatorio.AppendLine("❌ ERRO DURANTE MIGRAÇÃO");
                relatorio.AppendLine(ex.ToString());

                resultado.Relatorio = relatorio.ToString();
            }

            return resultado;
        }
    }
}
