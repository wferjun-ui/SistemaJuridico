using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaJuridico.Models;
using SistemaJuridico.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace SistemaJuridico.ViewModels
{
    public partial class ProcessoDetalhesViewModel : ObservableObject
    {
        private readonly ProcessService _processService;
        private readonly ContaService _contaService;

        private readonly string _processoId;

        public Processo Processo { get; private set; }

        public ObservableCollection<Conta> Contas { get; } = new();

        [ObservableProperty]
        private bool _modoSomenteLeitura;

        [ObservableProperty]
        private string _usuarioEditandoTexto = "";

        public ProcessoDetalhesViewModel(
            string processoId,
            ProcessService processService)
        {
            _processService = processService;

            var db = new DatabaseService();
            _contaService = new ContaService(db);

            _processoId = processoId;

            Processo = _processService
                .ListarProcessos()
                .First(x => x.Id == processoId);

            ValidarLock();
            CarregarContas();
        }

        private void ValidarLock()
        {
            var usuario = _processService.UsuarioEditando(_processoId);
            var atual = App.Session.UsuarioAtual?.Email;

            if (usuario != null && usuario != atual)
            {
                ModoSomenteLeitura = true;
                UsuarioEditandoTexto =
                    $"Processo em edição por {usuario}";
            }
            else
            {
                _processService.TentarLock(_processoId);
            }
        }

        private void CarregarContas()
        {
            Contas.Clear();

            foreach (var c in _contaService.ListarPorProcesso(_processoId))
                Contas.Add(c);
        }

        [RelayCommand]
        private void SalvarRascunho()
        {
            var motivo = Microsoft.VisualBasic.Interaction.InputBox(
                "Informe o motivo do rascunho:");

            if (string.IsNullOrWhiteSpace(motivo))
                return;

            _processService.MarcarRascunho(_processoId, motivo);

            MessageBox.Show("Rascunho salvo.");
        }

        [RelayCommand]
        private void ConcluirEdicao()
        {
            _processService.MarcarConcluido(_processoId);

            MessageBox.Show("Edição concluída.");
        }

        public bool PodeFechar()
        {
            if (ModoSomenteLeitura)
                return true;

            var r = MessageBox.Show(
                "Deseja sair sem salvar?",
                "Confirmação",
                MessageBoxButton.YesNo);

            return r == MessageBoxResult.Yes;
        }
    }
}
