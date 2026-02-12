using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaJuridico.Models;
using SistemaJuridico.Services;
using SistemaJuridico.Views;
using System.Collections.ObjectModel;
using System.Globalization;

namespace SistemaJuridico.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly ProcessService _service;
        private readonly ContaService _contaService;
        private readonly DiligenciaService _diligenciaService;

        public ObservableCollection<ProcessoResumoVM> Processos { get; } = new();
        public ObservableCollection<ProcessoPrazoVM> ProcessosAtrasados { get; } = new();
        public ObservableCollection<ProcessoPrazoVM> ProcessosAAtrasar { get; } = new();
        public ObservableCollection<string> ProcessosBloqueados { get; } = new();

        [ObservableProperty]
        private decimal _saldoTotalPendente;

        [ObservableProperty]
        private string _ultimaMovimentacaoTexto = "";

        [ObservableProperty]
        private int _totalAtrasados;

        [ObservableProperty]
        private int _totalAAtrasar;

        [ObservableProperty]
        private int _totalBloqueados;

        [ObservableProperty]
        private string _usuarioLogadoTexto = "";

        public DashboardViewModel()
        {
            var db = new DatabaseService();

            _service = new ProcessService(db);
            _contaService = new ContaService(db);
            _diligenciaService = new DiligenciaService(db);

            Carregar();
        }

        [RelayCommand]
        private void Carregar()
        {
            Processos.Clear();
            ProcessosAtrasados.Clear();
            ProcessosAAtrasar.Clear();
            ProcessosBloqueados.Clear();

            decimal saldoGlobal = 0;
            int atrasados = 0;
            int aAtrasar = 0;
            int bloqueados = 0;

            var hoje = DateTime.Today;
            var limiteAAtrasar = hoje.AddDays(7);
            var atual = App.Session.UsuarioAtual?.Email;
            UsuarioLogadoTexto = $"Logado como: {atual ?? "(não identificado)"}";

            foreach (var p in _service.ListarProcessos())
            {
                var resumo = _service.ObterResumo(p.Id);
                saldoGlobal += resumo.saldoPendente;

                var usuarioLock = _service.UsuarioEditando(p.Id);
                if (!string.IsNullOrWhiteSpace(usuarioLock) && usuarioLock != atual)
                {
                    bloqueados++;
                    ProcessosBloqueados.Add($"{p.Numero} - {usuarioLock}");
                }

                foreach (var diligencia in _diligenciaService.ListarPorProcesso(p.Id).Where(x => !x.Concluida))
                {
                    if (!TryParsePrazo(diligencia.Prazo, out var prazo))
                        continue;

                    var vmPrazo = new ProcessoPrazoVM
                    {
                        ProcessoId = p.Id,
                        NumeroProcesso = p.Numero,
                        DescricaoDiligencia = diligencia.Descricao,
                        Prazo = prazo
                    };

                    if (prazo.Date < hoje)
                    {
                        atrasados++;
                        ProcessosAtrasados.Add(vmPrazo);
                    }
                    else if (prazo.Date <= limiteAAtrasar)
                    {
                        aAtrasar++;
                        ProcessosAAtrasar.Add(vmPrazo);
                    }
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
            TotalAtrasados = atrasados;
            TotalAAtrasar = aAtrasar;
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

        private static bool TryParsePrazo(string? prazo, out DateTime data)
        {
            data = default;

            if (string.IsNullOrWhiteSpace(prazo))
                return false;

            return DateTime.TryParseExact(prazo, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out data)
                || DateTime.TryParseExact(prazo, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out data)
                || DateTime.TryParse(prazo, out data);
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
    }

    public class ProcessoPrazoVM
    {
        public string ProcessoId { get; set; } = string.Empty;
        public string NumeroProcesso { get; set; } = string.Empty;
        public string DescricaoDiligencia { get; set; } = string.Empty;
        public DateTime Prazo { get; set; }

        public string PrazoTexto => Prazo.ToString("dd/MM/yyyy");
    }
}
