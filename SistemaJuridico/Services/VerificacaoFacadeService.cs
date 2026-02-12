using Newtonsoft.Json;
using SistemaJuridico.Infrastructure;
using SistemaJuridico.Models;

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
            var snapshot = JsonConvert.SerializeObject(itensAtuais);

            var verificacao = new Verificacao
            {
                Id = Guid.NewGuid().ToString(),
                ProcessoId = processoId,
                DataHora = DateTime.Now.ToString("o"),
                StatusProcesso = statusProcesso,
                Responsavel = responsavel,
                DiligenciaDescricao = descricao,
                ItensSnapshotJson = snapshot
            };

            _verificacaoService.Inserir(verificacao);
            _itemSaudeService.SubstituirItensProcesso(processoId, itensAtuais);
            _processService.AtualizarStatus(processoId, statusProcesso);
            _historicoService.Registrar(processoId, "Nova verificação registrada", statusProcesso);
        }

        public void CriarVerificacaoCompleta(
            string processoId,
            string statusProcesso,
            string responsavel,
            string descricao,
            List<ItemSaude> itensAtuais)
        {
            CriarVerificacao(processoId, statusProcesso, responsavel, descricao, itensAtuais);
        }
    }
}
