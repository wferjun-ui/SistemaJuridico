namespace SistemaJuridico.Models
{
    public class MigracaoProcesso
    {
        public string id { get; set; } = "";
        public string numero { get; set; } = "";
        public string paciente { get; set; } = "";
        public string juiz { get; set; } = "";
        public string classificacao { get; set; } = "";
        public string status_fase { get; set; } = "";
        public string ultima_atualizacao { get; set; } = "";
        public string observacao_fixa { get; set; } = "";
    }
}
