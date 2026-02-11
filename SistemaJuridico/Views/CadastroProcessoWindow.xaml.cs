using SistemaJuridico.Services;
using SistemaJuridico.ViewModels;
using System.Windows;

namespace SistemaJuridico.Views
{
    public partial class CadastroProcessoWindow : Window
    {
        public CadastroProcessoWindow()
        {
            InitializeComponent();

            var db = new DatabaseService();
            var service = new ProcessService(db);

            var vm = new CadastroProcessoViewModel(service);

            vm.FecharTela = Close;

            DataContext = vm;
        }
    }
}

