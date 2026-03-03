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
using System.Text.Json;
using System.Windows;

namespace SistemaJuridico.ViewModels
{
    public enum TipoPrestacao { Diaria, Adiantamento, Reembolso, Convenio, Outro }
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

        [ObservableProperty] private TipoPrestacao? _tipoSelecionado;
        [ObservableProperty] private decimal _valorTotal;
        [ObservableProperty] private string _responsavel = string.Empty;
        [ObservableProperty] private string _observacoes = string.Empty;
        [ObservableProperty] private PrestacaoStatus _status = PrestacaoStatus.Rascunho;
        [ObservableProperty] private string _numeroProcesso = string.Empty;
        [ObservableProperty] private string _dataPrestacao = DateTime.Now.ToString("dd/MM/yyyy");
        [ObservableProperty] private string _prestacaoId = string.Empty;
        [ObservableProperty] private Conta? _prestacaoSelecionada;

        [ObservableProperty] private string _dataInicio = string.Empty;
        [ObservableProperty] private string _dataFim = string.Empty;
        [ObservableProperty] private string _destino = string.Empty;
        [ObservableProperty] private string _justificativa = string.Empty;
        [ObservableProperty] private decimal _valorPorDia;

        [ObservableProperty] private string _numeroDocumentoFiscal = string.Empty;
        [ObservableProperty] private string _dataDocumento = string.Empty;
        [ObservableProperty] private string _cnpjFornecedor = string.Empty;
        [ObservableProperty] private string _descricaoServico = string.Empty;
        [ObservableProperty] private decimal _valorDocumento;

        [ObservableProperty] private string _numeroConvenio = string.Empty;
        [ObservableProperty] private string _orgaoRepassador = string.Empty;
        [ObservableProperty] private string _periodoExecucao = string.Empty;
        [ObservableProperty] private string _relatorioDetalhado = string.Empty;
        [ObservableProperty] private string _detalhesOutroTipo = string.Empty;
        [ObservableProperty] private string _novoAnexo = string.Empty;

        public ObservableCollection<string> Anexos { get; } = new();
        public ObservableCollection<HistoricoPrestacao> Historico { get; } = new();
        public ObservableCollection<Conta> PrestacoesRealizadas { get; } = new();
        public Array TiposPrestacaoDisponiveis => Enum.GetValues(typeof(TipoPrestacao));

        public bool ExibirCampos => TipoSelecionado.HasValue;
        public bool IsDiaria => TipoSelecionado == TipoPrestacao.Diaria;
        public bool IsReembolso => TipoSelecionado == TipoPrestacao.Reembolso;
        public bool IsConvenio => TipoSelecionado == TipoPrestacao.Convenio;
        public bool IsOutro => TipoSelecionado == TipoPrestacao.Outro;
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
            NumeroProcesso = processoId;
            Responsavel = App.Session.UsuarioAtual?.Nome ?? "Sistema";

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
            OnPropertyChanged(nameof(IsDiaria));
            OnPropertyChanged(nameof(IsReembolso));
            OnPropertyChanged(nameof(IsConvenio));
            RecalcularValorDiaria();
        }

        partial void OnDataInicioChanged(string value) => RecalcularValorDiaria();
        partial void OnDataFimChanged(string value) => RecalcularValorDiaria();
        partial void OnValorTotalChanged(decimal value) => RecalcularValorDiaria();

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

            var campos = JsonSerializer.Deserialize<Dictionary<string, string>>(PrestacaoSelecionada.CamposEspecificosJson ?? "{}") ?? new();
            DataInicio = campos.GetValueOrDefault("Data Início", string.Empty);
            DataFim = campos.GetValueOrDefault("Data Fim", string.Empty);
            Destino = campos.GetValueOrDefault("Destino", string.Empty);
            Justificativa = campos.GetValueOrDefault("Justificativa", string.Empty);
            NumeroDocumentoFiscal = campos.GetValueOrDefault("Número Documento Fiscal", string.Empty);
            DataDocumento = campos.GetValueOrDefault("Data Documento", string.Empty);
            CnpjFornecedor = campos.GetValueOrDefault("CNPJ Fornecedor", string.Empty);
            DescricaoServico = campos.GetValueOrDefault("Descrição Serviço", string.Empty);
            ValorDocumento = decimal.TryParse(campos.GetValueOrDefault("Valor Documento"), out var vd) ? vd : 0;
            NumeroConvenio = campos.GetValueOrDefault("Número Convênio", string.Empty);
            OrgaoRepassador = campos.GetValueOrDefault("Órgão Repassador", string.Empty);
            PeriodoExecucao = campos.GetValueOrDefault("Período Execução", string.Empty);
            RelatorioDetalhado = campos.GetValueOrDefault("Relatório Detalhado", string.Empty);
            DetalhesOutroTipo = campos.GetValueOrDefault("Detalhes", string.Empty);

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
            DataInicio = DataFim = Destino = Justificativa = string.Empty;
            NumeroDocumentoFiscal = DataDocumento = CnpjFornecedor = DescricaoServico = string.Empty;
            ValorDocumento = 0;
            NumeroConvenio = OrgaoRepassador = PeriodoExecucao = RelatorioDetalhado = DetalhesOutroTipo = string.Empty;
            Anexos.Clear();
            Historico.Clear();
        }

        private void CarregarPrestacoes()
        {
            PrestacoesRealizadas.Clear();
            foreach (var item in _contaService.ListarPorProcesso(_processoId).OrderByDescending(x => ParseData(x.UltimaModificacao) ?? DateTime.MinValue))
                PrestacoesRealizadas.Add(item);
        }

        private bool ValidarFinalizacao()
        {
            if (!TipoSelecionado.HasValue || string.IsNullOrWhiteSpace(NumeroProcesso) || string.IsNullOrWhiteSpace(DataPrestacao)
                || ValorTotal <= 0 || string.IsNullOrWhiteSpace(Responsavel))
            {
                System.Windows.MessageBox.Show("Preencha todos os campos obrigatórios fixos e valor total > 0.");
                return false;
            }

            if (!DateTime.TryParse(DataPrestacao, out _))
            {
                System.Windows.MessageBox.Show("Data da prestação inválida.");
                return false;
            }

            if (IsDiaria)
            {
                if (!DateTime.TryParse(DataInicio, out var ini) || !DateTime.TryParse(DataFim, out var fim) || ini > fim
                    || string.IsNullOrWhiteSpace(Destino) || string.IsNullOrWhiteSpace(Justificativa))
                {
                    System.Windows.MessageBox.Show("Dados de diária inválidos. Verifique período e campos obrigatórios.");
                    return false;
                }
            }

            if (IsReembolso)
            {
                if (string.IsNullOrWhiteSpace(NumeroDocumentoFiscal) || !DateTime.TryParse(DataDocumento, out _)
                    || string.IsNullOrWhiteSpace(CnpjFornecedor) || string.IsNullOrWhiteSpace(DescricaoServico)
                    || ValorDocumento <= 0 || Anexos.Count == 0)
                {
                    System.Windows.MessageBox.Show("Reembolso exige documento fiscal válido e anexo obrigatório.");
                    return false;
                }
            }

            if (IsConvenio && (string.IsNullOrWhiteSpace(NumeroConvenio) || string.IsNullOrWhiteSpace(OrgaoRepassador)
                || string.IsNullOrWhiteSpace(PeriodoExecucao) || string.IsNullOrWhiteSpace(RelatorioDetalhado)))
            {
                System.Windows.MessageBox.Show("Convênio exige relatório detalhado e dados obrigatórios.");
                return false;
            }

            if (IsOutro && string.IsNullOrWhiteSpace(DetalhesOutroTipo))
            {
                System.Windows.MessageBox.Show("Informe a descrição detalhada para o tipo Outro.");
                return false;
            }

            return true;
        }

        private Conta ConstruirConta(PrestacaoStatus status)
        {
            var id = string.IsNullOrWhiteSpace(PrestacaoId) ? Guid.NewGuid().ToString() : PrestacaoId;
            PrestacaoId = id;
            Status = status;

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
            if (IsDiaria)
            {
                campos["Data Início"] = DataInicio;
                campos["Data Fim"] = DataFim;
                campos["Destino"] = Destino;
                campos["Justificativa"] = Justificativa;
                campos["Valor por Dia"] = ValorPorDia.ToString("N2");
            }
            else if (IsReembolso)
            {
                campos["Número Documento Fiscal"] = NumeroDocumentoFiscal;
                campos["Data Documento"] = DataDocumento;
                campos["CNPJ Fornecedor"] = CnpjFornecedor;
                campos["Descrição Serviço"] = DescricaoServico;
                campos["Valor Documento"] = ValorDocumento.ToString("N2");
            }
            else if (IsConvenio)
            {
                campos["Número Convênio"] = NumeroConvenio;
                campos["Órgão Repassador"] = OrgaoRepassador;
                campos["Período Execução"] = PeriodoExecucao;
                campos["Relatório Detalhado"] = RelatorioDetalhado;
            }
            else if (IsOutro)
            {
                campos["Detalhes"] = DetalhesOutroTipo;
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

        private void RecalcularValorDiaria()
        {
            if (!IsDiaria || !DateTime.TryParse(DataInicio, out var ini) || !DateTime.TryParse(DataFim, out var fim) || fim < ini)
            {
                ValorPorDia = 0;
                return;
            }

            var dias = (fim - ini).Days + 1;
            ValorPorDia = dias > 0 ? ValorTotal / dias : 0;
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
