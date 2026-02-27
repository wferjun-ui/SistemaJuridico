namespace SistemaJuridico.Models
{
    public class Verificacao
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ProcessoId { get; set; } = "";
        public string DataHora { get; set; } = "";
        public string StatusProcesso { get; set; } = "";
        public string Responsavel { get; set; } = "";
        public bool DiligenciaPendente { get; set; }
        public bool DiligenciaRealizada { get; set; }
        public string? PendenciaDescricao { get; set; }
        public string? PrazoDiligencia { get; set; }
        public string? ProximoPrazo { get; set; }
        public string? DataNotificacao { get; set; }
        public string? AlteracoesTexto { get; set; }
        public string? DiligenciaDescricao { get; set; }
        public string? ItensSnapshotJson { get; set; }
        public string? DiligenciaStatus { get; set; }
        public string? ProximaVerificacao { get; set; }
        public string? DescricaoPersistente { get; set; }

        public string Status { get => StatusProcesso; set => StatusProcesso = value; }
        public string? Descricao { get => PendenciaDescricao; set => PendenciaDescricao = value; }
    }
}
