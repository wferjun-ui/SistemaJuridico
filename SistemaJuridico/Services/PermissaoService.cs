using SistemaJuridico.Models;

namespace SistemaJuridico.Services
{
    public class PermissaoService
    {
        private Usuario? UsuarioAtual =>
            App.Session.UsuarioAtual;

        // =========================
        // PROCESSOS
        // =========================

        public bool PodeEditarProcesso()
        {
            return PerfilEh("Admin", "Advogado");
        }

        public bool PodeGerarPdf()
        {
            return UsuarioAtual != null;
        }

        // =========================
        // FINANCEIRO
        // =========================

        public bool PodeLancarConta()
        {
            return PerfilEh("Admin", "Financeiro");
        }

        public bool PodeExcluirConta()
        {
            return PerfilEh("Admin");
        }

        // =========================
        // DILIGÊNCIAS
        // =========================

        public bool PodeCriarDiligencia()
        {
            return PerfilEh("Admin", "Advogado");
        }

        // =========================
        // MÉTODO AUXILIAR
        // =========================

        private bool PerfilEh(params string[] perfis)
        {
            if (UsuarioAtual == null)
                return false;

            return perfis.Contains(UsuarioAtual.Perfil);
        }
    }
}
