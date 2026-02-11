namespace SistemaJuridico.Models
{
    public class Processo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Numero { get; set; } = "";

        public bool IsAntigo { get; set; }

        public string Paciente { get; set; } = "";

        public string Juiz { get; set; } = "";

        public string Classificacao { get; set; } = "";

        public string StatusFase { get; set; } = "Conhecimento";

        public string UltimaAtualizacao { get; set; } = "";

        public string? ObservacaoFixa { get; set; }

        public string? CacheProximoPrazo { get; set; }


        // =========================
        // CONTROLE MULTIUSUÁRIO
        // =========================

        /// <summary>
        /// Situação do processo:
        /// Concluído | Em edição | Rascunho
        /// </summary>
        public string SituacaoRascunho { get; set; } = "Concluído";


        /// <summary>
        /// Motivo informado ao salvar rascunho
        /// </summary>
        public string? MotivoRascunho { get; set; }


        /// <summary>
        /// Usuário que está editando ou criou o rascunho
        /// </summary>
        public string? UsuarioRascunho { get; set; }


        // =========================
        // PROPRIEDADES AUXILIARES (UI)
        // =========================

        /// <summary>
        /// Indica se o processo está bloqueado para edição
        /// </summary>
        public bool EstaBloqueado =>
            !string.IsNullOrEmpty(UsuarioRascunho) &&
            SituacaoRascunho != "Concluído";


        /// <summary>
        /// Indica se está em rascunho
        /// </summary>
        public bool EhRascunho =>
            SituacaoRascunho == "Rascunho";


        /// <summary>
        /// Indica se está sendo editado no momento
        /// </summary>
        public bool EmEdicao =>
            SituacaoRascunho == "Em edição";
    }
}
