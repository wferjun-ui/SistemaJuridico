using SistemaJuridico.Infrastructure;
using SistemaJuridico.Models;
using SistemaJuridico.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace SistemaJuridico.ViewModels
{
    public class ProcessoListViewModel : ViewModelBase
    {
        private readonly ProcessoFacadeService _processoService;
        private readonly NavigationCoordinatorService _navigator;

        public ObservableCollection<Processo> Processos { get; } = new();

        public RelayCommand CarregarCommand { get; }
        public RelayCommand<Processo> AbrirProcessoCommand { get; }

        public ProcessoListViewModel(
            ProcessoFacadeService processoService,
            NavigationCoordinatorService navigator)
        {
            _processoService = processoService;
            _navigator = navigator;

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

        private async void AbrirProcesso(Processo processo)
        {
            if (processo == null)
                return;

            await _navigator.NavigateWithParameterAsync(
                NavigationKey.ProcessoDetalhes,
                processo.Id);
        }
    }
}
