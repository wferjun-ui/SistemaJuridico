namespace SistemaJuridico.ViewModels
{
    public class ProcessoEditorViewModel : ViewModelBase
    {
        public ProcessoDetalhesViewModel DetalhesVm { get; }
        public ProcessoTimelineHostViewModel TimelineVm { get; }
        public ProcessoMultiusuarioHostViewModel MultiusuarioVm { get; }

        public ProcessoEditorViewModel(
            ProcessoDetalhesViewModel detalhesVm,
            ProcessoTimelineHostViewModel timelineVm,
            ProcessoMultiusuarioHostViewModel multiusuarioVm)
        {
            DetalhesVm = detalhesVm;
            TimelineVm = timelineVm;
            MultiusuarioVm = multiusuarioVm;
        }

        public async Task CarregarAsync(int processoId)
        {
            await DetalhesVm.CarregarAsync(processoId);
            await TimelineVm.CarregarAsync(processoId);
            await MultiusuarioVm.CarregarAsync(processoId);
        }
    }
}
