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

        private bool _alterado = false;

        public Processo Processo { get; set; }

        public string Numero => Processo.Numero;
        public string Paciente => Processo.Paciente;

        [ObservableProperty]
        private string _observacaoFixa = "";

        public ProcessoDetalhesViewModel(string processoId)
        {
            _processoId = processoId;
            _service = new ProcessService(App.DB!);

            Processo = _service.Obter(processoId);

            ObservacaoFixa = Processo.ObservacaoFixa ?? "";
        }

        partial void OnObservacaoFixaChanged(string value)
        {
            _alterado = true;
        }

        // =========================
        // SALVAR RASCUNHO
        // =========================

        [RelayCommand]
        private void SalvarRascunho()
        {
            var motivo = Microsoft.VisualBasic.Interaction.InputBox(
                "Informe o motivo do rascunho:",
                "Salvar Rascunho",
                "");

            if (string.IsNullOrWhiteSpace(motivo))
                return;

            Processo.ObservacaoFixa = ObservacaoFixa;
            Processo.SituacaoRascunho = "Rascunho";
            Processo.MotivoRascunho = motivo;

            _service.Salvar(Processo);

            _alterado = false;

            MessageBox.Show("Rascunho salvo.");
        }

        // =========================
        // CONCLUIR
        // =========================

        [RelayCommand]
        private void Concluir()
        {
            Processo.ObservacaoFixa = ObservacaoFixa;
            Processo.SituacaoRascunho = "Concluído";
            Processo.MotivoRascunho = null;

            _service.Salvar(Processo);

            _alterado = false;

            MessageBox.Show("Processo concluído.");
        }

        // =========================
        // AO FECHAR
        // =========================

        public bool PodeFechar()
        {
            if (!_alterado)
                return true;

            var res = MessageBox.Show(
                "Existem alterações não salvas. Deseja salvar como rascunho?",
                "Confirmação",
                MessageBoxButton.YesNoCancel);

            if (res == MessageBoxResult.Yes)
            {
                SalvarRascunho();
                return true;
            }

            if (res == MessageBoxResult.No)
                return true;

            return false;
        }
    }
}
