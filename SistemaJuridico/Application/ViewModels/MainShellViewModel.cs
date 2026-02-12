using SistemaJuridico.Infrastructure;
using SistemaJuridico.Services;

namespace SistemaJuridico.ViewModels
{
    public class MainShellViewModel : ViewModelBase
    {
        private readonly NavigationCoordinatorService _navigator;

        public RelayCommand OpenDashboardCommand { get; }
        public RelayCommand OpenProcessosCommand { get; }
        public RelayCommand OpenContasCommand { get; }
        public RelayCommand OpenAuditoriaCommand { get; }

        public MainShellViewModel(NavigationCoordinatorService navigator)
        {
            _navigator = navigator;

            OpenDashboardCommand = new RelayCommand(() =>
                _navigator.Navigate(NavigationKey.Dashboard));

            OpenProcessosCommand = new RelayCommand(() =>
                _navigator.Navigate(NavigationKey.Processos));

            OpenContasCommand = new RelayCommand(() =>
                _navigator.Navigate(NavigationKey.Contas));

            OpenAuditoriaCommand = new RelayCommand(() =>
                _navigator.Navigate(NavigationKey.Auditoria));
        }

        public void LoadInitialView()
        {
            _navigator.Navigate(NavigationKey.Dashboard);
        }
    }
}
