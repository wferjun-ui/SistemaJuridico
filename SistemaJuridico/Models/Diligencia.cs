namespace SistemaJuridico.Models
{
    public class Diligencia
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string ProcessoId { get; set; } = "";

        public string Descricao { get; set; } = "";

        public string DataCriacao { get; set; } =
            DateTime.Now.ToString("dd/MM/yyyy");

        public string? DataConclusao { get; set; }

        public bool Concluida { get; set; } = false;

        public string Responsavel { get; set; } = "";
    }
}
