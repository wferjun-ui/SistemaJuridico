using SistemaJuridico.Models;
using System.Threading.Tasks;

namespace SistemaJuridico.Services
{
    public class ProcessoEdicaoEstadoService
    {
        private readonly ProcessoFacadeService _facade;

        public ProcessoEdicaoEstadoService(ProcessoFacadeService facade)
        {
            _facade = facade;
        }

        public async Task<ProcessoLockInfo> ObterEstadoAsync(int processoId)
        {
            return await _facade.ObterEstadoEdicaoAsync(processoId);
        }
    }
}
