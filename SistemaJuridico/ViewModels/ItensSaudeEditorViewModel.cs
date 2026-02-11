using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaJuridico.Models;
using SistemaJuridico.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace SistemaJuridico.ViewModels
{
    public partial class ItensSaudeEditorViewModel : ObservableObject
    {
        private readonly ItemSaudeService _itemService;
        private readonly string _processoId;

        public ObservableCollection<ItemSaudeViewModel> Itens { get; }
            = new();

        [ObservableProperty]
        private ItemSaudeViewModel? _itemSelecionado;

        public List<string> TiposDisponiveis { get; } = new()
        {
            "Terapia",
            "Medicamento",
            "Cirurgia",
            "Outros"
        };

        // ======================
        // CONSTRUTOR
        // ======================

        public ItensSaudeEditorViewModel(
            string processoId,
            ItemSaudeService itemService)
        {
            _processoId = processoId;
            _itemService = itemService;

            CarregarItens();
        }

        // ======================
        // LOAD
        // ======================

        private void CarregarItens()
        {
            Itens.Clear();

            var lista = _itemService.ListarPorProcesso(_processoId);

            foreach (var item in lista)
            {
                Itens.Add(new ItemSaudeViewModel(item));
            }
        }

        // ======================
        // ADICIONAR
        // ======================

        [RelayCommand]
        private void Adicionar()
        {
            var novo = new ItemSaude
            {
                Id = Guid.NewGuid().ToString(),
                ProcessoId = _processoId,
                Tipo = "Terapia",
                Nome = "",
                Qtd = "1",
                Frequencia = "Mensal",
                Local = "Clínica",
                DataPrescricao = "",
                IsDesnecessario = 0,
                TemBloqueio = 0
            };

            var vm = new ItemSaudeViewModel(novo);

            Itens.Add(vm);
            ItemSelecionado = vm;
        }

        // ======================
        // REMOVER
        // ======================

        [RelayCommand]
        private void Remover()
        {
            if (ItemSelecionado == null)
                return;

            var r = MessageBox.Show(
                "Deseja excluir o item?",
                "Confirmação",
                MessageBoxButton.YesNo);

            if (r != MessageBoxResult.Yes)
                return;

            _itemService.Excluir(ItemSelecionado.Id);
            Itens.Remove(ItemSelecionado);
        }

        // ======================
        // SALVAR
        // ======================

        [RelayCommand]
        private void Salvar()
        {
            foreach (var vm in Itens)
            {
                var existente = _itemService.ObterPorId(vm.Id);

                if (existente == null)
                    _itemService.Inserir(vm.Model);
                else
                    _itemService.Atualizar(vm.Model);
            }

            MessageBox.Show("Itens salvos com sucesso.");
        }

        // ======================
        // DESNECESSÁRIO
        // ======================

        [RelayCommand]
        private void AlternarDesnecessario()
        {
            if (ItemSelecionado == null)
                return;

            ItemSelecionado.IsDesnecessario =
                !ItemSelecionado.IsDesnecessario;

            _itemService.MarcarDesnecessario(
                ItemSelecionado.Id,
                ItemSelecionado.IsDesnecessario);
        }

        // ======================
        // BLOQUEIO
        // ======================

        [RelayCommand]
        private void AlternarBloqueio()
        {
            if (ItemSelecionado == null)
                return;

            ItemSelecionado.TemBloqueio =
                !ItemSelecionado.TemBloqueio;

            _itemService.DefinirBloqueio(
                ItemSelecionado.Id,
                ItemSelecionado.TemBloqueio);
        }

        // ======================
        // RECARREGAR
        // ======================

        [RelayCommand]
        private void Recarregar()
        {
            CarregarItens();
        }
    }
}
