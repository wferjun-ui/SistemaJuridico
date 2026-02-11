namespace SistemaJuridico.Models
{
    public class Conta
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string ProcessoId { get; set; } = "";

        public string TipoLancamento { get; set; } = "";

        public string Historico { get; set; } = "";

        public string DataMovimentacao { get; set; } =
            DateTime.Now.ToString("dd/MM/yyyy");

        public string? MovProcesso { get; set; }

        public string? NumNfAlvara { get; set; }

        public decimal ValorAlvara { get; set; }

        public decimal ValorConta { get; set; }

        public string StatusConta { get; set; } = "rascunho";

        public string Responsavel { get; set; } = "";

        public string? Observacoes { get; set; }

        public bool PodeEditar => StatusConta == "rascunho";
    }
}
