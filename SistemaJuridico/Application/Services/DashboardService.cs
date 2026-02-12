using SistemaJuridico.Models;

namespace SistemaJuridico.Services
{
    public class DashboardService
    {
        private readonly ProcessService _processService;
        private readonly ContaService _contaService;
        private readonly DiligenciaService _diligenciaService;

        public DashboardService(DatabaseService db)
        {
            _processService = new ProcessService(db);
            _contaService = new ContaService(db);
            _diligenciaService = new DiligenciaService(db);
        }

        public List<Processo> ProcessosRascunho()
            => _processService.ListarProcessos().Where(p => p.IsRascunho).ToList();

        public List<Processo> ProcessosComPendencias()
            => _processService.ListarProcessos().Where(p => _diligenciaService.ExistePendencia(p.Id)).ToList();

        public List<(Processo processo, string usuario)> ProcessosBloqueados()
        {
            var lista = new List<(Processo, string)>();

            foreach (var p in _processService.ListarProcessos())
            {
                var usuario = _processService.UsuarioEditando(p.Id);
                if (!string.IsNullOrEmpty(usuario))
                    lista.Add((p, usuario));
            }

            return lista;
        }

        public decimal SaldoPendenteTotal()
        {
            decimal total = 0;
            foreach (var p in _processService.ListarProcessos())
                total += _processService.ObterResumo(p.Id).saldoPendente;
            return total;
        }

        public DateTime? UltimaMovimentacao()
        {
            var dataTexto = _contaService.ListarTodas()
                .OrderByDescending(c => c.DataMovimentacao)
                .FirstOrDefault()
                ?.DataMovimentacao;

            return DateTime.TryParse(dataTexto, out var data) ? data : null;
        }
    }
}
