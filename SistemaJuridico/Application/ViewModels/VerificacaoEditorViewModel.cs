using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaJuridico.Models;
using SistemaJuridico.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace SistemaJuridico.ViewModels
{
    public partial class VerificacaoEditorViewModel : ObservableObject
    {
        private readonly VerificacaoFacadeService _facade;
        private readonly string _processoId;


        public ObservableCollection<ItemSaudeVerificacaoItemViewModel> ItensSaude { get; } = new();

        // ========================
        // CAMPOS EDITÁVEIS
        // ========================

        [ObservableProperty]
        private string _statusProcesso = "";

        [ObservableProperty]
        private string _descricao = "";

        [ObservableProperty]
        private string _responsavel = "";

        [ObservableProperty]
        private bool _diligenciaRealizada;

        [ObservableProperty]
        private string _descricaoDiligencia = "";

        [ObservableProperty]
        private bool _possuiPendencias;

        [ObservableProperty]
        private string _descricaoPendencias = "";

        [ObservableProperty]
        private string _prazoDiligencia = "";

        [ObservableProperty]
        private string _proximoPrazoPadrao = "";

        [ObservableProperty]
        private string _dataNotificacao = "";

        // ========================
        // CONSTRUTOR
        // ========================

        public VerificacaoEditorViewModel(
            string processoId,
            VerificacaoFacadeService facade,
            List<ItemSaude> itensSnapshot)
        {
            _processoId = processoId;
            _facade = facade;

            Responsavel = App.Session.UsuarioAtual?.Nome ?? "Sistema";
            CarregarItens(itensSnapshot);
        }

        private void CarregarItens(IEnumerable<ItemSaude> itens)
        {
            ItensSaude.Clear();

            foreach (var item in itens
                .OrderBy(i => i.IsDesnecessario)
                .ThenBy(i => i.Tipo)
                .ThenBy(i => i.Nome))
            {
                var vm = new ItemSaudeVerificacaoItemViewModel(item);
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(ItemSaudeVerificacaoItemViewModel.IsDesnecessario))
                        ReordenarItens();
                };

                ItensSaude.Add(vm);
            }
        }

        private void ReordenarItens()
        {
            var ordenados = ItensSaude
                .OrderBy(i => i.IsDesnecessario)
                .ThenBy(i => i.Tipo)
                .ThenBy(i => i.Nome)
                .ToList();

            ItensSaude.Clear();
            foreach (var item in ordenados)
                ItensSaude.Add(item);
        }

        // ========================
        // SALVAR
        // ========================

        [RelayCommand]
        private void Salvar()
        {
            if (string.IsNullOrWhiteSpace(StatusProcesso))
            {
                System.Windows.MessageBox.Show("Informe o status do processo.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Responsavel))
            {
                System.Windows.MessageBox.Show("Informe o responsável da verificação.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Descricao))
            {
                System.Windows.MessageBox.Show("Descreva o que foi realizado na verificação.");
                return;
            }

            if (DiligenciaRealizada && string.IsNullOrWhiteSpace(DescricaoDiligencia))
            {
                System.Windows.MessageBox.Show("Informe a descrição da diligência realizada.");
                return;
            }

            if (PossuiPendencias && string.IsNullOrWhiteSpace(DescricaoPendencias))
            {
                System.Windows.MessageBox.Show("Informe as pendências encontradas.");
                return;
            }

            var itens = ItensSaude.Select(i => i.ToModel()).ToList();
            var itensSemPrescricao = itens
                .Where(i => !i.IsDesnecessario)
                .Where(i => string.IsNullOrWhiteSpace(i.DataPrescricao))
                .Select(i => i.Nome)
                .ToList();

            if (itensSemPrescricao.Count > 0)
            {
                System.Windows.MessageBox.Show(
                    $"Atualize a prescrição dos itens ativos: {string.Join(", ", itensSemPrescricao)}");
                return;
            }

            _facade.CriarVerificacaoCompleta(
                processoId: _processoId,
                statusProcesso: StatusProcesso,
                responsavel: Responsavel,
                descricao: Descricao,
                diligenciaRealizada: DiligenciaRealizada,
                descricaoDiligencia: DescricaoDiligencia,
                possuiPendencias: PossuiPendencias,
                descricaoPendencias: DescricaoPendencias,
                prazoDiligencia: PrazoDiligencia,
                proximoPrazoPadrao: ProximoPrazoPadrao,
                dataNotificacao: DataNotificacao,
                itensSnapshot: itens
            );

            System.Windows.MessageBox.Show("Verificação registrada.");
            FecharSolicitado?.Invoke();
        }

        // ========================
        // EVENTO PARA FECHAR TELA
        // ========================

        public Action? FecharSolicitado { get; set; }
    }

    public partial class ItemSaudeVerificacaoItemViewModel : ObservableObject
    {
        private readonly ItemSaude _model;

        public ItemSaudeVerificacaoItemViewModel(ItemSaude model)
        {
            _model = new ItemSaude
            {
                Id = model.Id,
                ProcessoId = model.ProcessoId,
                Tipo = model.Tipo,
                Nome = model.Nome,
                Qtd = model.Qtd,
                Frequencia = model.Frequencia,
                Local = model.Local,
                DataPrescricao = model.DataPrescricao,
                IsDesnecessario = model.IsDesnecessario,
                TemBloqueio = model.TemBloqueio
            };
        }

        public string Tipo
        {
            get => _model.Tipo;
            set => SetProperty(_model.Tipo, value, _model, (m, v) => m.Tipo = v);
        }

        public string Nome
        {
            get => _model.Nome;
            set => SetProperty(_model.Nome, value, _model, (m, v) => m.Nome = v);
        }

        public string Qtd
        {
            get => _model.Qtd;
            set => SetProperty(_model.Qtd, value, _model, (m, v) => m.Qtd = v);
        }

        public string Frequencia
        {
            get => _model.Frequencia;
            set => SetProperty(_model.Frequencia, value, _model, (m, v) => m.Frequencia = v);
        }

        public string Local
        {
            get => _model.Local;
            set => SetProperty(_model.Local, value, _model, (m, v) => m.Local = v);
        }

        public string DataPrescricao
        {
            get => _model.DataPrescricao;
            set => SetProperty(_model.DataPrescricao, value, _model, (m, v) => m.DataPrescricao = v);
        }

        public bool IsDesnecessario
        {
            get => _model.IsDesnecessario;
            set
            {
                if (!SetProperty(_model.IsDesnecessario, value, _model, (m, v) => m.IsDesnecessario = v))
                    return;

                if (value)
                    DataPrescricao = string.Empty;

                OnPropertyChanged(nameof(EhAtivo));
            }
        }

        public bool EhAtivo => !IsDesnecessario;

        public ItemSaude ToModel() => new()
        {
            Id = _model.Id,
            ProcessoId = _model.ProcessoId,
            Tipo = _model.Tipo,
            Nome = _model.Nome,
            Qtd = _model.Qtd,
            Frequencia = _model.Frequencia,
            Local = _model.Local,
            DataPrescricao = _model.DataPrescricao,
            IsDesnecessario = _model.IsDesnecessario,
            TemBloqueio = _model.TemBloqueio
        };
    }
}
