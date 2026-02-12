using SistemaJuridico.Services;
using SistemaJuridico.ViewModels;
using SistemaJuridico.Views;
using System;
using System.Windows;
using SistemaJuridico.Infrastructure;

namespace SistemaJuridico
{
    public partial class App : Application
    {
        private ServiceLocator _serviceLocator;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                ConfigureServices();

                // Abre Login
                var loginWindow = new LoginWindow();
                var loginVM = _serviceLocator.Get<LoginViewModel>();

                loginVM.LoginSucesso += OnLoginSucesso;

                loginWindow.DataContext = loginVM;
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

        private void ConfigureServices()
        {
            _serviceLocator = new ServiceLocator();

            // Serviços base
            _serviceLocator.RegisterSingleton<NavigationService>();
            _serviceLocator.RegisterSingleton<DialogService>();

            // ViewModels
            _serviceLocator.RegisterTransient<LoginViewModel>();
            _serviceLocator.RegisterTransient<MainShellViewModel>();
        }

        private void OnLoginSucesso(object sender, EventArgs e)
        {
            try
            {
                var navigationService = _serviceLocator.Get<NavigationService>();
                var mainShellVM = _serviceLocator.Get<MainShellViewModel>();

                var mainShell = new MainShellWindow(
                    navigationService,
                    mainShellVM);

                mainShell.Show();

                // Fecha login
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
