using SistemaJuridico.Infrastructure;
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
        private readonly LoggerService _logger = new();

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
                _logger.Audit("Início da migração");

                relatorio.AppendLine("===== INÍCIO DA MIGRAÇÃO =====");
                relatorio.AppendLine();

                _logger.Info("Importando dados JSON");

                _importador.Importar(caminhoJson);

                relatorio.AppendLine("✔ Importação concluída");

                _logger.Info("Executando validação");

                var validacao = _validador.Validar(caminhoJson);

                relatorio.AppendLine(validacao);

                resultado.Sucesso = !validacao.Contains("❌");
                resultado.Relatorio = relatorio.ToString();

                if (resultado.Sucesso)
                    _logger.Audit("Migração concluída com sucesso");
                else
                    _logger.Warn("Migração concluída com inconsistências");
            }
            catch (Exception ex)
            {
                _logger.Error("Erro durante migração", ex);

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
