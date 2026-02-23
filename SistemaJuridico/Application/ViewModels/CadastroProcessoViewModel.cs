using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaJuridico.Models;
using SistemaJuridico.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace SistemaJuridico.ViewModels
{
    public partial class CadastroProcessoViewModel : ObservableObject
    {
        private const int MinimoCaracteresNumeroProcesso = 15;
        private const string FormatoCnjRegex = @"^\d{7}-\d{2}\.\d{4}\.\d\.\d{2}\.\d{4}$";

        private readonly ProcessService _processService;
        private readonly ItemSaudeService _itemSaudeService;
        private readonly VerificacaoService _verificacaoService;
        private readonly HistoricoService _historicoService;

        public Processo NovoProcesso { get; set; } = new();

        public ObservableCollection<ReuCadastroViewModel> Reus { get; } = new();
        public ObservableCollection<SaudeItemCadastroViewModel> ItensSaudeCadastro { get; } = new();

        public ObservableCollection<string> SugestoesNumeroProcesso { get; } = new();
        public ObservableCollection<string> SugestoesPacientes { get; } = new();
        public ObservableCollection<string> SugestoesRepresentantes { get; } = new();
        public ObservableCollection<string> SugestoesJuizes { get; } = new();
        public ObservableCollection<string> SugestoesReus { get; } = new();

        public ObservableCollection<string> SugestoesMedicamentos { get; } = new();
        public ObservableCollection<string> SugestoesTerapias { get; } = new();
        public ObservableCollection<string> SugestoesCirurgias { get; } = new();
        public ObservableCollection<string> SugestoesOutros { get; } = new();

        public List<string> TiposProcessoDisponiveis { get; } = new() { "Saúde", "Cível", "Criminal", "Outros" };

        [ObservableProperty]
        private bool _isSaving;

        [ObservableProperty]
        private string _selectedTipoProcesso = "Saúde";


        [ObservableProperty]
        private bool _temRepresentante = true;

        [ObservableProperty]
        private string _statusMensagem = "Preencha os dados obrigatórios para salvar o processo.";

        public bool IsProcessoSaude => string.Equals(SelectedTipoProcesso, "Saúde", StringComparison.OrdinalIgnoreCase);

        public Action? FecharTela { get; set; }

        public CadastroProcessoViewModel(
            ProcessService processService,
            ItemSaudeService itemSaudeService,
            VerificacaoService verificacaoService,
            HistoricoService historicoService)
        {
            _processService = processService;
            _itemSaudeService = itemSaudeService;
            _verificacaoService = verificacaoService;
            _historicoService = historicoService;

            NovoProcesso.StatusFase = "Conhecimento";
            NovoProcesso.UltimaAtualizacao = DateTime.Now.ToString("dd/MM/yyyy");
            NovoProcesso.TipoProcesso = SelectedTipoProcesso;
            NovoProcesso.SemRepresentante = false;

            Reus.Add(new ReuCadastroViewModel(SugestoesReus) { IsFixo = true });
            CarregarSugestoesCadastroGeral();
            CarregarSugestoesSaude();
        }

        partial void OnTemRepresentanteChanged(bool value)
        {
            NovoProcesso.SemRepresentante = !value;

            if (!value)
                NovoProcesso.Representante = "Não possui";
            else if (NovoProcesso.Representante == "Não possui")
                NovoProcesso.Representante = string.Empty;

            OnPropertyChanged(nameof(NovoProcesso));
        }

        partial void OnSelectedTipoProcessoChanged(string value)
        {
            NovoProcesso.TipoProcesso = value;
            OnPropertyChanged(nameof(IsProcessoSaude));
        }

        private void CarregarSugestoesCadastroGeral()
        {
            PopularSugestoes(SugestoesNumeroProcesso, _processService.ListarValoresDistintosCadastro("numero"));
            PopularSugestoes(SugestoesPacientes, _processService.ListarValoresDistintosCadastro("paciente"));
            PopularSugestoes(SugestoesRepresentantes, _processService.ListarValoresDistintosCadastro("representante"));
            PopularSugestoes(SugestoesJuizes, _processService.ListarValoresDistintosCadastro("juiz"));
            PopularSugestoes(SugestoesReus, _processService.ListarReusDistintosCadastro());
        }

        [RelayCommand]
        private void AdicionarReu()
        {
            Reus.Add(new ReuCadastroViewModel(SugestoesReus));
        }

        [RelayCommand]
        private void RemoverReu(ReuCadastroViewModel? reu)
        {
            if (reu == null)
                return;

            if (reu.IsFixo)
            {
                reu.IsAtivo = false;
                reu.Nome = string.Empty;
                return;
            }

            Reus.Remove(reu);
        }

        [RelayCommand]
        private void AdicionarItemSaude()
        {
            ItensSaudeCadastro.Add(new SaudeItemCadastroViewModel(ObterSugestoesPorTipo, RegistrarSugestaoSaudeDigitada)
            {
                Tipo = "Medicamento",
                Quantidade = "1"
            });
        }

        [RelayCommand]
        private void RemoverSaudeItem(SaudeItemCadastroViewModel? item)
        {
            if (item == null)
                return;

            ItensSaudeCadastro.Remove(item);
        }

        [RelayCommand]
        private async Task SalvarAsync()
        {
            if (IsSaving)
                return;

            var erro = ValidarFormulario();
            if (erro != null)
            {
                StatusMensagem = erro;
                System.Windows.MessageBox.Show(erro, "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsSaving = true;
                StatusMensagem = "Salvando processo...";

                NovoProcesso.Representante = TemRepresentante ? NovoProcesso.Representante.Trim() : "Não possui";
                NovoProcesso.SemRepresentante = !TemRepresentante;
                NovoProcesso.Classificacao = NovoProcesso.TipoProcesso;
                NovoProcesso.UltimaAtualizacao = DateTime.Now.ToString("dd/MM/yyyy");

                NovoProcesso.Numero = NovoProcesso.Numero.Trim();
                NovoProcesso.Paciente = NovoProcesso.Paciente.Trim();
                NovoProcesso.Juiz = NovoProcesso.Juiz.Trim();
                _processService.CriarProcesso(NovoProcesso);
                _processService.SubstituirReus(NovoProcesso.Id, Reus.Where(r => r.IsAtivo && !string.IsNullOrWhiteSpace(r.Nome)).Select(r => r.Nome.Trim()).ToList());

                if (IsProcessoSaude)
                {
                    var itens = ObterItensSaudeParaPersistencia();
                    _itemSaudeService.SubstituirItensProcesso(NovoProcesso.Id, itens);

                    foreach (var item in itens)
                        _itemSaudeService.RegistrarCatalogo(item.Tipo, item.Nome);
                }

                var verificacaoInicial = new Verificacao
                {
                    ProcessoId = NovoProcesso.Id,
                    DataHora = DateTime.Now.ToString("dd/MM/yyyy"),
                    StatusProcesso = string.IsNullOrWhiteSpace(NovoProcesso.StatusFase) ? "Conhecimento" : NovoProcesso.StatusFase,
                    Responsavel = App.Session.UsuarioAtual?.Nome ?? "Sistema",
                    DiligenciaPendente = true,
                    PendenciaDescricao = "Registro inicial do processo.",
                    ProximoPrazo = DateTime.Today.AddDays(90).ToString("dd/MM/yyyy"),
                    DiligenciaDescricao = string.Empty
                };

                _verificacaoService.Inserir(verificacaoInicial);
                _historicoService.Registrar(NovoProcesso.Id, "Processo criado", "Registro inicial do processo.");

                await Task.Delay(350);

                StatusMensagem = "Processo criado com sucesso.";
                System.Windows.MessageBox.Show("Processo criado.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                FecharTela?.Invoke();
            }
            finally
            {
                IsSaving = false;
            }
        }

        [RelayCommand]
        private void Cancelar()
        {
            FecharTela?.Invoke();
        }

        private void CarregarSugestoesSaude()
        {
            PopularSugestoes(SugestoesMedicamentos, _itemSaudeService.ListarCatalogoPorTipo("Medicamento"));
            PopularSugestoes(SugestoesTerapias, _itemSaudeService.ListarCatalogoPorTipo("Terapia"));
            PopularSugestoes(SugestoesCirurgias, _itemSaudeService.ListarCatalogoPorTipo("Cirurgia"));
            PopularSugestoes(SugestoesOutros, _itemSaudeService.ListarCatalogoPorTipo("Outros"));
        }

        private static void PopularSugestoes(ObservableCollection<string> destino, IEnumerable<string> valores)
        {
            destino.Clear();
            foreach (var nome in valores
                         .Where(nome => !string.IsNullOrWhiteSpace(nome))
                         .Select(nome => nome.Trim())
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .OrderBy(nome => nome))
            {
                destino.Add(nome);
            }
        }

        private void RegistrarSugestaoSaudeDigitada(string tipo, string nome)
        {
            if (string.IsNullOrWhiteSpace(tipo) || string.IsNullOrWhiteSpace(nome))
                return;

            var destino = tipo.Trim() switch
            {
                "Medicamento" => SugestoesMedicamentos,
                "Terapia" => SugestoesTerapias,
                "Cirurgia" => SugestoesCirurgias,
                "Outros" => SugestoesOutros,
                _ => null
            };

            if (destino == null)
                return;

            var texto = nome.Trim();
            if (!destino.Any(item => string.Equals(item, texto, StringComparison.OrdinalIgnoreCase)))
                destino.Add(texto);
        }

        private string? ValidarFormulario()
        {
            if (string.IsNullOrWhiteSpace(NovoProcesso.Numero) || NovoProcesso.Numero.Trim().Length < MinimoCaracteresNumeroProcesso)
                return $"Número do processo é obrigatório e deve ter no mínimo {MinimoCaracteresNumeroProcesso} caracteres.";

            if (!Regex.IsMatch(NovoProcesso.Numero.Trim(), FormatoCnjRegex))
                return "Número do processo deve seguir o padrão CNJ: 0000000-00.0000.0.00.0000.";

            if (string.IsNullOrWhiteSpace(NovoProcesso.Paciente))
                return "O nome do paciente é obrigatório.";

            if (string.IsNullOrWhiteSpace(NovoProcesso.Juiz))
                return "O nome do juiz é obrigatório.";

            if (string.IsNullOrWhiteSpace(NovoProcesso.TipoProcesso))
                return "Selecione o tipo de processo.";

            if (!IsProcessoSaude)
                return null;

            if (ValidarItensSaude(ItensSaudeCadastro) is string erroSaude)
                return erroSaude;

            return null;
        }
        private static string? ValidarItensSaude(IEnumerable<SaudeItemCadastroViewModel> itens)
        {
            var preenchidos = itens.Where(item =>
                !string.IsNullOrWhiteSpace(item.Nome) ||
                !string.IsNullOrWhiteSpace(item.Quantidade) ||
                !string.IsNullOrWhiteSpace(item.Local)).ToList();

            if (preenchidos.Count == 0)
                return null;

            foreach (var item in preenchidos)
            {
                if (string.IsNullOrWhiteSpace(item.Nome))
                    return "Todos os itens de saúde devem ter nome.";

                if (string.IsNullOrWhiteSpace(item.Quantidade))
                    return "Todos os itens de saúde devem ter quantidade prescrita.";

                if (string.IsNullOrWhiteSpace(item.Tipo))
                    return "Selecione o tipo de cada item de saúde.";

                if (item.ExigeLocal && string.IsNullOrWhiteSpace(item.Local))
                    return $"Informe o local de realização para o item '{item.Nome}'.";
            }

            return null;
        }

        private IEnumerable<string> ObterSugestoesPorTipo(string tipo)
        {
            if (string.IsNullOrWhiteSpace(tipo))
                return Enumerable.Empty<string>();

            return tipo.Trim() switch
            {
                "Medicamento" => SugestoesMedicamentos,
                "Terapia" => SugestoesTerapias,
                "Cirurgia" => SugestoesCirurgias,
                "Outros" => SugestoesOutros,
                _ => Enumerable.Empty<string>()
            };
        }

        private List<ItemSaude> ObterItensSaudeParaPersistencia()
        {
            var lista = new List<ItemSaude>();

            lista.AddRange(ConverterParaItemSaude(ItensSaudeCadastro));

            return lista;
        }

        private IEnumerable<ItemSaude> ConverterParaItemSaude(IEnumerable<SaudeItemCadastroViewModel> itens)
        {
            foreach (var item in itens)
            {
                if (string.IsNullOrWhiteSpace(item.Nome) && string.IsNullOrWhiteSpace(item.Quantidade) && string.IsNullOrWhiteSpace(item.Local))
                    continue;

                yield return new ItemSaude
                {
                    Id = Guid.NewGuid().ToString(),
                    ProcessoId = NovoProcesso.Id,
                    Tipo = item.Tipo,
                    Nome = item.Nome.Trim(),
                    Qtd = item.Quantidade.Trim(),
                    Local = item.Local?.Trim() ?? string.Empty,
                    Frequencia = item.Quantidade.Trim(),
                    DataPrescricao = DateTime.Now.ToString("dd/MM/yyyy")
                };
            }
        }
    }

    public partial class SaudeItemCadastroViewModel : ObservableObject
    {
        private readonly Func<string, IEnumerable<string>>? _sugestoesPorTipo;
        private readonly Action<string, string>? _registrarSugestao;

        public IReadOnlyList<string> TiposDisponiveis { get; } = new[]
        {
            "Medicamento",
            "Terapia",
            "Cirurgia",
            "Outros"
        };

        public ObservableCollection<string> SugestoesNome { get; } = new();

        public SaudeItemCadastroViewModel(
            Func<string, IEnumerable<string>>? sugestoesPorTipo = null,
            Action<string, string>? registrarSugestao = null)
        {
            _sugestoesPorTipo = sugestoesPorTipo;
            _registrarSugestao = registrarSugestao;
            AtualizarSugestoesNome();
        }

        [ObservableProperty]
        private string _tipo = string.Empty;

        [ObservableProperty]
        private string _nome = string.Empty;

        [ObservableProperty]
        private string _quantidade = string.Empty;

        [ObservableProperty]
        private string _local = string.Empty;

        public bool ExigeLocal => string.Equals(Tipo, "Terapia", StringComparison.OrdinalIgnoreCase)
            || string.Equals(Tipo, "Cirurgia", StringComparison.OrdinalIgnoreCase);

        partial void OnTipoChanged(string value)
        {
            OnPropertyChanged(nameof(ExigeLocal));
            AtualizarSugestoesNome();

            if (!ExigeLocal)
                Local = string.Empty;
        }

        partial void OnNomeChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            if (SugestoesNome.Any(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase)))
                return;

            var texto = value.Trim();
            SugestoesNome.Add(texto);
            _registrarSugestao?.Invoke(Tipo, texto);
        }

        private void AtualizarSugestoesNome()
        {
            SugestoesNome.Clear();

            if (_sugestoesPorTipo == null)
                return;

            foreach (var item in _sugestoesPorTipo(Tipo)
                         .Where(nome => !string.IsNullOrWhiteSpace(nome))
                         .Select(nome => nome.Trim())
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .OrderBy(nome => nome))
            {
                SugestoesNome.Add(item);
            }

            if (!string.IsNullOrWhiteSpace(Nome)
                && !SugestoesNome.Any(item => string.Equals(item, Nome, StringComparison.OrdinalIgnoreCase)))
            {
                SugestoesNome.Add(Nome.Trim());
            }
        }
    }

    public partial class ReuCadastroViewModel : ObservableObject
    {
        public ObservableCollection<string> SugestoesNome { get; }

        public ReuCadastroViewModel()
        {
            SugestoesNome = new ObservableCollection<string>();
        }

        public ReuCadastroViewModel(ObservableCollection<string> sugestoes)
        {
            SugestoesNome = sugestoes;
        }

        public ReuCadastroViewModel(IEnumerable<string> sugestoes)
        {
            SugestoesNome = new ObservableCollection<string>(
                (sugestoes ?? Enumerable.Empty<string>())
                .Where(nome => !string.IsNullOrWhiteSpace(nome))
                .Select(nome => nome.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(nome => nome));
        }

        [ObservableProperty]
        private string _nome = string.Empty;

        [ObservableProperty]
        private bool _isFixo;

        [ObservableProperty]
        private bool _isAtivo = true;
    }
}
