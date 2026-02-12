using SistemaJuridico.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace SistemaJuridico.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            PwdBox.PasswordChanged += (s, e) =>
            {
                if (DataContext is LoginViewModel vm)
                    vm.Senha = PwdBox.Password;
            };
        }


        private void LoginWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            if (DataContext is not LoginViewModel vm)
                return;

            if (vm.EntrarCommand.CanExecute(null))
                vm.EntrarCommand.Execute(null);

            e.Handled = true;
        }
    }
}
