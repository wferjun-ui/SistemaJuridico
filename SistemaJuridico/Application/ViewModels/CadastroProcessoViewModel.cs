using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaJuridico.Models;
using SistemaJuridico.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace SistemaJuridico.ViewModels
{
    public partial class CadastroProcessoViewModel : ObservableObject
    {
        private const int MinimoCaracteresNumeroProcesso = 15;

        private readonly ProcessService _processService;
        private readonly ItemSaudeService _itemSaudeService;
        private readonly VerificacaoService _verificacaoService;
        private readonly HistoricoService _historicoService;

        public Processo NovoProcesso { get; set; } = new();

        public ObservableCollection<ReuCadastroViewModel> Reus { get; } = new();
        public ObservableCollection<SaudeItemCadastroViewModel> ItensSaudeCadastro { get; } = new();

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

            Reus.Add(new ReuCadastroViewModel { IsFixo = true });
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

        [RelayCommand]
        private void AdicionarReu()
        {
            Reus.Add(new ReuCadastroViewModel());
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
            ItensSaudeCadastro.Add(new SaudeItemCadastroViewModel(ObterSugestoesPorTipo)
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
            PopularSugestoes(SugestoesMedicamentos, "Medicamento");
            PopularSugestoes(SugestoesTerapias, "Terapia");
            PopularSugestoes(SugestoesCirurgias, "Cirurgia");
            PopularSugestoes(SugestoesOutros, "Outros");
        }

        private void PopularSugestoes(ObservableCollection<string> destino, string tipo)
        {
            destino.Clear();

            foreach (var nome in _itemSaudeService.ListarCatalogoPorTipo(tipo))
                destino.Add(nome);
        }

        private string? ValidarFormulario()
        {
            if (string.IsNullOrWhiteSpace(NovoProcesso.Numero) || NovoProcesso.Numero.Trim().Length < MinimoCaracteresNumeroProcesso)
                return $"Número do processo é obrigatório e deve ter no mínimo {MinimoCaracteresNumeroProcesso} caracteres.";

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

        public IReadOnlyList<string> TiposDisponiveis { get; } = new[]
        {
            "Medicamento",
            "Terapia",
            "Cirurgia",
            "Outros"
        };

        public ObservableCollection<string> SugestoesNome { get; } = new();

        public SaudeItemCadastroViewModel(Func<string, IEnumerable<string>>? sugestoesPorTipo = null)
        {
            _sugestoesPorTipo = sugestoesPorTipo;
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

            SugestoesNome.Add(value.Trim());
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
        [ObservableProperty]
        private string _nome = string.Empty;

        [ObservableProperty]
        private bool _isFixo;

        [ObservableProperty]
        private bool _isAtivo = true;
    }
}
