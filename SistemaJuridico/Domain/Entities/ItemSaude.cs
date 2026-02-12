namespace SistemaJuridico.Models
{
    public class ItemSaude
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string ProcessoId { get; set; } = "";

        public string Tipo { get; set; } = "";

        public string Nome { get; set; } = "";

        public string Qtd { get; set; } = "";

        public string Frequencia { get; set; } = "";

        public string Local { get; set; } = "";

        public string DataPrescricao { get; set; } = "";

        public bool IsDesnecessario { get; set; }

        public bool TemBloqueio { get; set; }
    }
}
