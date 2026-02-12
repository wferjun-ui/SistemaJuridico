using SistemaJuridico.ViewModels;
using System.Windows;
using UserControl = System.Windows.Controls.UserControl;

namespace SistemaJuridico.Views
{
    public partial class AdminEmailsWindow : Window
    {
        public AdminEmailsWindow()
        {
            InitializeComponent();
            DataContext = new AdminEmailsViewModel();
        }
    }
}

