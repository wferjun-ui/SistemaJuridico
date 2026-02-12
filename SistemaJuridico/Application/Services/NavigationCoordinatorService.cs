using SistemaJuridico.Infrastructure;
using SistemaJuridico.ViewModels;
using System.Threading.Tasks;

namespace SistemaJuridico.Services
{
    public class NavigationCoordinatorService
    {
        private readonly NavigationService _navigation;
        private readonly ViewFactoryService _factory;
        private readonly ViewRegistryService _registry;

        public NavigationCoordinatorService(
            NavigationService navigation,
            ViewFactoryService factory,
            ViewRegistryService registry)
        {
            _navigation = navigation;
            _factory = factory;
            _registry = registry;
        }

        public void Navigate(NavigationKey key)
        {
            var (viewType, vmType) = _registry.Resolve(key);
            var view = _factory.CreateView(viewType, vmType);
            _navigation.Navigate(view);
        }

        public async Task NavigateWithParameterAsync(
            NavigationKey key,
            int id)
        {
            var (viewType, vmType) = _registry.Resolve(key);
            var view = _factory.CreateView(viewType, vmType);
            _navigation.Navigate(view);

            if (view.DataContext is ProcessoDetalhesHostViewModel detalhesHostVm)
                await detalhesHostVm.CarregarProcessoAsync(id);

            if (view.DataContext is ProcessoEditorHostViewModel editorHostVm)
                await editorHostVm.CarregarAsync(id);
        }
    }
}
