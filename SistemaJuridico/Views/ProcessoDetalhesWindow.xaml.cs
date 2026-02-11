using SistemaJuridico.ViewModels;
using System.Windows;

namespace SistemaJuridico.Views
{
    public partial class ProcessoDetalhesWindow : Window
    {
        public ProcessoDetalhesWindow(string processoId)
        {
            InitializeComponent();
            DataContext = new ProcessoDetalhesViewModel(processoId);
        }
    }
}
