using SistemaJuridico.Infrastructure;
using SistemaJuridico.ViewModels;
using SistemaJuridico.Views;

namespace SistemaJuridico.Services
{
    public static class ViewRegistryBootstrap
    {
        public static void Register(ViewRegistryService registry)
        {
            registry.Register(
                NavigationKey.Dashboard,
                typeof(DashboardView),
                typeof(DashboardViewModel));

            registry.Register(
                NavigationKey.Processos,
                typeof(ProcessoListView),
                typeof(ProcessoListViewModel));

            registry.Register(
                NavigationKey.ProcessoDetalhes,
                typeof(ProcessoDetalhesHostView),
                typeof(ProcessoDetalhesHostViewModel));

            registry.Register(
                NavigationKey.Contas,
                typeof(ContasView),
                typeof(ContasViewModel));
        }
    }
}
