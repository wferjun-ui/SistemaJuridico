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

        // ============================
        // PROCESSOS COM RASCUNHO
        // ============================

        public List<Processo> ProcessosRascunho()
        {
            return _processService
                .ListarProcessos()
                .Where(p => p.IsRascunho)
                .ToList();
        }

        // ============================
        // PROCESSOS COM DILIGÊNCIA
        // ============================

        public List<Processo> ProcessosComPendencias()
        {
            var processos = _processService.ListarProcessos();

            return processos
                .Where(p => _diligenciaService.ExistePendencia(p.Id))
                .ToList();
        }

        // ============================
        // PROCESSOS BLOQUEADOS
        // ============================

        public List<(Processo processo, string usuario)>
            ProcessosBloqueados()
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

        // ============================
        // RESUMO FINANCEIRO
        // ============================

        public decimal SaldoPendenteTotal()
        {
            decimal total = 0;

            foreach (var p in _processService.ListarProcessos())
            {
                var resumo = _processService.ObterResumo(p.Id);
                total += resumo.saldoPendente;
            }

            return total;
        }

        public DateTime? UltimaMovimentacao()
        {
            return _contaService
                .ListarTodas()
                .OrderByDescending(c => c.DataMovimentacao)
                .FirstOrDefault()
                ?.DataMovimentacao;
        }
    }
}
