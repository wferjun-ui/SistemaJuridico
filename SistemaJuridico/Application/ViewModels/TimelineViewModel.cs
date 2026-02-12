using SistemaJuridico.Models;
using SistemaJuridico.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace SistemaJuridico.ViewModels
{
    public class TimelineViewModel : ViewModelBase
    {
        private readonly TimelineService _timelineService;

        public ObservableCollection<TimelineEventoDTO> Eventos { get; } = new();

        public TimelineViewModel(TimelineService timelineService)
        {
            _timelineService = timelineService;
        }

        public Task CarregarAsync(int processoId)
        {
            return CarregarAsync(processoId.ToString());
        }

        public async Task CarregarAsync(string processoId)
        {
            Eventos.Clear();

            var eventos = await _timelineService.ObterTimelineAsync(processoId);

            foreach (var e in eventos)
                Eventos.Add(e);
        }
    }
}
