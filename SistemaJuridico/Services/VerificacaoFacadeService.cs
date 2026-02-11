using Newtonsoft.Json;
using SistemaJuridico.Models;
using SistemaJuridico.Infrastructure;

namespace SistemaJuridico.Services
{
    public class VerificacaoFacadeService
    {
        private readonly VerificacaoService _verificacaoService;
        private readonly ItemSaudeService _itemSaudeService;
        private readonly HistoricoService _historicoService;
        private readonly ProcessService _processService;

        public VerificacaoFacadeService()
        {
            _verificacaoService = ServiceLocator.VerificacaoService;
            _itemSaudeService = ServiceLocator.ItemSaudeService;
            _historicoService = ServiceLocator.HistoricoService;
            _processService = ServiceLocator.ProcessService;
        }

        public void CriarVerificacao(
            string processoId,
            string statusProcesso,
            string responsavel,
            string descricao,
            List<ItemSaude> itensAtuais)
        {
            // =========================
            // SNAPSHOT
            // =========================

            var snapshot = JsonConvert.SerializeObject(itensAtuais);

            var verificacao = new Verificacao
            {
                Id = Guid.NewGuid().ToString(),
                ProcessoId = processoId,
                DataHora = DateTime.Now,
                StatusProcesso = statusProcesso,
                Responsavel = responsavel,
                DiligenciaDescricao = descricao,
                ItensSnapshotJson = snapshot
            };

            // =========================
            // SALVAR VERIFICAÇÃO
            // =========================

            _verificacaoService.Inserir(verificacao);

            // =========================
            // ATUALIZAR ITENS ATUAIS
            // =========================

            _itemSaudeService.SubstituirItensProcesso(
                processoId,
                itensAtuais);

            // =========================
            // ATUALIZAR STATUS PROCESSO
            // =========================

            _processService.AtualizarStatus(
                processoId,
                statusProcesso);

            // =========================
            // HISTÓRICO
            // =========================

            _historicoService.Registrar(
                processoId,
                "Nova verificação registrada",
                statusProcesso);
        }
    }
}
