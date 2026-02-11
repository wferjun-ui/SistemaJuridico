namespace SistemaJuridico.Models
{
    public class MigracaoConta
    {
        public string id { get; set; } = "";
        public string processo_id { get; set; } = "";
        public string data_movimentacao { get; set; } = "";
        public string tipo_lancamento { get; set; } = "";
        public string historico { get; set; } = "";
        public decimal valor_conta { get; set; }
        public string responsavel { get; set; } = "";
        public string status_conta { get; set; } = "";
    }
}
