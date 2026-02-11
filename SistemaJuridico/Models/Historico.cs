namespace SistemaJuridico.Models
{
    public class Historico
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string ProcessoId { get; set; } = "";

        public string Acao { get; set; } = "";

        public string Usuario { get; set; } = "";

        public string DataHora { get; set; } =
            DateTime.Now.ToString("dd/MM/yyyy HH:mm");

        public string? Detalhes { get; set; }
    }
}
