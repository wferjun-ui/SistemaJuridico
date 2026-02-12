using System.Windows;

namespace SistemaJuridico.Services
{
    public class DialogService
    {
        public bool? ShowDialog(Window window)
        {
            return window.ShowDialog();
        }

        public void Show(Window window)
        {
            window.Show();
        }
    }
}
