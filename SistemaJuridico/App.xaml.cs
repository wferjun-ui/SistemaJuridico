using SistemaJuridico.Services;
using SistemaJuridico.ViewModels;
using SistemaJuridico.Views;
using System;
using System.Windows;
using SistemaJuridico.Infrastructure;

namespace SistemaJuridico
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Abre Login
                var loginWindow = new LoginWindow();
                var loginVM = ServiceLocator.ProcessService; // garante inicialização lazy

                var vm = new LoginViewModel();

                vm.LoginSucesso += OnLoginSucesso;

                loginWindow.DataContext = vm;
                loginWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao iniciar sistema:\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OnLoginSucesso(object sender, EventArgs e)
        {
            try
            {
                var navigationService = new NavigationService();
                var mainShellVM = new MainShellViewModel();

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
                MessageBox.Show(
                    $"Erro ao abrir shell principal:\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
