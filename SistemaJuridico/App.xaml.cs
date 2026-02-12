using SistemaJuridico.Infrastructure;
using SistemaJuridico.Services;
using SistemaJuridico.ViewModels;
using SistemaJuridico.Views;
using System;
using System.IO;
using System.Windows;
using Forms = System.Windows.Forms;

namespace SistemaJuridico
{
    public partial class App : System.Windows.Application
    {
        private static DatabaseService? _database;
        public static string DB => _database?.ConnectionString ?? string.Empty;
        public static SessionService Session { get; } = new();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                var pastaSql = GarantirPastaBanco();
                _database = new DatabaseService(pastaSql);
                _database.Initialize();

                var loginWindow = new LoginWindow();
                var vm = new LoginViewModel();
                vm.LoginSucesso += OnLoginSucesso;

                loginWindow.DataContext = vm;
                MainWindow = loginWindow;
                loginWindow.Show();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Erro ao iniciar sistema:\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
            }
        }

        private static string GarantirPastaBanco()
        {
            var caminhoConfigurado = ConfigService.ObterCaminhoBanco();
            if (!string.IsNullOrWhiteSpace(caminhoConfigurado) && File.Exists(caminhoConfigurado))
                return Path.GetDirectoryName(caminhoConfigurado)!;

            using var dialog = new Forms.FolderBrowserDialog
            {
                Description = "Selecione a pasta do SQL (arquivo juridico.db).",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() != Forms.DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedPath))
                throw new InvalidOperationException("Pasta do SQL não selecionada.");

            var caminhoDb = Path.Combine(dialog.SelectedPath, "juridico.db");
            ConfigService.SalvarCaminhoBanco(caminhoDb);

            return dialog.SelectedPath;
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

                MainWindow = mainShell;
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
}
