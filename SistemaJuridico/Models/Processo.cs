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

        public string SituacaoRascunho { get; set; } = "Concluído";
        public string? MotivoRascunho { get; set; }
        public string? UsuarioRascunho { get; set; }
    }
}
