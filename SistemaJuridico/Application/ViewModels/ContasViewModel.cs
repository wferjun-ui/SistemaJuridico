using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dapper;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaJuridico.Models;
using SistemaJuridico.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace SistemaJuridico.ViewModels
{
    public enum TipoPrestacao { Alvara, Tratamento, OutrasDespesas }
    public enum PrestacaoStatus { Rascunho, Finalizada, Reaberta, Rejeitada }

    public class HistoricoPrestacao
    {
        public DateTime Data { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string Acao { get; set; } = string.Empty;
        public string Observacao { get; set; } = string.Empty;
        public string DataFormatada => Data.ToString("dd/MM/yyyy HH:mm");
    }

    public partial class ContasViewModel : ObservableObject
    {
        private readonly string _processoId;
        private readonly ContaService _contaService;
        private readonly DatabaseService _databaseService;
        private readonly ItemSaudeService _itemSaudeService;

        [ObservableProperty] private TipoPrestacao? _tipoSelecionado;
        [ObservableProperty] private decimal _valorTotal;
        [ObservableProperty] private string _responsavel = string.Empty;
        [ObservableProperty] private string _observacoes = string.Empty;
        [ObservableProperty] private PrestacaoStatus _status = PrestacaoStatus.Rascunho;
        [ObservableProperty] private string _numeroProcesso = string.Empty;
        [ObservableProperty] private string _dataPrestacao = DateTime.Now.ToString("dd/MM/yyyy");
        [ObservableProperty] private string _prestacaoId = string.Empty;
        [ObservableProperty] private Conta? _prestacaoSelecionada;

        [ObservableProperty] private string _destino = string.Empty;
        [ObservableProperty] private string _numeroAlvara = string.Empty;
        [ObservableProperty] private string _dataLevantamento = string.Empty;
        [ObservableProperty] private decimal _valorAlvara;

        [ObservableProperty] private string _tratamentoSelecionado = string.Empty;

        [ObservableProperty] private string _numeroDocumentoFiscal = string.Empty;
        [ObservableProperty] private bool _isRecibo;
        [ObservableProperty] private string _dataDocumento = string.Empty;
        [ObservableProperty] private string _cnpjFornecedor = string.Empty;
        [ObservableProperty] private decimal _valorDocumento;

        [ObservableProperty] private string _detalhesOutroTipo = string.Empty;
        [ObservableProperty] private string _novoAnexo = string.Empty;

        public ObservableCollection<string> Anexos { get; } = new();
        public ObservableCollection<HistoricoPrestacao> Historico { get; } = new();
        public ObservableCollection<Conta> PrestacoesRealizadas { get; } = new();
        public ObservableCollection<string> TratamentosDisponiveis { get; } = new();
        public Array TiposPrestacaoDisponiveis => Enum.GetValues(typeof(TipoPrestacao));

        public bool ExibirCampos => TipoSelecionado.HasValue;
        public bool IsAlvara => TipoSelecionado == TipoPrestacao.Alvara;
        public bool IsTratamento => TipoSelecionado == TipoPrestacao.Tratamento;
        public bool IsOutrasDespesas => TipoSelecionado == TipoPrestacao.OutrasDespesas;
        public bool GerarPdfHabilitado => Status == PrestacaoStatus.Finalizada && !string.IsNullOrWhiteSpace(PrestacaoId);
        public string StatusDescricao => Status.ToString();
        public string StatusCor => Status switch
        {
            PrestacaoStatus.Rascunho => "#F59E0B",
            PrestacaoStatus.Finalizada => "#16A34A",
            PrestacaoStatus.Rejeitada => "#DC2626",
            _ => "#2563EB"
        };

        public ContasViewModel() : this(string.Empty) { }

        public ContasViewModel(string processoId)
        {
            _processoId = processoId;
            _databaseService = new DatabaseService();
            _contaService = new ContaService(_databaseService);
            _itemSaudeService = new ItemSaudeService(_databaseService);
            NumeroProcesso = processoId;
            Responsavel = App.Session.UsuarioAtual?.Nome ?? "Sistema";

            CarregarTratamentosDisponiveis();
            CarregarPrestacoes();
            InicializarNovaPrestacao();
        }

        partial void OnTipoSelecionadoChanged(TipoPrestacao? value)
        {
            if (value.HasValue)
            {
                InicializarNovaPrestacao();
                TipoSelecionado = value;
            }

            OnPropertyChanged(nameof(ExibirCampos));
            OnPropertyChanged(nameof(IsAlvara));
            OnPropertyChanged(nameof(IsTratamento));
            OnPropertyChanged(nameof(IsOutrasDespesas));
        }

        partial void OnIsReciboChanged(bool value)
        {
            if (value)
                NumeroDocumentoFiscal = string.Empty;
        }

        [RelayCommand]
        private void SalvarRascunho()
        {
            if (!TipoSelecionado.HasValue)
            {
                System.Windows.MessageBox.Show("Selecione o tipo de prestação.");
                return;
            }

            var conta = ConstruirConta(PrestacaoStatus.Rascunho);
            conta.UltimaModificacao = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            var acao = string.IsNullOrWhiteSpace(PrestacaoId) ? "Criação do rascunho" : "Atualização de rascunho";
            PersistirPrestacao(conta);
            RegistrarHistorico(conta.Id, acao, "Prestação salva como rascunho.");
            CarregarPrestacoes();
            System.Windows.MessageBox.Show("Rascunho salvo com sucesso.");
        }

        [RelayCommand]
        private void FinalizarPrestacao()
        {
            if (!ValidarFinalizacao())
                return;

            var conta = ConstruirConta(PrestacaoStatus.Finalizada);
            conta.UltimaModificacao = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            PersistirPrestacao(conta);
            RegistrarHistorico(conta.Id, "Finalização", "Prestação finalizada com validação completa.");
            Status = PrestacaoStatus.Finalizada;
            CarregarPrestacoes();
            System.Windows.MessageBox.Show("Prestação finalizada.");
        }

        [RelayCommand]
        private void GerarPdf()
        {
            if (!GerarPdfHabilitado)
                return;

            var conta = _contaService.ListarPorProcesso(_processoId).FirstOrDefault(x => x.Id == PrestacaoId);
            if (conta is null)
                return;

            var destino = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"prestacao-{conta.Id}.pdf");
            var campos = JsonSerializer.Deserialize<Dictionary<string, string>>(conta.CamposEspecificosJson ?? "{}") ?? new();
            var anexos = JsonSerializer.Deserialize<List<string>>(conta.AnexosJson ?? "[]") ?? new();

            Document.Create(c =>
            {
                c.Page(p =>
                {
                    p.Margin(20);
                    p.Header().Column(col =>
                    {
                        col.Item().Text("[Logo da instituição]").Bold();
                        col.Item().Text("Órgão Responsável");
                        col.Item().Text($"Processo: {conta.ProcessoId}");
                        col.Item().Text($"Tipo da prestação: {conta.TipoLancamento}").Bold();
                    });

                    p.Content().Column(col =>
                    {
                        col.Item().PaddingTop(10).Text("Dados Gerais").Bold();
                        col.Item().Text($"Responsável: {conta.Responsavel}");
                        col.Item().Text($"Data: {conta.DataMovimentacao}");
                        col.Item().Text($"Valor total: {conta.ValorConta.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))}");
                        col.Item().Text($"Status: {conta.StatusConta}");
                        col.Item().PaddingTop(10).Text("Dados específicos").Bold();
                        foreach (var campo in campos)
                            col.Item().Text($"{campo.Key}: {campo.Value}");

                        col.Item().PaddingTop(10).Text("Anexos").Bold();
                        foreach (var anexo in anexos)
                            col.Item().Text($"- {anexo}");

                        col.Item().PaddingTop(18).Text("Assinaturas: ______________________________");
                    });
                });
            }).GeneratePdf(destino);

            conta.PdfPath = destino;
            PersistirPrestacao(conta);
            RegistrarHistorico(conta.Id, "PDF gerado", "PDF gerado");
            CarregarHistorico(conta.Id);
            System.Windows.MessageBox.Show("PDF gerado com sucesso.");
        }

        [RelayCommand]
        private void AdicionarAnexo()
        {
            if (string.IsNullOrWhiteSpace(NovoAnexo))
                return;

            Anexos.Add(NovoAnexo.Trim());
            NovoAnexo = string.Empty;
        }

        [RelayCommand]
        private void RemoverAnexo(string? anexo)
        {
            if (string.IsNullOrWhiteSpace(anexo))
                return;

            Anexos.Remove(anexo);
        }

        [RelayCommand]
        private void CarregarPrestacaoSelecionada()
        {
            if (PrestacaoSelecionada is null)
                return;

            PrestacaoId = PrestacaoSelecionada.Id;
            NumeroProcesso = PrestacaoSelecionada.ProcessoId;
            DataPrestacao = PrestacaoSelecionada.DataMovimentacao;
            ValorTotal = PrestacaoSelecionada.ValorConta;
            Responsavel = PrestacaoSelecionada.Responsavel;
            Observacoes = PrestacaoSelecionada.Observacoes ?? string.Empty;
            Status = ParseStatus(PrestacaoSelecionada.StatusConta);
            TipoSelecionado = Enum.TryParse<TipoPrestacao>(PrestacaoSelecionada.TipoLancamento, true, out var tipo) ? tipo : null;

            var campos = JsonSerializer.Deserialize<Dictionary<string, string?>>(PrestacaoSelecionada.CamposEspecificosJson ?? "{}") ?? new();
            Destino = campos.GetValueOrDefault("Destino", string.Empty) ?? string.Empty;
            NumeroAlvara = campos.GetValueOrDefault("Número do Alvará", campos.GetValueOrDefault("Número Documento Fiscal", string.Empty)) ?? string.Empty;
            DataLevantamento = campos.GetValueOrDefault("Data Levantamento", campos.GetValueOrDefault("Data Documento", string.Empty)) ?? string.Empty;
            ValorAlvara = decimal.TryParse(campos.GetValueOrDefault("Valor do Alvará"), out var va) ? va : 0;

            TratamentoSelecionado = campos.GetValueOrDefault("Tratamento", campos.GetValueOrDefault("Descrição Serviço", string.Empty)) ?? string.Empty;
            NumeroDocumentoFiscal = campos.GetValueOrDefault("Número da NF", campos.GetValueOrDefault("Número Documento Fiscal", string.Empty)) ?? string.Empty;
            IsRecibo = bool.TryParse(campos.GetValueOrDefault("Recibo"), out var recibo) && recibo;
            DataDocumento = campos.GetValueOrDefault("Data da NF", campos.GetValueOrDefault("Data Documento", string.Empty)) ?? string.Empty;
            CnpjFornecedor = campos.GetValueOrDefault("CNPJ Fornecedor", string.Empty) ?? string.Empty;
            ValorDocumento = decimal.TryParse(campos.GetValueOrDefault("Valor da NF", campos.GetValueOrDefault("Valor Documento")), out var vd) ? vd : 0;
            DetalhesOutroTipo = campos.GetValueOrDefault("Do que se trata", campos.GetValueOrDefault("Detalhes", string.Empty)) ?? string.Empty;

            Anexos.Clear();
            foreach (var item in JsonSerializer.Deserialize<List<string>>(PrestacaoSelecionada.AnexosJson ?? "[]") ?? new())
                Anexos.Add(item);

            CarregarHistorico(PrestacaoSelecionada.Id);
        }

        private void InicializarNovaPrestacao()
        {
            PrestacaoId = string.Empty;
            DataPrestacao = DateTime.Now.ToString("dd/MM/yyyy");
            ValorTotal = 0;
            Observacoes = string.Empty;
            Status = PrestacaoStatus.Rascunho;
            Destino = NumeroAlvara = DataLevantamento = string.Empty;
            ValorAlvara = 0;
            TratamentoSelecionado = string.Empty;
            NumeroDocumentoFiscal = DataDocumento = CnpjFornecedor = string.Empty;
            IsRecibo = false;
            ValorDocumento = 0;
            DetalhesOutroTipo = string.Empty;
            Anexos.Clear();
            Historico.Clear();
        }

        private void CarregarTratamentosDisponiveis()
        {
            TratamentosDisponiveis.Clear();
            if (string.IsNullOrWhiteSpace(_processoId))
                return;

            var tratamentos = _itemSaudeService
                .ListarPorProcesso(_processoId)
                .Where(x => !x.IsDesnecessario && !string.IsNullOrWhiteSpace(x.Nome))
                .Select(x => x.Nome.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x);

            foreach (var tratamento in tratamentos)
                TratamentosDisponiveis.Add(tratamento);
        }

        private void CarregarPrestacoes()
        {
            PrestacoesRealizadas.Clear();
            foreach (var item in _contaService.ListarPorProcesso(_processoId).OrderByDescending(x => ParseData(x.UltimaModificacao) ?? DateTime.MinValue))
                PrestacoesRealizadas.Add(item);
        }

        private bool ValidarFinalizacao()
        {
            if (!TipoSelecionado.HasValue || string.IsNullOrWhiteSpace(DataPrestacao) || string.IsNullOrWhiteSpace(Responsavel))
            {
                System.Windows.MessageBox.Show("Preencha os campos obrigatórios da prestação.");
                return false;
            }

            if (!DateTime.TryParse(DataPrestacao, out _))
            {
                System.Windows.MessageBox.Show("Data da prestação inválida.");
                return false;
            }

            if (IsAlvara)
            {
                if (string.IsNullOrWhiteSpace(Destino)
                    || string.IsNullOrWhiteSpace(NumeroAlvara)
                    || !DateTime.TryParse(DataLevantamento, out _)
                    || ValorAlvara <= 0)
                {
                    System.Windows.MessageBox.Show("Para alvará, informe destino, número, data de levantamento e valor do alvará.");
                    return false;
                }
            }

            if (IsTratamento)
            {
                if (string.IsNullOrWhiteSpace(TratamentoSelecionado)
                    || (!IsRecibo && string.IsNullOrWhiteSpace(NumeroDocumentoFiscal))
                    || !DateTime.TryParse(DataDocumento, out _)
                    || ValorDocumento <= 0
                    || string.IsNullOrWhiteSpace(CnpjFornecedor))
                {
                    System.Windows.MessageBox.Show("Tratamento exige tratamento selecionado, NF/recibo, data, valor e CNPJ.");
                    return false;
                }
            }

            if (IsOutrasDespesas && (string.IsNullOrWhiteSpace(DetalhesOutroTipo)
                || (!IsRecibo && string.IsNullOrWhiteSpace(NumeroDocumentoFiscal))
                || !DateTime.TryParse(DataDocumento, out _)
                || ValorDocumento <= 0))
            {
                System.Windows.MessageBox.Show("Outras despesas exige descrição, NF/recibo, data da NF e valor da NF.");
                return false;
            }

            return true;
        }

        private Conta ConstruirConta(PrestacaoStatus status)
        {
            var id = string.IsNullOrWhiteSpace(PrestacaoId) ? Guid.NewGuid().ToString() : PrestacaoId;
            PrestacaoId = id;
            Status = status;

            if (IsAlvara)
                ValorTotal = ValorAlvara;
            else if (IsTratamento || IsOutrasDespesas)
                ValorTotal = ValorDocumento;

            return new Conta
            {
                Id = id,
                ProcessoId = NumeroProcesso,
                TipoLancamento = TipoSelecionado?.ToString() ?? string.Empty,
                DataMovimentacao = DataPrestacao,
                ValorConta = ValorTotal,
                Responsavel = Responsavel,
                Observacoes = Observacoes,
                Historico = "Prestação de contas",
                StatusConta = status.ToString(),
                CamposEspecificosJson = JsonSerializer.Serialize(ObterCamposEspecificos()),
                AnexosJson = JsonSerializer.Serialize(Anexos.ToList())
            };
        }

        private Dictionary<string, string> ObterCamposEspecificos()
        {
            var campos = new Dictionary<string, string>();
            if (IsAlvara)
            {
                campos["Destino"] = Destino;
                campos["Número do Alvará"] = NumeroAlvara;
                campos["Data Levantamento"] = DataLevantamento;
                campos["Valor do Alvará"] = ValorAlvara.ToString("N2");
            }
            else if (IsTratamento)
            {
                campos["Tratamento"] = TratamentoSelecionado;
                campos["Número da NF"] = NumeroDocumentoFiscal;
                campos["Recibo"] = IsRecibo.ToString();
                campos["Data da NF"] = DataDocumento;
                campos["CNPJ Fornecedor"] = CnpjFornecedor;
                campos["Valor da NF"] = ValorDocumento.ToString("N2");
            }
            else if (IsOutrasDespesas)
            {
                campos["Do que se trata"] = DetalhesOutroTipo;
                campos["Número da NF"] = NumeroDocumentoFiscal;
                campos["Recibo"] = IsRecibo.ToString();
                campos["Data da NF"] = DataDocumento;
                campos["Valor da NF"] = ValorDocumento.ToString("N2");
            }
            return campos;
        }

        private void PersistirPrestacao(Conta conta)
        {
            var existente = _contaService.ListarPorProcesso(_processoId).Any(x => x.Id == conta.Id);
            if (existente)
                _contaService.Atualizar(conta);
            else
                _contaService.Inserir(conta);

            CarregarHistorico(conta.Id);
            OnPropertyChanged(nameof(GerarPdfHabilitado));
            OnPropertyChanged(nameof(StatusCor));
            OnPropertyChanged(nameof(StatusDescricao));
        }

        private void RegistrarHistorico(string prestacaoId, string acao, string observacao)
        {
            using var conn = _databaseService.GetConnection();
            conn.Execute(@"INSERT INTO prestacao_historico (id, processo_id, prestacao_id, data, usuario, acao, observacao)
VALUES (@Id,@ProcessoId,@PrestacaoId,@Data,@Usuario,@Acao,@Observacao)", new
            {
                Id = Guid.NewGuid().ToString(),
                ProcessoId = _processoId,
                PrestacaoId = prestacaoId,
                Data = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Usuario = App.Session.UsuarioAtual?.Nome ?? "Sistema",
                Acao = acao,
                Observacao = observacao
            });
        }

        private void CarregarHistorico(string prestacaoId)
        {
            Historico.Clear();
            using var conn = _databaseService.GetConnection();
            var itens = conn.Query<HistoricoPrestacaoLinha>(@"SELECT data as Data, usuario as Usuario, acao as Acao, observacao as Observacao
FROM prestacao_historico WHERE prestacao_id=@PrestacaoId ORDER BY data DESC", new { PrestacaoId = prestacaoId }).ToList();

            foreach (var item in itens)
            {
                Historico.Add(new HistoricoPrestacao
                {
                    Data = DateTime.TryParse(item.Data, out var data) ? data : DateTime.Now,
                    Usuario = item.Usuario,
                    Acao = item.Acao,
                    Observacao = item.Observacao
                });
            }
        }

        private static DateTime? ParseData(string? valor)
            => DateTime.TryParse(valor, out var data) ? data : null;

        private static PrestacaoStatus ParseStatus(string? status)
            => Enum.TryParse<PrestacaoStatus>(status, true, out var parsed) ? parsed : PrestacaoStatus.Rascunho;

        private class HistoricoPrestacaoLinha
        {
            public string Data { get; set; } = string.Empty;
            public string Usuario { get; set; } = string.Empty;
            public string Acao { get; set; } = string.Empty;
            public string Observacao { get; set; } = string.Empty;
        }
    }
}
