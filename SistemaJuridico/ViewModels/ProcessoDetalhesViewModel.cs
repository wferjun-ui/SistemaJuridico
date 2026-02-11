using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaJuridico.Models;
using SistemaJuridico.Services;
using SistemaJuridico.Views;
using System.Collections.ObjectModel;
using System.Windows;

namespace SistemaJuridico.ViewModels
{
    public partial class ProcessoDetalhesViewModel : ObservableObject
    {
        private readonly ProcessService _processService;
        private readonly ContaService _contaService;
        private readonly ItemSaudeService _itemSaudeService;
        private readonly VerificacaoService _verificacaoService;

        private readonly string _processoId;

        public Processo Processo { get; private set; }

        public ObservableCollection<Conta> Contas { get; } = new();
        public ObservableCollection<ItemSaude> ItensSaude { get; } = new();
        public ObservableCollection<Verificacao> Verificacoes { get; } = new();
        public ObservableCollection<Diligencia> Diligencias { get; } = new();

        [ObservableProperty]
        private bool _modoSomenteLeitura;

        [ObservableProperty]
        private string _usuarioEditandoTexto = "";

        public ProcessoDetalhesViewModel(
            string processoId,
            ProcessService processService)
        {
            _processService = processService;

            var db = new DatabaseService();

            _contaService = new ContaService(db);
            _itemSaudeService = new ItemSaudeService(db);
            _verificacaoService = new VerificacaoService(db);

            _processoId = processoId;

            Processo = _processService
                .ListarProcessos()
                .First(x => x.Id == processoId);

            ValidarLock();

            CarregarContas();
            CarregarItensSaude();
            CarregarVerificacoes();
            CarregarDiligencias();
        }

        // ========================
        // LOCK MULTIUSUÁRIO
        // ========================

        private void ValidarLock()
        {
            var usuario = _processService.UsuarioEditando(_processoId);
            var atual = App.Session.UsuarioAtual?.Email;

            if (usuario != null && usuario != atual)
            {
                ModoSomenteLeitura = true;
                UsuarioEditandoTexto =
                    $"Processo em edição por {usuario}";
            }
            else
            {
                _processService.TentarLock(_processoId);
            }
        }

        // ========================
        // LOADERS
        // ========================

        private void CarregarContas()
        {
            Contas.Clear();

            foreach (var c in _contaService.ListarPorProcesso(_processoId))
                Contas.Add(c);
        }

        private void CarregarItensSaude()
        {
            ItensSaude.Clear();

            foreach (var item in _itemSaudeService.ListarPorProcesso(_processoId))
                ItensSaude.Add(item);
        }

        private void CarregarVerificacoes()
        {
            Verificacoes.Clear();

            foreach (var v in _verificacaoService
                         .ListarPorProcesso(_processoId)
                         .OrderByDescending(x => x.DataHora))
            {
                Verificacoes.Add(v);
            }
        }

        private void CarregarDiligencias()
        {
            Diligencias.Clear();

            var lista = new DiligenciaService(new DatabaseService())
                .ListarPorProcesso(_processoId);

            foreach (var d in lista)
                Diligencias.Add(d);
        }

        // ========================
        // COMANDOS
        // ========================

        [RelayCommand]
        private void SalvarRascunho()
        {
            var motivo = Microsoft.VisualBasic.Interaction.InputBox(
                "Informe o motivo do rascunho:");

            if (string.IsNullOrWhiteSpace(motivo))
                return;

            _processService.MarcarRascunho(_processoId, motivo);

            MessageBox.Show("Rascunho salvo.");
        }

        [RelayCommand]
        private void ConcluirEdicao()
        {
            _processService.MarcarConcluido(_processoId);

            MessageBox.Show("Edição concluída.");
        }

        // ========================
        // VERIFICAÇÕES
        // ========================

        [RelayCommand]
        private void NovaVerificacao()
        {
            if (ModoSomenteLeitura)
                return;

            var facade = new VerificacaoFacadeService();

            var status = Microsoft.VisualBasic.Interaction.InputBox(
                "Informe o status do processo:");

            if (string.IsNullOrWhiteSpace(status))
                return;

            var descricao = Microsoft.VisualBasic.Interaction.InputBox(
                "Descrição da verificação:");

            var responsavel = App.Session.UsuarioAtual?.Nome ?? "Sistema";

            facade.CriarVerificacao(
                _processoId,
                status,
                responsavel,
                descricao,
                ItensSaude.ToList());

            MessageBox.Show("Verificação registrada.");

            CarregarVerificacoes();
            CarregarItensSaude();
        }

        // ========================
        // DILIGÊNCIAS
        // ========================

        [RelayCommand]
        private void NovaDiligencia()
        {
            if (ModoSomenteLeitura)
                return;

            var facade = CriarFacade();

            var vm = new DiligenciaEditorViewModel(
                _processoId,
                facade);

            var tela = new DiligenciaEditorWindow(vm)
            {
                Owner = Application.Current.MainWindow
            };

            tela.ShowDialog();

            CarregarDiligencias();
        }

        [RelayCommand]
        private void ConcluirDiligencia(Diligencia d)
        {
            if (d == null) return;

            var facade = CriarFacade();

            facade.ConcluirDiligencia(d.Id, _processoId);

            CarregarDiligencias();
        }

        [RelayCommand]
        private void ReabrirDiligencia(Diligencia d)
        {
            if (d == null) return;

            var facade = CriarFacade();

            facade.ReabrirDiligencia(d.Id, _processoId);

            CarregarDiligencias();
        }

        // ========================
        // FACADE
        // ========================

        private ProcessoFacadeService CriarFacade()
        {
            var db = new DatabaseService();

            return new ProcessoFacadeService(
                _processService,
                new ContaService(db),
                new DiligenciaService(db),
                new HistoricoService(db));
        }

        // ========================
        // CONTROLE FECHAMENTO
        // ========================

        public bool PodeFechar()
        {
            if (ModoSomenteLeitura)
                return true;

            var r = MessageBox.Show(
                "Deseja sair sem salvar?",
                "Confirmação",
                MessageBoxButton.YesNo);

            return r == MessageBoxResult.Yes;
        }
    }
}
