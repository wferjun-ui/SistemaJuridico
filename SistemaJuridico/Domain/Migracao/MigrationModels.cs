namespace SistemaJuridico.Models
{
    public class MigrationRoot
    {
        public List<UsuarioMigration>? usuarios { get; set; }
        public List<ProcessoMigration>? processos { get; set; }
        public List<ContaMigration>? contas { get; set; }
        public List<VerificacaoMigration>? verificacoes { get; set; }
    }

    public class UsuarioMigration
    {
        public string id { get; set; }
        public string email { get; set; }
        public string username { get; set; }
        public int is_admin { get; set; }
    }

    public class ProcessoMigration
    {
        public string id { get; set; }
        public string numero { get; set; }
        public string paciente { get; set; }
        public string juiz { get; set; }
        public string classificacao { get; set; }
        public string status_fase { get; set; }
        public string ultima_atualizacao { get; set; }
        public string observacao_fixa { get; set; }
    }

    public class ContaMigration
    {
        public string id { get; set; }
        public string processo_id { get; set; }
        public string data_movimentacao { get; set; }
        public string tipo_lancamento { get; set; }
        public string historico { get; set; }
        public string mov_processo { get; set; }
        public string num_nf_alvara { get; set; }
        public decimal valor_alvara { get; set; }
        public decimal valor_conta { get; set; }
        public string observacoes { get; set; }
        public string responsavel { get; set; }
        public string status_conta { get; set; }
    }

    public class VerificacaoMigration
    {
        public string id { get; set; }
        public string processo_id { get; set; }
        public string data_hora { get; set; }
        public string status_processo { get; set; }
        public string responsavel { get; set; }
        public int diligencia_pendente { get; set; }
        public string pendencias_descricao { get; set; }
    }
}
