using SistemaJuridico.Models;
using SistemaJuridico.Services;
using SistemaJuridico.Infrastructure;
using SistemaJuridico.Views;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace SistemaJuridico.ViewModels
{
    public class ProcessoListViewModel : ViewModelBase
    {
        private readonly ProcessoFacadeService _processoService;

        public ObservableCollection<Processo> Processos { get; } = new();

        private Processo? _processoSelecionado;
        public Processo? ProcessoSelecionado
        {
            get => _processoSelecionado;
            set
            {
                _processoSelecionado = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand CarregarCommand { get; }
        public RelayCommand NovoProcessoCommand { get; }
        public RelayCommand<Processo?> AbrirProcessoCommand { get; }

        public ProcessoListViewModel(ProcessoFacadeService processoService)
        {
            _processoService = processoService;

            CarregarCommand = new RelayCommand(async () => await Carregar());
            NovoProcessoCommand = new RelayCommand(NovoProcesso);
            AbrirProcessoCommand = new RelayCommand<Processo?>(AbrirProcesso);

            _ = Carregar();
        }

        private async Task Carregar()
        {
            Processos.Clear();

            var lista = await _processoService.ListarProcessosAsync();

            foreach (var p in lista)
                Processos.Add(p);
        }

        private void NovoProcesso()
        {
            var window = new CadastroProcessoWindow();
            window.ShowDialog();
            _ = Carregar();
        }

        private void AbrirProcesso(Processo? processo)
        {
            if (processo == null)
                return;

            var window = new ProcessoDetalhesWindow(processo.Id);
            window.ShowDialog();
            _ = Carregar();
        }
    }
}
