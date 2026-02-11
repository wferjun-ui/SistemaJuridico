using SistemaJuridico.Models;

namespace SistemaJuridico.Services
{
    public class ProcessoCompletoDTO
    {
        public Processo Processo { get; set; } = new();
        public List<ItemSaude> ItensSaude { get; set; } = new();
        public List<Verificacao> Verificacoes { get; set; } = new();
        public List<Conta> Contas { get; set; } = new();
        public List<Diligencia> Diligencias { get; set; } = new();
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
                .FirstOrDefault(x => x.Id == id)
                ?? new Processo();
        }

        public List<Processo> ListarProcessos()
        {
            return _processService.ListarProcessos();
        }

        // ========================
        // LOADER COMPLETO
        // ========================

        public ProcessoCompletoDTO CarregarProcessoCompleto(string processoId)
        {
            return new ProcessoCompletoDTO
            {
                Processo = ObterProcesso(processoId),
                ItensSaude = _itemSaudeService.ListarPorProcesso(processoId) ?? new(),
                Verificacoes = _verificacaoService.ListarPorProcesso(processoId) ?? new(),
                Contas = ObterContas(processoId) ?? new(),
                Diligencias = ObterDiligencias(processoId) ?? new()
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
                conta.ProcessoId,
                "Conta",
                "Inserção",
                $"Valor: {conta.ValorConta}");
        }

        public void AtualizarConta(Conta conta)
        {
            _contaService.Atualizar(conta);

            _historicoService.Registrar(
                conta.ProcessoId,
                "Conta atualizada");

            _auditService.Registrar(
                conta.ProcessoId,
                "Conta",
                "Atualização",
                conta.Id);
        }

        public void ExcluirConta(string id, string processoId)
        {
            _contaService.Excluir(id);

            _historicoService.Registrar(
                processoId,
                "Conta excluída");

            _auditService.Registrar(
                processoId,
                "Conta",
                "Exclusão",
                id);
        }

        public void FecharConta(string id, string processoId)
        {
            _contaService.FecharConta(id);

            _historicoService.Registrar(
                processoId,
                "Conta finalizada");

            _auditService.Registrar(
                processoId,
                "Conta",
                "Fechamento",
                id);
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
                diligencia.ProcessoId,
                "Diligência",
                "Inserção",
                diligencia.Descricao);
        }

        public void ConcluirDiligencia(string id, string processoId)
        {
            _diligenciaService.Concluir(id);

            _historicoService.Registrar(
                processoId,
                "Diligência concluída");

            _auditService.Registrar(
                processoId,
                "Diligência",
                "Conclusão",
                id);
        }

        public void ReabrirDiligencia(string id, string processoId)
        {
            _diligenciaService.Reabrir(id);

            _historicoService.Registrar(
                processoId,
                "Diligência reaberta");

            _auditService.Registrar(
                processoId,
                "Diligência",
                "Reabertura",
                id);
        }

        public void ExcluirDiligencia(string id, string processoId)
        {
            _diligenciaService.Excluir(id);

            _historicoService.Registrar(
                processoId,
                "Diligência excluída");

            _auditService.Registrar(
                processoId,
                "Diligência",
                "Exclusão",
                id);
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
                processoId,
                "Processo",
                "Rascunho",
                motivo);
        }

        public void ConcluirEdicao(string processoId)
        {
            _processService.MarcarConcluido(processoId);

            _historicoService.Registrar(
                processoId,
                "Edição concluída");

            _auditService.Registrar(
                processoId,
                "Processo",
                "Conclusão edição",
                "");
        }

        // ========================
        // LOCK MULTIUSUÁRIO
        // ========================

        public bool TentarLock(string processoId)
        {
            var ok = _processService.TentarLock(processoId);

            if (ok)
            {
                _auditService.Registrar(
                    processoId,
                    "Processo",
                    "Lock",
                    "Processo bloqueado para edição");
            }

            return ok;
        }

        public void LiberarLock(string processoId)
        {
            _processService.LiberarLock(processoId);

            _auditService.Registrar(
                processoId,
                "Processo",
                "Unlock",
                "Processo liberado");
        }

        public string? UsuarioEditando(string processoId)
        {
            return _processService.UsuarioEditando(processoId);
        }
    }
}
