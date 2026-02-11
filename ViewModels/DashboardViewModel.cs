using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaJuridico.Models;
using SistemaJuridico.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace SistemaJuridico.ViewModels
{
    public partial class ProcessoResumoDto : ObservableObject
    {
        public Processo Processo { get; set; } = new();

        [ObservableProperty]
        private string _saldoPendente = "";

        [ObservableProperty]
        private string _diligencia = "";

        [ObservableProperty]
        private string _ultimaMov = "";

        [ObservableProperty]
        private string _rascunho = "";
    }

    public partial class DashboardViewModel : ObservableObject
    {
        private readonly ProcessService _service;

        public ObservableCollection<ProcessoResumoDto> Processos { get; set; } = new();

        public DashboardViewModel()
        {
            _service = new ProcessService(App.DB!);
            Carregar();
        }

        public void Carregar()
        {
            Processos.Clear();

            foreach (var p in _service.ListarProcessos())
            {
                var (saldo, dilig, data) = _service.ObterResumo(p.Id);

                Processos.Add(new ProcessoResumoDto
                {
                    Processo = p,
                    SaldoPendente = saldo > 0 ? $"R$ {saldo:N2}" : "-",
                    Diligencia = dilig ? "Pendente" : "OK",
                    UltimaMov = data ?? "-",
                    Rascunho = p.SituacaoRascunho
                });
            }
        }

        // =========================
        // ABRIR PROCESSO
        // =========================

        [RelayCommand]
        private void AbrirProcesso(ProcessoResumoDto dto)
        {
            var usuarioEditando = _service.UsuarioEditando(dto.Processo.Id);

            if (usuarioEditando != null &&
                usuarioEditando != App.Session.UsuarioAtual?.Email)
            {
                MessageBox.Show(
                    $"Processo em edição por {usuarioEditando}");
                return;
            }

            if (!_service.TentarLock(dto.Processo.Id))
            {
                MessageBox.Show("Não foi possível obter lock.");
                return;
            }

            MessageBox.Show("Aqui abrirá tela de detalhes futuramente.");
        }
    }
}
