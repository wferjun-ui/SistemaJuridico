using SistemaJuridico.Infrastructure;

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
    }
}
