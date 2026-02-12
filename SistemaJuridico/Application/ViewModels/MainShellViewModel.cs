using SistemaJuridico.Infrastructure;
using SistemaJuridico.Services;
using SistemaJuridico.Views;

namespace SistemaJuridico.ViewModels
{
    public class MainShellViewModel : ViewModelBase
    {
        private readonly NavigationCoordinatorService _navigator;

        public RelayCommand OpenDashboardCommand { get; }
        public RelayCommand OpenProcessosCommand { get; }
        public RelayCommand OpenContasCommand { get; }
        public RelayCommand OpenAuditoriaCommand { get; }
        public RelayCommand OpenAdminCommand { get; }
        public RelayCommand OpenCadastroUsuarioCommand { get; }

        public bool IsAdmin => App.Session.IsAdmin();

        public MainShellViewModel(NavigationCoordinatorService navigator)
        {
            _navigator = navigator;

            OpenDashboardCommand = new RelayCommand(() =>
                _navigator.Navigate(NavigationKey.Dashboard));

            OpenProcessosCommand = new RelayCommand(() =>
                _navigator.Navigate(NavigationKey.Processos));

            OpenContasCommand = new RelayCommand(() =>
                _navigator.Navigate(NavigationKey.Contas));

            OpenAuditoriaCommand = new RelayCommand(() =>
                _navigator.Navigate(NavigationKey.Auditoria));

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
        }

        public void LoadInitialView()
        {
            _navigator.Navigate(NavigationKey.Dashboard);
        }
    }
}
