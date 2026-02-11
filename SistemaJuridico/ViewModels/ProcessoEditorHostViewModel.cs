namespace SistemaJuridico.ViewModels
{
    public class ProcessoEditorHostViewModel : ViewModelBase
    {
        public ProcessoEditorViewModel EditorVm { get; }

        public ProcessoEditorHostViewModel(ProcessoEditorViewModel editorVm)
        {
            EditorVm = editorVm;
        }

        public async Task CarregarAsync(int processoId)
        {
            await EditorVm.CarregarAsync(processoId);
        }
    }
}
