using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaJuridico.Models;
using SistemaJuridico.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace SistemaJuridico.ViewModels
{
    public partial class VerificacaoEditorViewModel : ObservableObject
    {
        private readonly VerificacaoFacadeService _facade;
        private readonly string _processoId;

        private readonly List<ItemSaude> _itensSnapshot;

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
            _itensSnapshot = itensSnapshot;

            Responsavel = App.Session.UsuarioAtual?.Nome ?? "Sistema";
        }

        // ========================
        // SALVAR
        // ========================

        [RelayCommand]
        private void Salvar()
        {
            if (string.IsNullOrWhiteSpace(StatusProcesso))
            {
                MessageBox.Show("Informe o status do processo.");
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
                itensSnapshot: _itensSnapshot
            );

            MessageBox.Show("Verificação registrada.");
            FecharSolicitado?.Invoke();
        }

        // ========================
        // EVENTO PARA FECHAR TELA
        // ========================

        public Action? FecharSolicitado { get; set; }
    }
}
