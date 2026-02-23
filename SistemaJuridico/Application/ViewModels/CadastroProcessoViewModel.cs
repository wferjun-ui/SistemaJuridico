using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaJuridico.Models;
using SistemaJuridico.Services;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;

namespace SistemaJuridico.ViewModels
{
    public partial class CadastroProcessoViewModel : ObservableObject
    {
        private static readonly Regex CnjRegex = new(@"^\d{7}-\d{2}\.\d{4}\.\d\.\d{2}\.\d{4}$", RegexOptions.Compiled);

        private readonly ProcessService _processService;
        private readonly ItemSaudeService _itemSaudeService;

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

        public CadastroProcessoViewModel(ProcessService processService, ItemSaudeService itemSaudeService)
        {
            _processService = processService;
            _itemSaudeService = itemSaudeService;

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
            ItensSaudeCadastro.Add(new SaudeItemCadastroViewModel
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
            if (!CnjRegex.IsMatch(NovoProcesso.Numero ?? string.Empty))
                return "Número do processo inválido. Use o padrão CNJ: 0000000-00.0000.0.00.0000.";

            if (string.IsNullOrWhiteSpace(NovoProcesso.Paciente))
                return "O nome do paciente é obrigatório.";

            if (TemRepresentante && string.IsNullOrWhiteSpace(NovoProcesso.Representante))
                return "Informe o nome do genitor/representante ou desative essa opção.";

            if (string.IsNullOrWhiteSpace(NovoProcesso.Juiz))
                return "O nome do juiz é obrigatório.";

            if (string.IsNullOrWhiteSpace(NovoProcesso.TipoProcesso))
                return "Selecione o tipo de processo.";

            if (Reus.Where(r => r.IsAtivo).All(r => string.IsNullOrWhiteSpace(r.Nome)))
                return "Informe pelo menos um réu.";

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
        public IReadOnlyList<string> TiposDisponiveis { get; } = new[]
        {
            "Medicamento",
            "Terapia",
            "Cirurgia",
            "Outros"
        };

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

            if (!ExigeLocal)
                Local = string.Empty;
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
