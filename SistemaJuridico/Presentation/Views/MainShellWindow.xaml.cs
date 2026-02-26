using SistemaJuridico.Services;
using SistemaJuridico.ViewModels;
using System.Windows;
using System.Threading.Tasks;
using UserControl = System.Windows.Controls.UserControl;

namespace SistemaJuridico.Views
{
    public partial class MainShellWindow : Window
    {
        public MainShellWindow(
            NavigationService navigationService,
            MainShellViewModel vm)
        {
            InitializeComponent();

            navigationService.Configure(MainRegion);

            DataContext = vm;

            Loaded += (s, e) =>
            {
                vm.LoadInitialView();
            };
        }

        public Task AbrirProcessoDetalhesAsync(string processoId)
        {
            if (DataContext is not MainShellViewModel vm)
                return Task.CompletedTask;

            return vm.AbrirProcessoDetalhesAsync(processoId);
        }
    }
}
