using SistemaJuridico.Models;
using SistemaJuridico.Services;
using System.Threading.Tasks;

namespace SistemaJuridico.ViewModels
{
    public class ProcessoDetalhesHostViewModel : ViewModelBase
    {
        private readonly ProcessoFacadeService _facade;

        public ProcessoDetalhesViewModel InnerViewModel { get; }

        public ProcessoDetalhesHostViewModel(
            ProcessoFacadeService facade,
            ProcessoDetalhesViewModel innerVm)
        {
            _facade = facade;
            InnerViewModel = innerVm;
        }

        public async Task CarregarProcessoAsync(int processoId)
        {
            var processo = await _facade.ObterProcessoCompletoAsync(processoId);

            await InnerViewModel.CarregarAsync(processo);
        }
    }
}
