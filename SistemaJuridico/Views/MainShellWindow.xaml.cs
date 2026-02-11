using SistemaJuridico.Services;
using SistemaJuridico.ViewModels;
using System.Windows;

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
    }
}
