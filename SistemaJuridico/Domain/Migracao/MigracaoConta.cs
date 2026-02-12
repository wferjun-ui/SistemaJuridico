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
        public string? mov_processo { get; set; }
        public string? num_nf_alvara { get; set; }
        public decimal valor_alvara { get; set; }
        public string? terapia_medicamento_nome { get; set; }
        public string? quantidade { get; set; }
        public string? mes_referencia { get; set; }
        public string? ano_referencia { get; set; }
        public string? observacoes { get; set; }
    }
}
