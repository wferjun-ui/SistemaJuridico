using SistemaJuridico.Models;
using SistemaJuridico.Services;
using SistemaJuridico.Views;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace SistemaJuridico.ViewModels
{
    public class ProcessoListViewModel : ViewModelBase
    {
        private readonly ProcessoFacadeService _processoService;

        public ObservableCollection<Processo> Processos { get; } = new();

        public RelayCommand CarregarCommand { get; }
        public RelayCommand<Processo> AbrirProcessoCommand { get; }

        public ProcessoListViewModel(ProcessoFacadeService processoService)
        {
            _processoService = processoService;

            CarregarCommand = new RelayCommand(async () => await Carregar());
            AbrirProcessoCommand = new RelayCommand<Processo>(AbrirProcesso);
        }

        private async Task Carregar()
        {
            Processos.Clear();

            var lista = await _processoService.ListarProcessosAsync();

            foreach (var p in lista)
                Processos.Add(p);
        }

        private void AbrirProcesso(Processo processo)
        {
            if (processo == null)
                return;

            var window = new ProcessoDetalhesWindow(processo.Id);
            window.Show();
        }
    }
}
