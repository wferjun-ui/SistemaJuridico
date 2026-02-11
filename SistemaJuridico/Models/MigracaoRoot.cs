namespace SistemaJuridico.Models
{
    public class MigracaoRoot
    {
        public List<MigracaoUsuario>? usuarios { get; set; }
        public List<MigracaoProcesso>? processos { get; set; }
        public List<MigracaoConta>? contas { get; set; }
        public List<MigracaoVerificacao>? verificacoes { get; set; }
        public List<MigracaoItemSaude>? itens_saude { get; set; }
    }
}
