using SistemaJuridico.Infrastructure;
using SistemaJuridico.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace SistemaJuridico.ViewModels
{
    public class RelatoriosViewModel : ViewModelBase
    {
        private readonly ProcessoCacheService _cacheService;
        private readonly RelatorioProcessoService _relatorioService;

        private readonly List<ProcessoBuscaRelatorioItemVM> _todos = new();
        private List<ProcessoBuscaRelatorioItemVM> _filtrados = new();

        public ObservableCollection<ProcessoBuscaRelatorioItemVM> Processos { get; } = new();

        private string _numeroFiltro = string.Empty;
        public string NumeroFiltro
        {
            get => _numeroFiltro;
            set => SetProperty(ref _numeroFiltro, value);
        }

        private string _pacienteFiltro = string.Empty;
        public string PacienteFiltro
        {
            get => _pacienteFiltro;
            set => SetProperty(ref _pacienteFiltro, value);
        }

        private string _genitorFiltro = string.Empty;
        public string GenitorFiltro
        {
            get => _genitorFiltro;
            set => SetProperty(ref _genitorFiltro, value);
        }

        private string _juizFiltro = string.Empty;
        public string JuizFiltro
        {
            get => _juizFiltro;
            set => SetProperty(ref _juizFiltro, value);
        }

        private string _statusFiltro = string.Empty;
        public string StatusFiltro
        {
            get => _statusFiltro;
            set => SetProperty(ref _statusFiltro, value);
        }

        private string _tipoFiltro = string.Empty;
        public string TipoFiltro
        {
            get => _tipoFiltro;
            set => SetProperty(ref _tipoFiltro, value);
        }

        private int _totalResultados;
        public int TotalResultados
        {
            get => _totalResultados;
            set => SetProperty(ref _totalResultados, value);
        }

        public RelayCommand CarregarCommand { get; }
        public RelayCommand BuscarCommand { get; }
        public RelayCommand LimparFiltrosCommand { get; }
        public RelayCommand ExportarPdfSelecionadosCommand { get; }

        public RelatoriosViewModel()
        {
            var db = new DatabaseService();
            _cacheService = new ProcessoCacheService(db);
            _relatorioService = new RelatorioProcessoService(db);

            CarregarCommand = new RelayCommand(Carregar);
            BuscarCommand = new RelayCommand(AplicarBusca);
            LimparFiltrosCommand = new RelayCommand(LimparFiltros);
            ExportarPdfSelecionadosCommand = new RelayCommand(ExportarPdfSelecionados);

            Carregar();
        }

        private void Carregar()
        {
            _todos.Clear();
            var cache = _cacheService.ObterCacheLeve();
            foreach (var p in cache)
            {
                _todos.Add(new ProcessoBuscaRelatorioItemVM
                {
                    ProcessoId = p.ProcessoId,
                    Numero = p.Numero,
                    Paciente = p.Paciente,
                    Genitor = p.Genitor,
                    Juiz = p.Juiz,
                    Tipo = p.TipoProcesso,
                    StatusAtual = p.StatusCalculado ?? p.StatusProcesso,
                    Categoria = ObterCategoria(p.TipoProcesso)
                });
            }

            AplicarBusca();
        }

        private static string ObterCategoria(string? tipo)
        {
            if (string.IsNullOrWhiteSpace(tipo)) return "Outros";
            var t = tipo.Trim();
            if (t.Contains("med", StringComparison.OrdinalIgnoreCase)) return "Medicamento";
            if (t.Contains("tera", StringComparison.OrdinalIgnoreCase)) return "Terapia";
            if (t.Contains("cir", StringComparison.OrdinalIgnoreCase)) return "Cirurgia";
            return "Outros";
        }

        private static bool Contem(string? fonte, string termo)
            => !string.IsNullOrWhiteSpace(fonte) && fonte.Contains(termo, StringComparison.OrdinalIgnoreCase);

        private void AplicarBusca()
        {
            IEnumerable<ProcessoBuscaRelatorioItemVM> q = _todos;

            if (!string.IsNullOrWhiteSpace(NumeroFiltro)) q = q.Where(x => Contem(x.Numero, NumeroFiltro));
            if (!string.IsNullOrWhiteSpace(PacienteFiltro)) q = q.Where(x => Contem(x.Paciente, PacienteFiltro));
            if (!string.IsNullOrWhiteSpace(GenitorFiltro)) q = q.Where(x => Contem(x.Genitor, GenitorFiltro));
            if (!string.IsNullOrWhiteSpace(JuizFiltro)) q = q.Where(x => Contem(x.Juiz, JuizFiltro));
            if (!string.IsNullOrWhiteSpace(StatusFiltro)) q = q.Where(x => Contem(x.StatusAtual, StatusFiltro));
            if (!string.IsNullOrWhiteSpace(TipoFiltro)) q = q.Where(x => Contem(x.Tipo, TipoFiltro) || Contem(x.Categoria, TipoFiltro));

            _filtrados = q.OrderBy(x => x.Numero).ToList();
            TotalResultados = _filtrados.Count;

            Processos.Clear();
            foreach (var p in _filtrados)
                Processos.Add(p);
        }

        private void LimparFiltros()
        {
            NumeroFiltro = string.Empty;
            PacienteFiltro = string.Empty;
            GenitorFiltro = string.Empty;
            JuizFiltro = string.Empty;
            StatusFiltro = string.Empty;
            TipoFiltro = string.Empty;
            AplicarBusca();
        }

        private void ExportarPdfSelecionados()
        {
            var selecionados = Processos.Where(x => x.Selecionado).ToList();
            if (selecionados.Count == 0)
            {
                System.Windows.MessageBox.Show("Selecione ao menos um processo para gerar PDF.");
                return;
            }

            var save = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF|*.pdf",
                FileName = $"relatorio_processos_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
            };

            if (save.ShowDialog() != true)
                return;

            var modelos = selecionados.Select(s => _relatorioService.GerarModelo(s.ProcessoId)).ToList();
            new PdfRelatorioProcessoService().GerarPdfLista(modelos, save.FileName);
            System.Windows.MessageBox.Show("Relatório PDF gerado com sucesso.");
        }
    }

    public class ProcessoBuscaRelatorioItemVM : ViewModelBase
    {
        public string ProcessoId { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string Paciente { get; set; } = string.Empty;
        public string? Genitor { get; set; }
        public string Juiz { get; set; } = string.Empty;
        public string? Tipo { get; set; }
        public string Categoria { get; set; } = "Outros";
        public string? StatusAtual { get; set; }

        private bool _selecionado;
        public bool Selecionado
        {
            get => _selecionado;
            set => SetProperty(ref _selecionado, value);
        }
    }
}
