namespace SistemaJuridico.Models
{
    public class ActiveSession
    {
        public string Id { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string LastActivityTimestamp { get; set; } = string.Empty;
        public string? LastProcessId { get; set; }
        public string? LastProcessNumero { get; set; }
        public string? LastProcessPaciente { get; set; }
    }
}
