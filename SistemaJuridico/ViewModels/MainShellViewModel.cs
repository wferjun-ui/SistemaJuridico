using SistemaJuridico.Infrastructure;
using SistemaJuridico.Services;

namespace SistemaJuridico.ViewModels
{
    public class MainShellViewModel : ViewModelBase
    {
        private readonly NavigationService _navigation;

        public RelayCommand OpenDashboardCommand { get; }
        public RelayCommand OpenNovoProcessoCommand { get; }

        public MainShellViewModel(NavigationService navigation)
        {
            _navigation = navigation;

            OpenDashboardCommand = new RelayCommand(OpenDashboard);
            OpenNovoProcessoCommand = new RelayCommand(OpenNovoProcesso);
        }

        private void OpenDashboard()
        {
            // Ainda não vamos conectar View real
        }

        private void OpenNovoProcesso()
        {
            // Será conectado depois
        }
    }
}
