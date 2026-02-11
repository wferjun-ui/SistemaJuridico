using SistemaJuridico.Models;

namespace SistemaJuridico.Services
{
    public class ProcessoFacadeService
    {
        private readonly ProcessService _processService;
        private readonly ContaService _contaService;

        public ProcessoFacadeService(
            ProcessService processService,
            ContaService contaService)
        {
            _processService = processService;
            _contaService = contaService;
        }

        // ========================
        // CARREGAMENTO COMPLETO
        // ========================

        public Processo ObterProcesso(string id)
        {
            return _processService
                .ListarProcessos()
                .First(x => x.Id == id);
        }

        public List<Conta> ObterContas(string processoId)
        {
            return _contaService.ListarPorProcesso(processoId);
        }

        // ========================
        // RESUMO DASHBOARD
        // ========================

        public (decimal saldo, bool diligencia, string? dataUltLanc)
            ObterResumo(string processoId)
        {
            return _processService.ObterResumo(processoId);
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
        // LOCK
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

        // ========================
        // CONTAS
        // ========================

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
    }
}
