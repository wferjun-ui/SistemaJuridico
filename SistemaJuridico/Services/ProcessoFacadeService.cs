using SistemaJuridico.Models;

namespace SistemaJuridico.Services
{
    public class ProcessoCompletoDTO
    {
        public Processo Processo { get; set; } = new();
        public List<ItemSaude> ItensSaude { get; set; } = new();
        public List<Verificacao> Verificacoes { get; set; } = new();
        public List<Conta> Contas { get; set; } = new();
    }

    public class ProcessoFacadeService
    {
        private readonly ProcessService _processService;
        private readonly ContaService _contaService;
        private readonly DiligenciaService _diligenciaService;
        private readonly HistoricoService _historicoService;
        private readonly ItemSaudeService _itemSaudeService;
        private readonly VerificacaoService _verificacaoService;
        private readonly AuditService _auditService;


        public ProcessoFacadeService(
            ProcessService processService,
            ContaService contaService,
            DiligenciaService diligenciaService,
            HistoricoService historicoService,
            ItemSaudeService itemSaudeService,
            VerificacaoService verificacaoService,
            AuditService auditService)
        {
            _processService = processService;
            _contaService = contaService;
            _diligenciaService = diligenciaService;
            _historicoService = historicoService;
            _itemSaudeService = itemSaudeService;
            _verificacaoService = verificacaoService;
            _auditService = auditService;
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
        // LOADER COMPLETO (NOVO)
        // ========================

        public ProcessoCompletoDTO CarregarProcessoCompleto(string processoId)
        {
            return new ProcessoCompletoDTO
            {
                Processo = ObterProcesso(processoId),
                ItensSaude = _itemSaudeService.ListarPorProcesso(processoId),
                Verificacoes = _verificacaoService.ListarPorProcesso(processoId),
                Contas = ObterContas(processoId)
            };
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

            _auditService.Registrar(
                "Inserir Conta",
                "Conta",
                conta.Id,
                $"Processo: {conta.ProcessoId} Valor: {conta.ValorConta}");
        }

        public void AtualizarConta(Conta conta)
        {
            _contaService.Atualizar(conta);

            _historicoService.Registrar(
                conta.ProcessoId,
                "Conta atualizada");

            _auditService.Registrar(
                "Atualizar Conta",
                "Conta",
                conta.Id,
                $"Processo: {conta.ProcessoId}");
        }

       public void ExcluirConta(string id, string processoId)
       {
            _contaService.Excluir(id);

            _historicoService.Registrar(
               processoId,
               "Conta excluída");

            _auditService.Registrar(
                "Excluir Conta",
                "Conta",
                id,
                $"Processo: {processoId}");
        }

        public void FecharConta(string id, string processoId)
{
    _contaService.FecharConta(id);

    _historicoService.Registrar(
        processoId,
        "Conta finalizada");

    _auditService.Registrar(
        "Fechar Conta",
        "Conta",
        id,
        $"Processo: {processoId}");
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

    _auditService.Registrar(
        "Inserir Diligência",
        "Diligencia",
        diligencia.Id,
        diligencia.Descricao);
}

       public void ConcluirDiligencia(string id, string processoId)
{
    _diligenciaService.Concluir(id);

    _historicoService.Registrar(
        processoId,
        "Diligência concluída");

    _auditService.Registrar(
        "Concluir Diligência",
        "Diligencia",
        id,
        $"Processo: {processoId}");
}

       public void ReabrirDiligencia(string id, string processoId)
{
    _diligenciaService.Reabrir(id);

    _historicoService.Registrar(
        processoId,
        "Diligência reaberta");

    _auditService.Registrar(
        "Reabrir Diligência",
        "Diligencia",
        id,
        $"Processo: {processoId}");
}

        public void ExcluirDiligencia(string id, string processoId)
{
    _diligenciaService.Excluir(id);

    _historicoService.Registrar(
        processoId,
        "Diligência excluída");

    _auditService.Registrar(
        "Excluir Diligência",
        "Diligencia",
        id,
        $"Processo: {processoId}");
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

    _auditService.Registrar(
        "Salvar Rascunho",
        "Processo",
        processoId,
        motivo);
}

        public void ConcluirEdicao(string processoId)
{
    _processService.MarcarConcluido(processoId);

    _historicoService.Registrar(
        processoId,
        "Edição concluída");

    _auditService.Registrar(
        "Concluir Edição",
        "Processo",
        processoId);
}

        // ========================
        // LOCK MULTIUSUÁRIO
        // ========================

       public bool TentarLock(string processoId)
{
    var sucesso = _processService.TentarLock(processoId);

    if (sucesso)
        _auditService.Registrar(
            "Lock Processo",
            "Processo",
            processoId);

    return sucesso;
}

        public void LiberarLock(string processoId)
{
    _processService.LiberarLock(processoId);

    _auditService.Registrar(
        "Unlock Processo",
        "Processo",
        processoId);
}

        public string? UsuarioEditando(string processoId)
        {
            return _processService.UsuarioEditando(processoId);
        }
    }
}
