using SistemaJuridico.Infrastructure;
using SistemaJuridico.ViewModels;
using System;
using System.Threading.Tasks;

namespace SistemaJuridico.Services
{
    public class NavigationCoordinatorService
    {
        private readonly NavigationService _navigation;
        private readonly ViewFactoryService _factory;
        private readonly ViewRegistryService _registry;
        private readonly LoggerService _logger = new();

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
            try
            {
                _logger.Debug($"Navegando para {key}.");
                var (viewType, vmType) = _registry.Resolve(key);
                var view = _factory.CreateView(viewType, vmType);
                _navigation.Navigate(view);
            }
            catch (Exception ex)
            {
                _logger.Error($"Falha ao navegar para {key}", ex);
                throw;
            }
        }

        public async Task NavigateWithParameterAsync(
            NavigationKey key,
            int id)
        {
            try
            {
                _logger.Debug($"Navegando para {key} com id={id}.");
                var (viewType, vmType) = _registry.Resolve(key);
                var view = _factory.CreateView(viewType, vmType);
                _navigation.Navigate(view);

                if (view.DataContext is ProcessoDetalhesHostViewModel detalhesHostVm)
                    await detalhesHostVm.CarregarProcessoAsync(id);

                if (view.DataContext is ProcessoEditorHostViewModel editorHostVm)
                    await editorHostVm.CarregarAsync(id);
            }
            catch (Exception ex)
            {
                _logger.Error($"Falha ao navegar para {key} com id={id}", ex);
                throw;
            }
        }

        public async Task NavigateWithParameterAsync(
            NavigationKey key,
            string processoId)
        {
            try
            {
                _logger.Debug($"Navegando para {key} com processoId={processoId}.");
                var (viewType, vmType) = _registry.Resolve(key);
                var view = _factory.CreateView(viewType, vmType);
                _navigation.Navigate(view);

                if (view.DataContext is ProcessoDetalhesHostViewModel detalhesHostVm)
                    await detalhesHostVm.CarregarProcessoAsync(processoId);
            }
            catch (Exception ex)
            {
                _logger.Error($"Falha ao navegar para {key} com processoId={processoId}", ex);
                throw;
            }
        }
    }
}
