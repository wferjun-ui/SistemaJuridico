using SistemaJuridico.Models;

namespace SistemaJuridico.Services
{
    public class ProcessoFacadeService
    {
        private readonly ProcessService _processService;
        private readonly ContaService _contaService;
        private readonly DiligenciaService _diligenciaService;

        public ProcessoFacadeService(
            ProcessService processService,
            ContaService contaService,
            DiligenciaService diligenciaService)
        {
            _processService = processService;
            _contaService = contaService;
            _diligenciaService = diligenciaService;
        }

        // ========================
        // PROCESSO
        // ========================

        public Processo ObterProcesso(string id)
        {
            return _processService
                .ListarProcessos()
                .First(x => x.Id == id);
        }

        public List<Processo> ListarProcessos()
        {
            return _processService.ListarProcessos();
        }

        // ========================
        // CONTAS
        // ========================

        public List<Conta> ObterContas(string processoId)
        {
            return _contaService.ListarPorProcesso(processoId);
        }

        public void InserirConta(Conta conta)
        {
            _contaService.Inserir(conta);
        }

        public void AtualizarConta(Conta conta)
        {
            _contaService.Atualizar(conta);
        }

        public void ExcluirConta(string id)
        {
            _contaService.Excluir(id);
        }

        public void FecharConta(string id)
        {
            _contaService.FecharConta(id);
        }

        // ========================
        // DILIGÊNCIAS
        // ========================

        public List<Diligencia> ObterDiligencias(string processoId)
        {
            return _diligenciaService.ListarPorProcesso(processoId);
        }

        public void InserirDiligencia(Diligencia diligencia)
        {
            _diligenciaService.Inserir(diligencia);
        }

        public void ConcluirDiligencia(string id)
        {
            _diligenciaService.Concluir(id);
        }

        public void ReabrirDiligencia(string id)
        {
            _diligenciaService.Reabrir(id);
        }

        public void ExcluirDiligencia(string id)
        {
            _diligenciaService.Excluir(id);
        }

        public bool ExisteDiligenciaPendente(string processoId)
        {
            return _diligenciaService.ExistePendencia(processoId);
        }

        // ========================
        // RESUMO DASHBOARD
        // ========================

        public (decimal saldo, bool diligencia, string? dataUltLanc)
            ObterResumoCompleto(string processoId)
        {
            var resumoFinanceiro = _processService.ObterResumo(processoId);
            var diligencia = _diligenciaService.ExistePendencia(processoId);

            return (
                resumoFinanceiro.saldoPendente,
                diligencia,
                resumoFinanceiro.dataUltLanc
            );
        }

        // ========================
        // RASCUNHO
        // ========================

        public void SalvarRascunho(string processoId, string motivo)
        {
            _processService.MarcarRascunho(processoId, motivo);
        }

        public void ConcluirEdicao(string processoId)
        {
            _processService.MarcarConcluido(processoId);
        }

        // ========================
        // LOCK MULTIUSUÁRIO
        // ========================

        public bool TentarLock(string processoId)
        {
            return _processService.TentarLock(processoId);
        }

        public void LiberarLock(string processoId)
        {
            _processService.LiberarLock(processoId);
        }

        public string? UsuarioEditando(string processoId)
        {
            return _processService.UsuarioEditando(processoId);
        }
    }
}
