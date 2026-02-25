using SistemaJuridico.Infrastructure;
using SistemaJuridico.Services;
using SistemaJuridico.Views;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;

namespace SistemaJuridico.ViewModels
{
    public class ProcessoListViewModel : ViewModelBase
    {
        private readonly ProcessoCacheService _cacheService;

        private readonly List<ProcessoBuscaItemVM> _todos = new();
        private List<ProcessoBuscaItemVM> _filtrados = new();

        public ObservableCollection<ProcessoBuscaItemVM> Processos { get; } = new();
        public ObservableCollection<ProcessoBuscaItemVM> SugestoesBusca { get; } = new();
        public ObservableCollection<ProcessoBuscaItemVM> ResultadosBuscaRapida { get; } = new();

        private ProcessoBuscaItemVM? _processoSelecionado;
        public ProcessoBuscaItemVM? ProcessoSelecionado
        {
            get => _processoSelecionado;
            set => SetProperty(ref _processoSelecionado, value);
        }

        private string _textoBusca = string.Empty;
        public string TextoBusca
        {
            get => _textoBusca;
            set
            {
                if (SetProperty(ref _textoBusca, value))
                {
                    PaginaAtual = 1;
                    AplicarBusca();

                    var termo = _textoBusca?.Trim() ?? string.Empty;
                    MostrarResultadosBuscaRapida = termo.Length >= 1;
                    AtualizarBuscaRapida();
                }
            }
        }

        private bool _mostrarSugestoesBusca;
        public bool MostrarSugestoesBusca
        {
            get => _mostrarSugestoesBusca;
            set => SetProperty(ref _mostrarSugestoesBusca, value);
        }

        private bool _mostrarResultadosBuscaRapida;
        public bool MostrarResultadosBuscaRapida
        {
            get => _mostrarResultadosBuscaRapida;
            set => SetProperty(ref _mostrarResultadosBuscaRapida, value);
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

        private string _responsavelFiltro = string.Empty;
        public string ResponsavelFiltro
        {
            get => _responsavelFiltro;
            set => SetProperty(ref _responsavelFiltro, value);
        }

        private decimal? _valorMinimo;
        public decimal? ValorMinimo
        {
            get => _valorMinimo;
            set => SetProperty(ref _valorMinimo, value);
        }

        private decimal? _valorMaximo;
        public decimal? ValorMaximo
        {
            get => _valorMaximo;
            set => SetProperty(ref _valorMaximo, value);
        }

        private string _palavraChaveHistorico = string.Empty;
        public string PalavraChaveHistorico
        {
            get => _palavraChaveHistorico;
            set => SetProperty(ref _palavraChaveHistorico, value);
        }

        private int _paginaAtual = 1;
        public int PaginaAtual
        {
            get => _paginaAtual;
            set => SetProperty(ref _paginaAtual, value);
        }

        private int _itensPorPagina = 25;
        public int ItensPorPagina
        {
            get => _itensPorPagina;
            set => SetProperty(ref _itensPorPagina, value);
        }

        private int _totalPaginas = 1;
        public int TotalPaginas
        {
            get => _totalPaginas;
            set => SetProperty(ref _totalPaginas, value);
        }

        private int _totalResultados;
        public int TotalResultados
        {
            get => _totalResultados;
            set => SetProperty(ref _totalResultados, value);
        }

        private bool _mostrarBuscaAvancada;
        public bool MostrarBuscaAvancada
        {
            get => _mostrarBuscaAvancada;
            set => SetProperty(ref _mostrarBuscaAvancada, value);
        }

        private string _resumoEstatistico = string.Empty;
        public string ResumoEstatistico
        {
            get => _resumoEstatistico;
            set => SetProperty(ref _resumoEstatistico, value);
        }

        public RelayCommand CarregarCommand { get; }
        public RelayCommand NovoProcessoCommand { get; }
        public RelayCommand<ProcessoBuscaItemVM?> AbrirProcessoCommand { get; }
        public RelayCommand BuscarCommand { get; }
        public RelayCommand LimparFiltrosCommand { get; }
        public RelayCommand ToggleBuscaAvancadaCommand { get; }
        public RelayCommand ExportarResultadosCommand { get; }
        public RelayCommand ProximaPaginaCommand { get; }
        public RelayCommand PaginaAnteriorCommand { get; }
        public RelayCommand<ProcessoBuscaItemVM?> SelecionarSugestaoBuscaCommand { get; }
        public RelayCommand<ProcessoBuscaItemVM?> AbrirResultadoBuscaRapidaCommand { get; }

        public ProcessoListViewModel()
        {
            _cacheService = new ProcessoCacheService(new DatabaseService());

            CarregarCommand = new RelayCommand(async () => await Carregar());
            NovoProcessoCommand = new RelayCommand(NovoProcesso);
            AbrirProcessoCommand = new RelayCommand<ProcessoBuscaItemVM?>(AbrirProcesso);
            BuscarCommand = new RelayCommand(Buscar);
            LimparFiltrosCommand = new RelayCommand(LimparFiltros);
            ToggleBuscaAvancadaCommand = new RelayCommand(() => MostrarBuscaAvancada = !MostrarBuscaAvancada);
            ExportarResultadosCommand = new RelayCommand(ExportarResultados);
            ProximaPaginaCommand = new RelayCommand(() => MudarPagina(+1));
            PaginaAnteriorCommand = new RelayCommand(() => MudarPagina(-1));
            SelecionarSugestaoBuscaCommand = new RelayCommand<ProcessoBuscaItemVM?>(SelecionarSugestaoBusca);
            AbrirResultadoBuscaRapidaCommand = new RelayCommand<ProcessoBuscaItemVM?>(AbrirProcesso);

            CarregarComSeguranca();
        }

        private async Task Carregar()
        {
            try
            {
                _todos.Clear();
                PopularComCache(_cacheService.ObterCacheLeve());
                AplicarBusca();

                var atualizados = await Task.Run(() => _cacheService.AtualizarCache());
                _todos.Clear();
                PopularComCache(atualizados);
                AplicarBusca();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Falha ao carregar processos: {ex.Message}");
            }
        }

        private async void CarregarComSeguranca()
        {
            await Carregar();
        }

        private void PopularComCache(List<ProcessoResumoCacheItem> cache)
        {
            foreach (var p in cache)
            {
                _todos.Add(new ProcessoBuscaItemVM
                {
                    Id = p.ProcessoId,
                    Numero = p.Numero,
                    Paciente = p.Paciente,
                    Genitor = p.Genitor,
                    Juiz = p.Juiz,
                    TipoProcesso = p.TipoProcesso,
                    StatusProcesso = p.StatusProcesso,
                    StatusCalculado = p.StatusCalculado,
                    Responsavel = p.ResponsavelUltimaVerificacao,
                    Saldo = p.TotalCredito - p.TotalDebito
                });
            }
        }

        private void AplicarBusca()
        {
            IEnumerable<ProcessoBuscaItemVM> consulta = _todos;

            if (!string.IsNullOrWhiteSpace(TextoBusca))
            {
                var termo = TextoBusca.Trim();
                consulta = consulta.Where(x =>
                    Contem(x.Numero, termo) ||
                    Contem(x.Paciente, termo) ||
                    Contem(x.Genitor, termo) ||
                    Contem(x.Juiz, termo) ||
                    Contem(x.TipoProcesso, termo) ||
                    Contem(x.StatusCalculado, termo) ||
                    Contem(x.StatusProcesso, termo));
            }

            if (!string.IsNullOrWhiteSpace(StatusFiltro))
                consulta = consulta.Where(x => Contem(x.StatusCalculado, StatusFiltro) || Contem(x.StatusProcesso, StatusFiltro));

            if (!string.IsNullOrWhiteSpace(TipoFiltro))
                consulta = consulta.Where(x => Contem(x.TipoProcesso, TipoFiltro));

            if (!string.IsNullOrWhiteSpace(ResponsavelFiltro))
                consulta = consulta.Where(x => Contem(x.Responsavel, ResponsavelFiltro));

            if (ValorMinimo.HasValue)
                consulta = consulta.Where(x => x.Saldo >= ValorMinimo.Value);

            if (ValorMaximo.HasValue)
                consulta = consulta.Where(x => x.Saldo <= ValorMaximo.Value);

            if (!string.IsNullOrWhiteSpace(PalavraChaveHistorico))
                consulta = consulta.Where(x =>
                    Contem(x.Numero, PalavraChaveHistorico) ||
                    Contem(x.Paciente, PalavraChaveHistorico) ||
                    Contem(x.StatusCalculado, PalavraChaveHistorico));

            _filtrados = consulta.OrderBy(x => x.Numero).ToList();
            TotalResultados = _filtrados.Count;
            TotalPaginas = Math.Max(1, (int)Math.Ceiling((double)TotalResultados / ItensPorPagina));
            PaginaAtual = Math.Min(PaginaAtual, TotalPaginas);
            PaginaAtual = Math.Max(1, PaginaAtual);

            var pagina = _filtrados
                .Skip((PaginaAtual - 1) * ItensPorPagina)
                .Take(ItensPorPagina)
                .ToList();

            Processos.Clear();
            foreach (var p in pagina)
                Processos.Add(p);

            var porStatus = _filtrados
                .GroupBy(x => string.IsNullOrWhiteSpace(x.StatusCalculado) ? "Sem status" : x.StatusCalculado)
                .Select(x => $"{x.Key}: {x.Count()}");

            ResumoEstatistico = $"Resultados: {TotalResultados} | " + string.Join(" | ", porStatus.Take(5));

            AtualizarBuscaRapida();
        }

        private void AtualizarBuscaRapida()
        {
            var termo = TextoBusca?.Trim() ?? string.Empty;

            SugestoesBusca.Clear();
            ResultadosBuscaRapida.Clear();

            if (string.IsNullOrWhiteSpace(termo))
            {
                MostrarSugestoesBusca = false;
                MostrarResultadosBuscaRapida = false;
                return;
            }

            var resultados = _todos
                .Where(x =>
                    Contem(x.Numero, termo) ||
                    Contem(x.Paciente, termo) ||
                    Contem(x.Genitor, termo) ||
                    Contem(x.Juiz, termo) ||
                    Contem(x.Responsavel, termo) ||
                    Contem(x.TipoProcesso, termo) ||
                    Contem(x.StatusCalculado, termo) ||
                    Contem(x.StatusProcesso, termo))
                .OrderBy(x => x.Numero)
                .Take(30)
                .ToList();

            foreach (var item in resultados.Take(8))
                SugestoesBusca.Add(item);

            MostrarSugestoesBusca = SugestoesBusca.Count > 0;

            if (MostrarResultadosBuscaRapida)
            {
                foreach (var item in resultados)
                    ResultadosBuscaRapida.Add(item);
            }
        }

        private void SelecionarSugestaoBusca(ProcessoBuscaItemVM? processo)
        {
            if (processo == null)
                return;

            TextoBusca = processo.Numero;
            MostrarSugestoesBusca = false;
            AbrirProcesso(processo);
        }

        private static bool Contem(string? fonte, string termo)
            => !string.IsNullOrWhiteSpace(fonte) && fonte.Contains(termo, StringComparison.OrdinalIgnoreCase);

        private void LimparFiltros()
        {
            TextoBusca = string.Empty;
            StatusFiltro = string.Empty;
            TipoFiltro = string.Empty;
            ResponsavelFiltro = string.Empty;
            ValorMinimo = null;
            ValorMaximo = null;
            PalavraChaveHistorico = string.Empty;
            PaginaAtual = 1;
            MostrarSugestoesBusca = false;
            MostrarResultadosBuscaRapida = false;
            ResultadosBuscaRapida.Clear();
            AplicarBusca();
        }

        private void Buscar()
        {
            PaginaAtual = 1;
            AplicarBusca();

            if (string.IsNullOrWhiteSpace(TextoBusca) || TextoBusca.Trim().Length < 1)
            {
                MostrarResultadosBuscaRapida = false;
                ResultadosBuscaRapida.Clear();
                return;
            }

            MostrarResultadosBuscaRapida = true;
            AtualizarBuscaRapida();
            MostrarSugestoesBusca = false;
        }

        private void MudarPagina(int delta)
        {
            var nova = PaginaAtual + delta;
            if (nova < 1 || nova > TotalPaginas)
                return;

            PaginaAtual = nova;
            AplicarBusca();
        }

        private void ExportarResultados()
        {
            var save = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV|*.csv",
                FileName = $"busca_processos_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (save.ShowDialog() != true)
                return;

            var sb = new StringBuilder();
            sb.AppendLine("Numero;Paciente;Genitor;Juiz;Tipo;StatusCalculado;Responsavel;Saldo");

            foreach (var p in _filtrados)
            {
                sb.AppendLine(string.Join(';',
                    Escapar(p.Numero),
                    Escapar(p.Paciente),
                    Escapar(p.Genitor),
                    Escapar(p.Juiz),
                    Escapar(p.TipoProcesso),
                    Escapar(p.StatusCalculado),
                    Escapar(p.Responsavel),
                    p.Saldo.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"))));
            }

            File.WriteAllText(save.FileName, sb.ToString(), Encoding.UTF8);
            System.Windows.MessageBox.Show("Resultados exportados com sucesso.");
        }

        private static string Escapar(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return string.Empty;

            return valor.Replace(';', ',');
        }

        private void NovoProcesso()
        {
            var window = new CadastroProcessoWindow();
            window.ShowDialog();
            CarregarComSeguranca();
        }

        private void AbrirProcesso(ProcessoBuscaItemVM? processo)
        {
            if (processo == null || string.IsNullOrWhiteSpace(processo.Id))
                return;

            MostrarSugestoesBusca = false;
            MostrarResultadosBuscaRapida = false;

            var window = new ProcessoDetalhesWindow(processo.Id);
            window.ShowDialog();
            CarregarComSeguranca();
        }
    }

    public class ProcessoBuscaItemVM
    {
        public string Id { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string Paciente { get; set; } = string.Empty;
        public string? Genitor { get; set; }
        public string Juiz { get; set; } = string.Empty;
        public string? TipoProcesso { get; set; }
        public string StatusProcesso { get; set; } = string.Empty;
        public string? StatusCalculado { get; set; }
        public string? Responsavel { get; set; }
        public decimal Saldo { get; set; }
    }
}
