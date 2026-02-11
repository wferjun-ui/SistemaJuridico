using SistemaJuridico.Infrastructure;
using SistemaJuridico.Services;
using System.Windows.Controls;

namespace SistemaJuridico.ViewModels
{
    public class MainShellViewModel : ViewModelBase
    {
        private readonly NavigationService _navigation;
        private readonly ViewFactoryService _factory;
        private readonly ViewRegistryService _registry;

        public RelayCommand OpenDashboardCommand { get; }

        public MainShellViewModel(
            NavigationService navigation,
            ViewFactoryService factory,
            ViewRegistryService registry)
        {
            _navigation = navigation;
            _factory = factory;
            _registry = registry;

            OpenDashboardCommand = new RelayCommand(OpenDashboard);
        }

        public void LoadInitialView()
        {
            OpenDashboard();
        }

        private void OpenDashboard()
        {
            var (viewType, vmType) = _registry.Resolve("Dashboard");

            var view = _factory.CreateView(viewType, vmType);

            _navigation.Navigate(view);
        }
    }
}
