namespace SistemaJuridico.Models
{
    public class MigracaoItemSaude
    {
        public string id { get; set; } = "";
        public string processo_id { get; set; } = "";
        public string tipo { get; set; } = "";
        public string nome { get; set; } = "";
        public string qtd { get; set; } = "";
        public string frequencia { get; set; } = "";
        public string local { get; set; } = "";
        public string data_prescricao { get; set; } = "";
        public int is_desnecessario { get; set; }
        public int tem_bloqueio { get; set; }
    }
}
