using SistemaJuridico.ViewModels;
using System.Windows;

namespace SistemaJuridico.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();

            var vm = new LoginViewModel();
            DataContext = vm;

            PwdBox.PasswordChanged += (s, e) =>
            {
                vm.Senha = PwdBox.Password;
            };
        }
    }
}

