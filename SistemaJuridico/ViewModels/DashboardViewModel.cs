using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaJuridico.Models;
using SistemaJuridico.Services;
using SistemaJuridico.Views;
using System.Collections.ObjectModel;
using System.Windows;

namespace SistemaJuridico.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly ProcessService _service;

        public ObservableCollection<ProcessoResumoVM> Processos { get; } = new();

        public DashboardViewModel()
        {
            var db = new DatabaseService();
            _service = new ProcessService(db);

            Carregar();
        }

        // =========================
        // CARREGAR DASHBOARD
        // =========================

        [RelayCommand]
        private void Carregar()
        {
            Processos.Clear();

            foreach (var p in _service.ListarProcessos())
            {
                var resumo = _service.ObterResumo(p.Id);

                Processos.Add(new ProcessoResumoVM
                {
                    Processo = p,
                    SaldoPendente = resumo.saldoPendente,
                    DiligenciaPendente = resumo.diligenciaPendente,
                    DataUltimoLancamento = resumo.dataUltLanc
                });
            }
        }

        // =========================
        // ABRIR PROCESSO
        // =========================

        [RelayCommand]
        private void AbrirProcesso(ProcessoResumoVM processo)
        {
            if (processo == null)
                return;

            var tela = new ProcessoDetalhesWindow(processo.Processo.Id);
            tela.ShowDialog();

            Carregar();
        }
    }

    // =========================
    // VM AUXILIAR
    // =========================

    public class ProcessoResumoVM
    {
        public Processo Processo { get; set; } = new();

        public decimal SaldoPendente { get; set; }

        public bool DiligenciaPendente { get; set; }

        public string? DataUltimoLancamento { get; set; }

        public string StatusVisual =>
            Processo.SituacaoRascunho switch
            {
                "Rascunho" => "📝 Rascunho",
                "Em edição" => $"🔒 Editado por {Processo.UsuarioRascunho}",
                _ => "✔ Concluído"
            };

        public string ResumoFinanceiro =>
            SaldoPendente > 0
                ? $"Saldo pendente: {SaldoPendente:C}"
                : "Sem pendências financeiras";

        public string DiligenciaTexto =>
            DiligenciaPendente
                ? "⚠ Diligência pendente"
                : "";
    }
}
