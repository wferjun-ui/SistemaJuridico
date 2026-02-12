using SistemaJuridico.Infrastructure;
using SistemaJuridico.ViewModels;
using System.Windows;

namespace SistemaJuridico.Views
{
    public partial class CadastroProcessoWindow : Window
    {
        public CadastroProcessoWindow()
        {
            InitializeComponent();

            var vm = new CadastroProcessoViewModel(
                ServiceLocator.ProcessService,
                ServiceLocator.ItemSaudeService);

            vm.FecharTela = Close;
            DataContext = vm;
        }
    }
}
