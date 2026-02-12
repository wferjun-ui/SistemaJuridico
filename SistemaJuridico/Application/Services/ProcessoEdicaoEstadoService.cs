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
            // STUB seguro até implementação real no facade
            try
            {
                return await _facade.ObterEstadoEdicaoAsync(processoId);
            }
            catch
            {
                return await Task.FromResult(new ProcessoLockInfo());
            }
        }
    }
}
