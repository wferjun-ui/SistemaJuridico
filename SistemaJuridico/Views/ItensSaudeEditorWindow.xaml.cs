using SistemaJuridico.ViewModels;
using System.Windows;
using UserControl = System.Windows.Controls.UserControl;

namespace SistemaJuridico.Views
{
    public partial class ItensSaudeEditorWindow : Window
    {
        public ItensSaudeEditorWindow(ItensSaudeEditorViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void Fechar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
