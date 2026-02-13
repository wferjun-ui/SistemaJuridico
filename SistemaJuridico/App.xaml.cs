using SistemaJuridico.Infrastructure;
using SistemaJuridico.Services;
using SistemaJuridico.ViewModels;
using SistemaJuridico.Views;
using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace SistemaJuridico
{
    public partial class App : System.Windows.Application
    {
        private static DatabaseService? _database;
        private static readonly LoggerService _logger = new();
        public static string DB => _database?.ConnectionString ?? string.Empty;
        public static SessionService Session { get; } = new();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnMainWindowClose;

            RegistrarTratamentoGlobalExcecoes();

            try
            {
                var pastaSql = GarantirPastaBanco();
                _database = new DatabaseService(pastaSql);
                _database.Initialize();

                var versionador = new DatabaseVersionService(_database);
                versionador.GarantirAtualizacao();

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


        private void RegistrarTratamentoGlobalExcecoes()
        {
            DispatcherUnhandledException += (_, args) =>
            {
                _logger.Error("Exceção não tratada na UI", args.Exception);
                args.Handled = false;
            };

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                    _logger.Error("Exceção não tratada no AppDomain", ex);
            };
        }

        private static string GarantirPastaBanco()
        {
            var caminhoConfigurado = ConfigService.ObterCaminhoBanco()?.Trim();
            if (!string.IsNullOrWhiteSpace(caminhoConfigurado))
            {
                var pastaConfigurada = Path.GetDirectoryName(caminhoConfigurado);
                if (!string.IsNullOrWhiteSpace(pastaConfigurada))
                {
                    Directory.CreateDirectory(pastaConfigurada);
                    var caminhoNormalizado = Path.Combine(pastaConfigurada, "juridico.db");
                    ConfigService.SalvarCaminhoBanco(caminhoNormalizado);
                    return pastaConfigurada;
                }
            }

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
                var viewRegistry = new ViewRegistryService();
                ViewRegistryBootstrap.Register(viewRegistry);

                var mainShellVM = new MainShellViewModel(new NavigationCoordinatorService(
                    navigationService,
                    new ViewFactoryService(),
                    viewRegistry));

                var mainShell = new MainShellWindow(
                    navigationService,
                    mainShellVM);

                var loginWindow = Current.MainWindow;

                MainWindow = mainShell;
                mainShell.Show();

                loginWindow?.Close();
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
