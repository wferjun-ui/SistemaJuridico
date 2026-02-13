using SistemaJuridico.ViewModels;
using System.Windows;

namespace SistemaJuridico.Views
{
    public partial class RelatoriosWindow : Window
    {
        public RelatoriosWindow()
        {
            InitializeComponent();
            DataContext = new RelatoriosViewModel();
        }
    }
}
