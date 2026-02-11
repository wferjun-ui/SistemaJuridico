using SistemaJuridico.ViewModels;
using System.Windows.Controls;

namespace SistemaJuridico.Views
{
    public partial class ContasView : UserControl
    {
        public ContasView(string processoId)
        {
            InitializeComponent();
            DataContext = new ContasViewModel(processoId);
        }
    }
}
