namespace SistemaJuridico.Models
{
    public class RelatorioProcessoModel
    {
        public Processo Processo { get; set; } = new();

        public List<ItemSaude> ItensSaude { get; set; } = new();

        public List<Conta> Contas { get; set; } = new();

        public List<Diligencia> Diligencias { get; set; } = new();

        public List<Verificacao> Verificacoes { get; set; } = new();

        public DateTime DataGeracao { get; set; } = DateTime.Now;

        public string UsuarioGerador { get; set; } = "";
    }
}
