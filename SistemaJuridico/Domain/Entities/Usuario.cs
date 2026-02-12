namespace SistemaJuridico.Models
{
    public class Usuario
    {
        public string Id { get; set; } = "";
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string Perfil { get; set; } = "";
        public int IsAdmin => Perfil == "Admin" ? 1 : 0;
        public string Nome => Username;
    }
}
