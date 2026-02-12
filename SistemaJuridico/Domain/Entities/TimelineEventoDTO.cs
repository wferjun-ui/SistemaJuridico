namespace SistemaJuridico.Models
{
    public class TimelineEventoDTO
    {
        public string Tipo { get; set; } = "";

        public string Titulo { get; set; } = "";

        public string? Descricao { get; set; }

        public DateTime DataHora { get; set; }

        public string? Usuario { get; set; }

        public string? ReferenciaId { get; set; }
    }
}
