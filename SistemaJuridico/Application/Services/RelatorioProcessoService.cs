using SistemaJuridico.Models;

namespace SistemaJuridico.Services
{
    public class RelatorioProcessoService
    {
        private readonly ProcessService _processService;
        private readonly ContaService _contaService;
        private readonly DiligenciaService _diligenciaService;
        private readonly ItemSaudeService _itemSaudeService;
        private readonly VerificacaoService _verificacaoService;

        public RelatorioProcessoService(DatabaseService db)
        {
            _processService = new ProcessService(db);
            _contaService = new ContaService(db);
            _diligenciaService = new DiligenciaService(db);
            _itemSaudeService = new ItemSaudeService(db);
            _verificacaoService = new VerificacaoService(db);
        }

        public RelatorioProcessoModel GerarModelo(string processoId)
        {
            var processo = _processService
                .ListarProcessos()
                .First(x => x.Id == processoId);

            return new RelatorioProcessoModel
            {
                Processo = processo,
                ItensSaude = _itemSaudeService.ListarPorProcesso(processoId),
                Contas = _contaService.ListarPorProcesso(processoId),
                Diligencias = _diligenciaService.ListarPorProcesso(processoId),
                Verificacoes = _verificacaoService.ListarPorProcesso(processoId),
                UsuarioGerador = App.Session.UsuarioAtual?.Nome ?? "Sistema"
            };
        }
    }
}
