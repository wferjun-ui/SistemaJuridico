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
        public int diligencia_realizada { get; set; }
        public string? diligencia_descricao { get; set; }
        public string? prazo_diligencia { get; set; }
        public string? proximo_prazo_padrao { get; set; }
        public string? data_notificacao { get; set; }
        public string? alteracoes_texto { get; set; }
        public string? itens_snapshot_json { get; set; }
    }
}
