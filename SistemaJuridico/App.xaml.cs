using SistemaJuridico.Infrastructure;
using SistemaJuridico.Services;
using SistemaJuridico.ViewModels;
using SistemaJuridico.Views;
using System;
using System.Windows;

namespace SistemaJuridico
{
    public partial class App : System.Windows.Application
    {
        private static readonly DatabaseService _database = new();
        public static string DB => _database.ConnectionString;
        public static SessionService Session { get; } = new();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                var loginWindow = new LoginWindow();
                var loginVM = ServiceLocator.ProcessService;

                var vm = new LoginViewModel();

                vm.LoginSucesso += OnLoginSucesso;

                loginWindow.DataContext = vm;
                loginWindow.Show();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Erro ao iniciar sistema:\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OnLoginSucesso(object? sender, EventArgs e)
        {
            try
            {
                var navigationService = new NavigationService();
                var mainShellVM = new MainShellViewModel(new NavigationCoordinatorService(
                    navigationService,
                    new ViewFactoryService(),
                    new ViewRegistryService()));

                var mainShell = new MainShellWindow(
                    navigationService,
                    mainShellVM);

                mainShell.Show();

                foreach (Window window in Current.Windows)
                {
                    if (window is LoginWindow)
                    {
                        window.Close();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Erro ao abrir shell principal:\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
