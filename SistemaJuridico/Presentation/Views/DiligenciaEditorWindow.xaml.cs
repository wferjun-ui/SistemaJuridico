using System.Windows;
using UserControl = System.Windows.Controls.UserControl;

namespace SistemaJuridico.Views
{
    public partial class DiligenciaEditorWindow : Window
    {
        public DiligenciaEditorWindow(object vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
