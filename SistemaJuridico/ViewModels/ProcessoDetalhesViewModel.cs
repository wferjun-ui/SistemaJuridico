using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaJuridico.Models;
using SistemaJuridico.Services;
using System.Windows;

namespace SistemaJuridico.ViewModels
{
    public partial class ProcessoDetalhesViewModel : ObservableObject
    {
        private readonly ProcessService _service;

        private readonly string _processoId;

        private bool _possuiLock = false;

        // =========================
        // PROCESSO
        // =========================

        [ObservableProperty]
        private Processo _processo = new();

        // =========================
        // CONTROLE DE EDIÇÃO
        // =========================

        [ObservableProperty]
        private bool _modoSomenteLeitura;

        [ObservableProperty]
        private string _usuarioEditandoTexto = "";

        // =========================
        // CONSTRUTOR
        // =========================

        public ProcessoDetalhesViewModel(
            string processoId,
            ProcessService service)
        {
            _processoId = processoId;
            _service = service;

            CarregarProcesso();
            VerificarLock();
        }

        // =========================
        // CARREGAR PROCESSO
        // =========================

        private void CarregarProcesso()
        {
            Processo = _service
                .ListarProcessos()
                .First(x => x.Id == _processoId);
        }

        // =========================
        // LOCK MULTIUSUÁRIO
        // =========================

        private void VerificarLock()
        {
            var usuario = _service.UsuarioEditando(_processoId);

            if (string.IsNullOrEmpty(usuario))
            {
                _possuiLock = _service.TentarLock(_processoId);
                ModoSomenteLeitura = !_possuiLock;
            }
            else if (usuario == App.Session.UsuarioAtual?.Email)
            {
                _possuiLock = true;
                ModoSomenteLeitura = false;
            }
            else
            {
                ModoSomenteLeitura = true;
                UsuarioEditandoTexto =
                    $"Editado por {usuario}";
            }
        }

        // =========================
        // SALVAR COMO RASCUNHO
        // =========================

        [RelayCommand]
        private void SalvarRascunho()
        {
            var motivo = Microsoft.VisualBasic.Interaction.InputBox(
                "Informe o motivo do rascunho:",
                "Salvar rascunho");

            if (string.IsNullOrWhiteSpace(motivo))
            {
                MessageBox.Show("Motivo obrigatório.");
                return;
            }

            _service.MarcarRascunho(_processoId, motivo);

            MessageBox.Show("Salvo como rascunho.");
        }

        // =========================
        // CONCLUIR EDIÇÃO
        // =========================

        [RelayCommand]
        private void ConcluirEdicao()
        {
            _service.MarcarConcluido(_processoId);
            _service.LiberarLock(_processoId);

            MessageBox.Show("Edição concluída.");
        }

        // =========================
        // AO FECHAR TELA
        // =========================

        public bool PodeFechar()
        {
            if (!_possuiLock)
                return true;

            if (Processo.SituacaoRascunho == "Concluído")
            {
                _service.LiberarLock(_processoId);
                return true;
            }

            var resp = MessageBox.Show(
                "Processo possui alterações.\nDeseja salvar como rascunho?",
                "Confirmação",
                MessageBoxButton.YesNoCancel);

            if (resp == MessageBoxResult.Cancel)
                return false;

            if (resp == MessageBoxResult.Yes)
            {
                SalvarRascunho();
                return true;
            }

            return false;
        }
    }
}
