using SistemaJuridico.Services;
using SistemaJuridico.ViewModels;
using SistemaJuridico.Views;
using System.Windows;

namespace SistemaJuridico
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var databaseService = new DatabaseService();
            databaseService.Inicializar();

            var estadoService = new EstadoSistemaService(databaseService);

            if (!estadoService.SistemaPossuiDados())
            {
                AbrirTelaMigracao(databaseService);
            }
            else
            {
                AbrirSistemaPrincipal();
            }
        }

        private void AbrirTelaMigracao(DatabaseService db)
        {
            var workflow = new WorkflowMigracaoService(
                new ImportacaoJsonService(db),
                new ValidacaoMigracaoService(db)
            );

            var vm = new MigracaoViewModel(workflow);
            var view = new MigracaoView { DataContext = vm };

            var window = new Window
            {
                Title = "Migração Inicial",
                Content = view,
                Width = 800,
                Height = 600
            };

            window.Show();
        }

        private void AbrirSistemaPrincipal()
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
