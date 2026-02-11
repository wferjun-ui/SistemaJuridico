namespace SistemaJuridico.Models
{
    public class MigracaoVerificacao
    {
        public string id { get; set; } = "";
        public string processo_id { get; set; } = "";
        public string data_hora { get; set; } = "";
        public string status_processo { get; set; } = "";
        public string responsavel { get; set; } = "";
        public int diligencia_pendente { get; set; }
        public string pendencias_descricao { get; set; } = "";
    }
}
