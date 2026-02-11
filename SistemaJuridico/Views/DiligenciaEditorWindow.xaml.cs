using System.Windows;

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
