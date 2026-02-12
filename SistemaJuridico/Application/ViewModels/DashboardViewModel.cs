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
        private readonly ContaService _contaService;

        public ObservableCollection<ProcessoResumoVM> Processos { get; } = new();
        public ObservableCollection<Processo> ProcessosRascunho { get; } = new();
        public ObservableCollection<Processo> ProcessosPendentes { get; } = new();
        public ObservableCollection<string> ProcessosBloqueados { get; } = new();

        [ObservableProperty]
        private decimal _saldoTotalPendente;

        [ObservableProperty]
        private string _ultimaMovimentacaoTexto = "";

        [ObservableProperty]
        private int _totalRascunhos;

        [ObservableProperty]
        private int _totalBloqueados;

        public DashboardViewModel()
        {
            var db = new DatabaseService();

            _service = new ProcessService(db);
            _contaService = new ContaService(db);

            Carregar();
        }

        [RelayCommand]
        private void Carregar()
        {
            Processos.Clear();
            ProcessosRascunho.Clear();
            ProcessosPendentes.Clear();
            ProcessosBloqueados.Clear();

            decimal saldoGlobal = 0;
            int rascunhos = 0;
            int bloqueados = 0;

            var atual = App.Session.UsuarioAtual?.Email;

            foreach (var p in _service.ListarProcessos())
            {
                var resumo = _service.ObterResumo(p.Id);

                saldoGlobal += resumo.saldoPendente;

                if (p.SituacaoRascunho == "Rascunho")
                {
                    rascunhos++;
                    ProcessosRascunho.Add(p);
                }

                if (resumo.diligenciaPendente)
                    ProcessosPendentes.Add(p);

                var usuarioLock = _service.UsuarioEditando(p.Id);
                if (!string.IsNullOrWhiteSpace(usuarioLock) && usuarioLock != atual)
                {
                    bloqueados++;
                    ProcessosBloqueados.Add($"{p.Numero} - {usuarioLock}");
                }

                Processos.Add(new ProcessoResumoVM
                {
                    Processo = p,
                    SaldoPendente = resumo.saldoPendente,
                    DiligenciaPendente = resumo.diligenciaPendente,
                    DataUltimoLancamento = resumo.dataUltLanc
                });
            }

            SaldoTotalPendente = saldoGlobal;
            TotalRascunhos = rascunhos;
            TotalBloqueados = bloqueados;

            AtualizarUltimaMovimentacao();
        }

        private void AtualizarUltimaMovimentacao()
        {
            var conta = _contaService
                .ListarTodas()
                .OrderByDescending(c => c.DataMovimentacao)
                .FirstOrDefault();

            UltimaMovimentacaoTexto =
                conta == null
                ? "Sem movimentações"
                : conta.DataMovimentacao;
        }

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
