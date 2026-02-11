using SistemaJuridico.Models;

namespace SistemaJuridico.Services
{
    public class ProcessoFacadeService
    {
        private readonly ProcessService _processService;
        private readonly ContaService _contaService;
        private readonly DiligenciaService _diligenciaService;
        private readonly HistoricoService _historicoService;

        public ProcessoFacadeService(
            ProcessService processService,
            ContaService contaService,
            DiligenciaService diligenciaService,
            HistoricoService historicoService)
        {
            _processService = processService;
            _contaService = contaService;
            _diligenciaService = diligenciaService;
            _historicoService = historicoService;
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

            _historicoService.Registrar(
                conta.ProcessoId,
                "Conta adicionada",
                $"Valor: {conta.ValorConta}");
        }

        public void AtualizarConta(Conta conta)
        {
            _contaService.Atualizar(conta);

            _historicoService.Registrar(
                conta.ProcessoId,
                "Conta atualizada");
        }

        public void ExcluirConta(string id, string processoId)
        {
            _contaService.Excluir(id);

            _historicoService.Registrar(
                processoId,
                "Conta excluída");
        }

        public void FecharConta(string id, string processoId)
        {
            _contaService.FecharConta(id);

            _historicoService.Registrar(
                processoId,
                "Conta finalizada");
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

            _historicoService.Registrar(
                diligencia.ProcessoId,
                "Diligência criada",
                diligencia.Descricao);
        }

        public void ConcluirDiligencia(string id, string processoId)
        {
            _diligenciaService.Concluir(id);

            _historicoService.Registrar(
                processoId,
                "Diligência concluída");
        }

        public void ReabrirDiligencia(string id, string processoId)
        {
            _diligenciaService.Reabrir(id);

            _historicoService.Registrar(
                processoId,
                "Diligência reaberta");
        }

        public void ExcluirDiligencia(string id, string processoId)
        {
            _diligenciaService.Excluir(id);

            _historicoService.Registrar(
                processoId,
                "Diligência excluída");
        }

        public bool ExisteDiligenciaPendente(string processoId)
        {
            return _diligenciaService.ExistePendencia(processoId);
        }

        // ========================
        // RESUMO
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

            _historicoService.Registrar(
                processoId,
                "Rascunho salvo",
                motivo);
        }

        public void ConcluirEdicao(string processoId)
        {
            _processService.MarcarConcluido(processoId);

            _historicoService.Registrar(
                processoId,
                "Edição concluída");
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
