namespace SistemaJuridico.Models
{
    public class Verificacao
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string ProcessoId { get; set; } = "";

        public string DataHora { get; set; } = "";

        public string StatusProcesso { get; set; } = "";

        public string Responsavel { get; set; } = "";

        public bool DiligenciaPendente { get; set; }

        public string? PendenciaDescricao { get; set; }

        public string? ProximoPrazo { get; set; }
    }
}
