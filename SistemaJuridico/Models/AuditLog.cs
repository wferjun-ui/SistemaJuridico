namespace SistemaJuridico.Models
{
    public class AuditLog
    {
        public string Id { get; set; } = "";
        public string DataHora { get; set; } = "";
        public string Usuario { get; set; } = "";
        public string Acao { get; set; } = "";
        public string Entidade { get; set; } = "";
        public string? EntidadeId { get; set; }
        public string? Detalhes { get; set; }
    }
}
