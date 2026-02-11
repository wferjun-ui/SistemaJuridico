using SistemaJuridico.ViewModels;
using SistemaJuridico.Views;

namespace SistemaJuridico.Services
{
    public static class ViewRegistryBootstrap
    {
        public static void Register(ViewRegistryService registry)
        {
            registry.Register(
                "Dashboard",
                typeof(DashboardView),
                typeof(DashboardViewModel));
        }
    }
}
