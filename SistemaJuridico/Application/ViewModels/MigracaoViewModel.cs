using Microsoft.Win32;
using SistemaJuridico.Services;
using SistemaJuridico.Views;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SistemaJuridico.ViewModels
{
    public class MigracaoViewModel : INotifyPropertyChanged
    {
        private readonly WorkflowMigracaoService _workflow;

        public event Action? MigracaoConcluidaComSucesso;

        public MigracaoViewModel(WorkflowMigracaoService workflow)
        {
            _workflow = workflow;

            SelecionarArquivoCommand = new RelayCommand(SelecionarArquivo);
            ExecutarMigracaoCommand = new RelayCommand(async () => await ExecutarMigracao(), () => !IsProcessando);
        }

        public ICommand SelecionarArquivoCommand { get; }
        public ICommand ExecutarMigracaoCommand { get; }

        private string _caminhoArquivo = "";
        public string CaminhoArquivo
        {
            get => _caminhoArquivo;
            set { _caminhoArquivo = value; OnPropertyChanged(); }
        }

        private string _relatorio = "";
        public string Relatorio
        {
            get => _relatorio;
            set { _relatorio = value; OnPropertyChanged(); }
        }

        private bool _isProcessando;
        public bool IsProcessando
        {
            get => _isProcessando;
            set 
            { 
                _isProcessando = value; 
                OnPropertyChanged();
                (ExecutarMigracaoCommand as RelayCommand)?.Raise();
            }
        }

        private void SelecionarArquivo()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON (*.json)|*.json"
            };

            if (dialog.ShowDialog() == true)
                CaminhoArquivo = dialog.FileName;
        }

        private async Task ExecutarMigracao()
        {
            if (!File.Exists(CaminhoArquivo))
            {
                MessageBox.Show("Selecione um arquivo JSON válido.");
                return;
            }

            IsProcessando = true;
            Relatorio = "Processando...";

            await Task.Run(() =>
            {
                var resultado = _workflow.ExecutarMigracao(CaminhoArquivo);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Relatorio = resultado.Relatorio;

                    if (resultado.Sucesso)
                    {
                        MessageBox.Show("Migração concluída com sucesso.");

                        MigracaoConcluidaComSucesso?.Invoke();
                    }
                    else
                    {
                        MessageBox.Show("Migração concluída com inconsistências. Verifique o relatório.");
                    }
                });
            });

            IsProcessando = false;
        }

       public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string nome = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
    }
}
