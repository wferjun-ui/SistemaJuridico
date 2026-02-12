using SistemaJuridico.ViewModels;
using System.Windows;
using UserControl = System.Windows.Controls.UserControl;

namespace SistemaJuridico.Views
{
    public partial class CadastroUsuarioWindow : Window
    {
        public CadastroUsuarioWindow()
        {
            InitializeComponent();

            var vm = new CadastroUsuarioViewModel();
            DataContext = vm;

            PwdBox.PasswordChanged += (s, e) =>
            {
                vm.Senha = PwdBox.Password;
            };
        }
    }
}

