using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.VisualBasic;
using SistemaJuridico.Models;
using SistemaJuridico.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;

namespace SistemaJuridico.ViewModels
{
    public enum StatusVerificacao
    {
        EmAndamento,
        Aprovado,
        Rejeitado,
        Retornado
    }

    public enum VerificacaoStepState
    {
        Pendente,
        Atual,
        Concluida,
        Rejeitada
    }

    public partial class VerificacaoStep : ObservableObject
    {
        [ObservableProperty] private VerificacaoStepState _estado;

        public int Indice { get; }
        public string Nome { get; }
        public bool TemProximaEtapa { get; }

        public VerificacaoStep(int indice, string nome, VerificacaoStepState estado, bool temProximaEtapa)
        {
            Indice = indice;
            Nome = nome;
            _estado = estado;
            TemProximaEtapa = temProximaEtapa;
        }

        public Brush CorEtapa => Estado switch
        {
            VerificacaoStepState.Atual => Brushes.DodgerBlue,
            VerificacaoStepState.Concluida => Brushes.SeaGreen,
            VerificacaoStepState.Rejeitada => Brushes.Firebrick,
            _ => Brushes.Gray
        };

        public string Simbolo => Estado switch
        {
            VerificacaoStepState.Concluida => "✔",
            VerificacaoStepState.Rejeitada => "✖",
            _ => (Indice + 1).ToString()
        };

        partial void OnEstadoChanged(VerificacaoStepState value)
        {
            OnPropertyChanged(nameof(CorEtapa));
            OnPropertyChanged(nameof(Simbolo));
        }
    }

    public class HistoricoVerificacao
    {
        public DateTime Data { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string Etapa { get; set; } = string.Empty;
        public string Acao { get; set; } = string.Empty;
        public string Observacao { get; set; } = string.Empty;
    }

    public class VerificacaoStepViewModelBase
    {
        public VerificacaoEditorViewModel Parent { get; }
        protected VerificacaoStepViewModelBase(VerificacaoEditorViewModel parent) => Parent = parent;
    }

    public sealed class VerificacaoDadosGeraisStepViewModel : VerificacaoStepViewModelBase { public VerificacaoDadosGeraisStepViewModel(VerificacaoEditorViewModel parent) : base(parent) { } }
    public sealed class VerificacaoAnaliseTecnicaStepViewModel : VerificacaoStepViewModelBase { public VerificacaoAnaliseTecnicaStepViewModel(VerificacaoEditorViewModel parent) : base(parent) { } }
    public sealed class VerificacaoFinanceiraStepViewModel : VerificacaoStepViewModelBase { public VerificacaoFinanceiraStepViewModel(VerificacaoEditorViewModel parent) : base(parent) { } }
    public sealed class VerificacaoAprovacaoFinalStepViewModel : VerificacaoStepViewModelBase { public VerificacaoAprovacaoFinalStepViewModel(VerificacaoEditorViewModel parent) : base(parent) { } }

    public partial class VerificacaoEditorViewModel : ObservableObject
    {
        private readonly VerificacaoFacadeService _facade;
        private readonly string _processoId;
        private readonly IReadOnlyList<VerificacaoStepViewModelBase> _stepViews;

        public ObservableCollection<ItemSaudeVerificacaoItemViewModel> ItensSaude { get; } = new();
        public ObservableCollection<VerificacaoStep> Steps { get; } = new();
        public ObservableCollection<HistoricoVerificacao> Historico { get; } = new();

        [ObservableProperty] private int _currentStepIndex;
        [ObservableProperty] private VerificacaoStepViewModelBase? _currentStepViewModel;
        [ObservableProperty] private StatusVerificacao _statusGeral = StatusVerificacao.EmAndamento;
        [ObservableProperty] private bool _fluxoTravado;

        [ObservableProperty] private string _statusProcesso = "";
        [ObservableProperty] private string _descricao = "";
        [ObservableProperty] private string _responsavel = "";
        [ObservableProperty] private bool _diligenciaRealizada;
        [ObservableProperty] private string _descricaoDiligencia = "";
        [ObservableProperty] private bool _possuiPendencias;
        [ObservableProperty] private string _descricaoPendencias = "";
        [ObservableProperty] private string _prazoDiligencia = "";
        [ObservableProperty] private string _proximoPrazoPadrao = "";
        [ObservableProperty] private string _dataNotificacao = "";

        public bool IsUltimaEtapa => CurrentStepIndex == Steps.Count - 1;

        public VerificacaoEditorViewModel(
            string processoId,
            VerificacaoFacadeService facade,
            List<ItemSaude> itensSnapshot)
        {
            _processoId = processoId;
            _facade = facade;

            Steps.Add(new VerificacaoStep(0, "Dados Gerais", VerificacaoStepState.Atual, temProximaEtapa: true));
            Steps.Add(new VerificacaoStep(1, "Análise Técnica", VerificacaoStepState.Pendente, temProximaEtapa: true));
            Steps.Add(new VerificacaoStep(2, "Conferência Financeira", VerificacaoStepState.Pendente, temProximaEtapa: true));
            Steps.Add(new VerificacaoStep(3, "Aprovação Final", VerificacaoStepState.Pendente, temProximaEtapa: false));

            _stepViews = new VerificacaoStepViewModelBase[]
            {
                new VerificacaoDadosGeraisStepViewModel(this),
                new VerificacaoAnaliseTecnicaStepViewModel(this),
                new VerificacaoFinanceiraStepViewModel(this),
                new VerificacaoAprovacaoFinalStepViewModel(this)
            };

            CurrentStepIndex = 0;
            CurrentStepViewModel = _stepViews[0];

            Responsavel = App.Session.UsuarioAtual?.Nome ?? "Sistema";
            CarregarItens(itensSnapshot);
            RegistrarHistorico("Entrada em etapa", Steps[0].Nome, "Início da verificação");
        }

        partial void OnCurrentStepIndexChanged(int value)
        {
            CurrentStepViewModel = _stepViews[value];
            OnPropertyChanged(nameof(IsUltimaEtapa));
            AvancarCommand.NotifyCanExecuteChanged();
            VoltarCommand.NotifyCanExecuteChanged();
            AprovarCommand.NotifyCanExecuteChanged();
            RejeitarCommand.NotifyCanExecuteChanged();
        }

        partial void OnFluxoTravadoChanged(bool value)
        {
            AvancarCommand.NotifyCanExecuteChanged();
            VoltarCommand.NotifyCanExecuteChanged();
            AprovarCommand.NotifyCanExecuteChanged();
            RejeitarCommand.NotifyCanExecuteChanged();
        }

        private void CarregarItens(IEnumerable<ItemSaude> itens)
        {
            ItensSaude.Clear();

            foreach (var item in itens.OrderBy(i => i.IsDesnecessario).ThenBy(i => i.Tipo).ThenBy(i => i.Nome))
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
            var ordenados = ItensSaude.OrderBy(i => i.IsDesnecessario).ThenBy(i => i.Tipo).ThenBy(i => i.Nome).ToList();
            ItensSaude.Clear();
            foreach (var item in ordenados) ItensSaude.Add(item);
        }

        [RelayCommand(CanExecute = nameof(PodeAvancar))]
        private void Avancar()
        {
            if (!ValidarEtapa(CurrentStepIndex)) return;
            Steps[CurrentStepIndex].Estado = VerificacaoStepState.Concluida;
            RegistrarHistorico("Conclusão de etapa", Steps[CurrentStepIndex].Nome, "Etapa concluída");

            CurrentStepIndex++;
            Steps[CurrentStepIndex].Estado = VerificacaoStepState.Atual;
            RegistrarHistorico("Entrada em etapa", Steps[CurrentStepIndex].Nome, "Avanço no fluxo");
        }

        private bool PodeAvancar() => !FluxoTravado && CurrentStepIndex < Steps.Count - 1;

        [RelayCommand(CanExecute = nameof(PodeVoltar))]
        private void Voltar()
        {
            Steps[CurrentStepIndex].Estado = VerificacaoStepState.Pendente;
            CurrentStepIndex--;
            Steps[CurrentStepIndex].Estado = VerificacaoStepState.Atual;
            RegistrarHistorico("Retorno para correção", Steps[CurrentStepIndex].Nome, "Usuário retornou etapa");
        }

        private bool PodeVoltar() => !FluxoTravado && CurrentStepIndex > 0;

        [RelayCommand(CanExecute = nameof(PodeAprovar))]
        private void Aprovar()
        {
            if (!TrySalvarVerificacao())
                return;

            StatusGeral = StatusVerificacao.Aprovado;
            RegistrarHistorico("Aprovação", Steps[CurrentStepIndex].Nome, "Verificação aprovada");
            MessageBox.Show("Verificação aprovada e registrada.");
            FecharSolicitado?.Invoke();
        }

        private bool PodeAprovar() => !FluxoTravado && IsUltimaEtapa;

        [RelayCommand(CanExecute = nameof(PodeRejeitar))]
        private void Rejeitar()
        {
            var motivo = Interaction.InputBox("Informe o motivo da rejeição:", "Rejeitar verificação", "");
            if (string.IsNullOrWhiteSpace(motivo))
            {
                MessageBox.Show("A rejeição exige justificativa obrigatória.");
                return;
            }

            Steps[CurrentStepIndex].Estado = VerificacaoStepState.Rejeitada;
            StatusGeral = StatusVerificacao.Rejeitado;
            FluxoTravado = true;
            RegistrarHistorico("Rejeição", Steps[CurrentStepIndex].Nome, motivo.Trim());
        }

        private bool PodeRejeitar() => !FluxoTravado;

        [RelayCommand]
        private void Salvar() => TrySalvarVerificacao();

        private bool TrySalvarVerificacao()
        {
            for (var indice = 0; indice < Steps.Count; indice++)
            {
                if (!ValidarEtapa(indice))
                    return false;
            }

            var itens = ItensSaude.Select(i => i.ToModel()).ToList();
            var itensSemPrescricao = itens.Where(i => !i.IsDesnecessario).Where(i => string.IsNullOrWhiteSpace(i.DataPrescricao)).Select(i => i.Nome).ToList();
            var terapiasAtivasSemQuantidade = itens.Where(i => string.Equals(i.Tipo, "Terapia", StringComparison.OrdinalIgnoreCase)).Where(i => !i.IsDesnecessario).Where(i => string.IsNullOrWhiteSpace(i.Qtd)).Select(i => i.Nome).ToList();
            if (terapiasAtivasSemQuantidade.Count > 0)
            {
                MessageBox.Show($"Informe a quantidade das terapias ativas: {string.Join(", ", terapiasAtivasSemQuantidade)}");
                return false;
            }

            var terapiasAtivasSemLocal = itens.Where(i => string.Equals(i.Tipo, "Terapia", StringComparison.OrdinalIgnoreCase)).Where(i => !i.IsDesnecessario).Where(i => string.IsNullOrWhiteSpace(i.Local)).Select(i => i.Nome).ToList();
            if (terapiasAtivasSemLocal.Count > 0)
            {
                MessageBox.Show($"Informe o local das terapias ativas: {string.Join(", ", terapiasAtivasSemLocal)}");
                return false;
            }

            if (itensSemPrescricao.Count > 0)
            {
                MessageBox.Show($"Atualize a prescrição dos itens ativos: {string.Join(", ", itensSemPrescricao)}");
                return false;
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
                diligenciaStatus: PossuiPendencias ? "Pendente" : (DiligenciaRealizada ? "Concluída" : "Não realizada"),
                descricaoPersistente: Descricao,
                itensSnapshot: itens);

            return true;
        }

        private bool ValidarEtapa(int indice)
        {
            if (indice == 0)
            {
                if (string.IsNullOrWhiteSpace(StatusProcesso))
                {
                    MessageBox.Show("Informe o status do processo.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(Responsavel))
                {
                    MessageBox.Show("Informe o responsável da verificação.");
                    return false;
                }
            }

            if (indice == 1 && string.IsNullOrWhiteSpace(Descricao))
            {
                MessageBox.Show("Descreva o que foi realizado na verificação.");
                return false;
            }

            if (indice == 2)
            {
                if (DiligenciaRealizada && string.IsNullOrWhiteSpace(DescricaoDiligencia))
                {
                    MessageBox.Show("Informe a descrição da diligência realizada.");
                    return false;
                }

                if (PossuiPendencias && string.IsNullOrWhiteSpace(DescricaoPendencias))
                {
                    MessageBox.Show("Informe as pendências encontradas.");
                    return false;
                }
            }

            return true;
        }

        private void RegistrarHistorico(string acao, string etapa, string observacao)
        {
            Historico.Insert(0, new HistoricoVerificacao
            {
                Data = DateTime.Now,
                Usuario = App.Session.UsuarioAtual?.Nome ?? "Sistema",
                Etapa = etapa,
                Acao = acao,
                Observacao = observacao
            });
        }

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
                IsSus = model.IsSus,
                IsParticular = model.IsParticular,
                TemBloqueio = model.TemBloqueio
            };
        }

        public string Tipo { get => _model.Tipo; set => SetProperty(_model.Tipo, value, _model, (m, v) => m.Tipo = v); }
        public string Nome { get => _model.Nome; set => SetProperty(_model.Nome, value, _model, (m, v) => m.Nome = v); }
        public string Qtd { get => _model.Qtd; set => SetProperty(_model.Qtd, value, _model, (m, v) => m.Qtd = v); }
        public string Frequencia { get => _model.Frequencia; set => SetProperty(_model.Frequencia, value, _model, (m, v) => m.Frequencia = v); }
        public string Local { get => _model.Local; set => SetProperty(_model.Local, value, _model, (m, v) => m.Local = v); }
        public string DataPrescricao { get => _model.DataPrescricao; set => SetProperty(_model.DataPrescricao, value, _model, (m, v) => m.DataPrescricao = v); }

        public bool IsDesnecessario
        {
            get => _model.IsDesnecessario;
            set
            {
                if (!SetProperty(_model.IsDesnecessario, value, _model, (m, v) => m.IsDesnecessario = v)) return;
                if (value) DataPrescricao = string.Empty;
                OnPropertyChanged(nameof(EhAtivo));
                OnPropertyChanged(nameof(NecessidadeTexto));
            }
        }

        public bool EhAtivo => !IsDesnecessario;
        public string NecessidadeTexto => IsDesnecessario ? "Desnecessário" : "Necessário";
        public bool IsSus { get => _model.IsSus; set => SetProperty(_model.IsSus, value, _model, (m, v) => m.IsSus = v); }
        public bool IsParticular { get => _model.IsParticular; set => SetProperty(_model.IsParticular, value, _model, (m, v) => m.IsParticular = v); }

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
            IsSus = _model.IsSus,
            IsParticular = _model.IsParticular,
            TemBloqueio = _model.TemBloqueio
        };
    }
}
