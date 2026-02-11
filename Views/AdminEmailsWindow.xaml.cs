using SistemaJuridico.ViewModels;
using System.Windows;

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

