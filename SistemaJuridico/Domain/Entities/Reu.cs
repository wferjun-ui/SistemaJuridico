namespace SistemaJuridico.Models
{
    public class Reu
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string ProcessoId { get; set; } = "";

        public string Nome { get; set; } = "";
    }
}
