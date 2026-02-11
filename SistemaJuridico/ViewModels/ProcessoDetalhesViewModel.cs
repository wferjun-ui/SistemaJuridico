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
        private readonly VerificacaoFacadeService _verificacaoFacade;

        private readonly string _processoId;

        public Processo Processo { get; private set; }

        public ObservableCollection<Conta> Contas { get; } = new();
        public ObservableCollection<ItemSaude> ItensSaude { get; } = new();
        public ObservableCollection<Verificacao> Verificacoes { get; } = new();

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
            _verificacaoFacade = new VerificacaoFacadeService();

            _processoId = processoId;

            Processo = _processService
                .ListarProcessos()
                .First(x => x.Id == processoId);

            ValidarLock();

            CarregarContas();
            CarregarItensSaude();
            CarregarVerificacoes();
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

        // ========================
        // ITENS SAÚDE
        // ========================

        [RelayCommand]
        private void EditarItensSaude()
        {
            if (ModoSomenteLeitura)
                return;

            var vm = new ItensSaudeEditorViewModel(
                _processoId,
                _itemSaudeService);

            var tela = new ItensSaudeEditorWindow(vm)
            {
                Owner = Application.Current.MainWindow
            };

            tela.ShowDialog();

            CarregarItensSaude();
        }

        // ========================
        // VERIFICAÇÕES
        // ========================

        [RelayCommand]
        private void NovaVerificacao()
        {
            if (ModoSomenteLeitura)
                return;

            var vm = new VerificacaoEditorViewModel(
                _processoId,
                _verificacaoFacade,
                ItensSaude.ToList());

            var tela = new VerificacaoEditorWindow(vm)
            {
                Owner = Application.Current.MainWindow
            };

            var result = tela.ShowDialog();

            if (result == true)
            {
                CarregarVerificacoes();
                CarregarItensSaude();
            }
        }

        // ========================
        // RASCUNHO
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
        // CONTROLE DE FECHAMENTO
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
