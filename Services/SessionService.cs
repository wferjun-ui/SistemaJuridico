using SistemaJuridico.Models;

namespace SistemaJuridico.Services
{
    public class SessionService
    {
        public Usuario? UsuarioAtual { get; private set; }

        public void SetUsuario(Usuario u)
        {
            UsuarioAtual = u;
        }

        public bool IsAdmin()
            => UsuarioAtual?.Perfil == "Admin";

        public bool IsLeitura()
            => UsuarioAtual?.Perfil == "Leitura";
    }
}

