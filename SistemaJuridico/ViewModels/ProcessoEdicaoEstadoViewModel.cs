using SistemaJuridico.Models;
using SistemaJuridico.Services;
using System.Threading.Tasks;

namespace SistemaJuridico.ViewModels
{
    public class ProcessoEdicaoEstadoViewModel : ViewModelBase
    {
        private readonly ProcessoEdicaoEstadoService _estadoService;

        private bool _somenteLeitura;
        public bool SomenteLeitura
        {
            get => _somenteLeitura;
            set => SetProperty(ref _somenteLeitura, value);
        }

        private string _mensagemEstado;
        public string MensagemEstado
        {
            get => _mensagemEstado;
            set => SetProperty(ref _mensagemEstado, value);
        }

        public ProcessoEdicaoEstadoViewModel(ProcessoEdicaoEstadoService estadoService)
        {
            _estadoService = estadoService;
        }

        public async Task AtualizarEstadoAsync(int processoId)
        {
            var estado = await _estadoService.ObterEstadoAsync(processoId);

            if (estado == null)
            {
                SomenteLeitura = false;
                MensagemEstado = null;
                return;
            }

            if (estado.EmEdicaoPorOutroUsuario)
            {
                SomenteLeitura = true;
                MensagemEstado = $"Processo em edição por {estado.Usuario}";
            }
            else
            {
                SomenteLeitura = false;
                MensagemEstado = "Você possui o controle deste processo";
            }
        }
    }
}
