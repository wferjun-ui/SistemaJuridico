using System.Threading.Tasks;

namespace SistemaJuridico.ViewModels
{
    public class ProcessoTimelineHostViewModel : ViewModelBase
    {
        public TimelineViewModel InnerTimeline { get; }

        public ProcessoTimelineHostViewModel(TimelineViewModel timelineVm)
        {
            InnerTimeline = timelineVm;
        }

        public async Task CarregarAsync(int processoId)
        {
            await InnerTimeline.CarregarAsync(processoId);
        }
    }
}
