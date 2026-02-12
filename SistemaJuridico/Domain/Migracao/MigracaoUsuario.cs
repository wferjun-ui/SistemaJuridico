namespace SistemaJuridico.Models
{
    public class MigracaoUsuario
    {
        public string id { get; set; } = "";
        public string username { get; set; } = "";
        public string email { get; set; } = "";
        public int is_admin { get; set; }
        public string password_hash { get; set; } = "";
        public string salt { get; set; } = "";
    }
}
