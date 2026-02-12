using SistemaJuridico.Models;
using SistemaJuridico.Services;
using SistemaJuridico.Infrastructure;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace SistemaJuridico.ViewModels
{
    public class AuditoriaViewModel : ViewModelBase
    {
        private readonly AuditService _auditService;

        public ObservableCollection<AuditLog> Logs { get; } = new();

        public DateTime? DataInicial { get; set; }
        public DateTime? DataFinal { get; set; }
        public string? UsuarioFiltro { get; set; }
        public string? ProcessoIdFiltro { get; set; }

        public RelayCommand FiltrarCommand { get; }

        public AuditoriaViewModel() : this(new AuditService(new DatabaseService()))
        {
        }

        public AuditoriaViewModel(AuditService auditService)
        {
            _auditService = auditService;

            FiltrarCommand = new RelayCommand(async () => await CarregarAsync());
            _ = CarregarAsync();
        }

        public async Task CarregarAsync()
        {
            Logs.Clear();

            var lista = await _auditService.ObterLogsAsync(
                DataInicial,
                DataFinal,
                UsuarioFiltro,
                ProcessoIdFiltro);

            foreach (var log in lista)
                Logs.Add(log);
        }
    }
}
