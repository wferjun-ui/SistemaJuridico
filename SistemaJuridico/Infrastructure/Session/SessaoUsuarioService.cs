using SistemaJuridico.Models;

namespace SistemaJuridico.Infrastructure
{
    public class SessaoUsuarioService
    {
        private static SessaoUsuarioService? _instance;
        private static readonly object _lock = new();

        public static SessaoUsuarioService Instance
        {
            get
            {
                lock (_lock)
                {
                    _instance ??= new SessaoUsuarioService();
                    return _instance;
                }
            }
        }

        private SessaoUsuarioService() { }

        public Usuario? UsuarioLogado { get; private set; }

        public bool EstaLogado => UsuarioLogado != null;

        public string NomeUsuario => UsuarioLogado?.Username ?? "Sistema";

        public bool IsAdmin => UsuarioLogado?.IsAdmin == 1;

        public void IniciarSessao(Usuario usuario)
        {
            UsuarioLogado = usuario;
        }

        public void EncerrarSessao()
        {
            UsuarioLogado = null;
        }
    }
}
