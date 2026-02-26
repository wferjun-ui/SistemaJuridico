using SistemaJuridico.Infrastructure;
using SistemaJuridico.Services;
using SistemaJuridico.Views;
using System.Threading.Tasks;

namespace SistemaJuridico.ViewModels
{
    public class MainShellViewModel : ViewModelBase
    {
        private readonly NavigationCoordinatorService _navigator;
        private DebugConsoleWindow? _debugWindow;

        public RelayCommand OpenDashboardCommand { get; }
        public RelayCommand OpenRelatoriosCommand { get; }
        public RelayCommand OpenAdminCommand { get; }
        public RelayCommand OpenCadastroUsuarioCommand { get; }
        public RelayCommand OpenDebugConsoleCommand { get; }

        public bool IsAdmin => App.Session.IsAdmin();

        public MainShellViewModel(NavigationCoordinatorService navigator)
        {
            _navigator = navigator;

            OpenDashboardCommand = new RelayCommand(() =>
                _navigator.Navigate(NavigationKey.Dashboard));

            OpenRelatoriosCommand = new RelayCommand(() =>
            {
                var owner = System.Windows.Application.Current.MainWindow;
                var janela = new RelatoriosWindow { Owner = owner };
                janela.ShowDialog();
            });

            OpenAdminCommand = new RelayCommand(() =>
            {
                if (!IsAdmin)
                {
                    System.Windows.MessageBox.Show("Apenas administradores podem acessar este recurso.");
                    return;
                }

                new AdminEmailsWindow { Owner = System.Windows.Application.Current.MainWindow }.ShowDialog();
            });

            OpenCadastroUsuarioCommand = new RelayCommand(() =>
            {
                if (!IsAdmin)
                {
                    System.Windows.MessageBox.Show("Apenas administradores podem acessar este recurso.");
                    return;
                }

                new CadastroUsuarioWindow { Owner = System.Windows.Application.Current.MainWindow }.ShowDialog();
            });

            OpenDebugConsoleCommand = new RelayCommand(() =>
            {
                if (!IsAdmin)
                {
                    System.Windows.MessageBox.Show("Depuração disponível apenas para administradores.");
                    return;
                }

                if (_debugWindow == null || !_debugWindow.IsVisible)
                {
                    _debugWindow = new DebugConsoleWindow
                    {
                        Owner = System.Windows.Application.Current.MainWindow
                    };
                    _debugWindow.Closed += (_, _) => _debugWindow = null;
                    _debugWindow.Show();
                }

                _debugWindow.Activate();
            });
        }


        public async Task AbrirProcessoDetalhesAsync(string processoId)
        {
            if (string.IsNullOrWhiteSpace(processoId))
                return;

            await _navigator.NavigateWithParameterAsync(NavigationKey.ProcessoDetalhes, processoId);
        }

        public void LoadInitialView()
        {
            _navigator.Navigate(NavigationKey.Dashboard);
        }
    }
}
