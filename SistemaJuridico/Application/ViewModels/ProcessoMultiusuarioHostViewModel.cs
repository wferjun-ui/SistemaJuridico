using System.Threading.Tasks;

namespace SistemaJuridico.ViewModels
{
    public class ProcessoMultiusuarioHostViewModel : ViewModelBase
    {
        public ProcessoEdicaoEstadoViewModel EstadoVm { get; }

        public ProcessoMultiusuarioHostViewModel(ProcessoEdicaoEstadoViewModel estadoVm)
        {
            EstadoVm = estadoVm;
        }

        public async Task CarregarAsync(int processoId)
        {
            await EstadoVm.AtualizarEstadoAsync(processoId);
        }
    }
}
