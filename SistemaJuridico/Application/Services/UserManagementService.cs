using Dapper;
using SistemaJuridico.Infrastructure;
using SistemaJuridico.Models;

namespace SistemaJuridico.Services
{
    /// <summary>
    /// Legacy parity: user CRUD operations from codigo.gs
    /// (getAllUsers, registerNewUser, toggleUserAdminStatus, deleteUser).
    /// </summary>
    public static class UserManagementService
    {
        /// <summary>
        /// Legacy parity: getAllUsers().
        /// Returns all registered users.
        /// </summary>
        public static List<Usuario> GetAllUsers()
        {
            using var conn = ServiceLocator.Database.GetConnection();
            return conn.Query<Usuario>("SELECT id AS Id, username AS Username, email AS Email, perfil AS Perfil FROM usuarios ORDER BY username")
                .ToList();
        }

        /// <summary>
        /// Legacy parity: registerNewUser(email, name).
        /// Creates a new user with default non-admin profile.
        /// </summary>
        public static Usuario RegisterNewUser(string email, string name)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                throw new InvalidOperationException("Formato de e-mail inválido.");
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Nome do usuário é obrigatório.");

            using var conn = ServiceLocator.Database.GetConnection();

            var existe = conn.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM usuarios WHERE lower(email) = lower(@email)",
                new { email = email.Trim() });

            if (existe > 0)
                throw new InvalidOperationException($"Usuário com e-mail \"{email}\" já está cadastrado.");

            var newUser = new Usuario
            {
                Id = Guid.NewGuid().ToString(),
                Username = name.Trim(),
                Email = email.Trim(),
                Perfil = "Cadastrado"
            };

            conn.Execute(@"
                INSERT INTO usuarios (id, username, email, perfil)
                VALUES (@Id, @Username, @Email, @Perfil)",
                newUser);

            return newUser;
        }

        /// <summary>
        /// Legacy parity: toggleUserAdminStatus(userId, newIsAdminStatus).
        /// Changes a user's admin status.
        /// </summary>
        public static Usuario ToggleUserAdminStatus(string userId, bool newIsAdminStatus)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new InvalidOperationException("ID do usuário é obrigatório.");

            using var conn = ServiceLocator.Database.GetConnection();

            var user = conn.QueryFirstOrDefault<Usuario>(
                "SELECT id AS Id, username AS Username, email AS Email, perfil AS Perfil FROM usuarios WHERE id = @id",
                new { id = userId });

            if (user == null)
                throw new InvalidOperationException($"Usuário com ID {userId} não encontrado.");

            var currentUserEmail = App.Session?.UsuarioAtual?.Email;
            if (!string.IsNullOrWhiteSpace(currentUserEmail) &&
                string.Equals(user.Email, currentUserEmail, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Um administrador não pode remover o próprio status de administrador.");
            }

            var novoPerfil = newIsAdminStatus ? "Admin" : "Cadastrado";

            conn.Execute(
                "UPDATE usuarios SET perfil = @perfil WHERE id = @id",
                new { perfil = novoPerfil, id = userId });

            user.Perfil = novoPerfil;
            return user;
        }

        /// <summary>
        /// Legacy parity: deleteUser(userId).
        /// Deletes a registered user.
        /// </summary>
        public static string DeleteUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new InvalidOperationException("ID do usuário é obrigatório.");

            using var conn = ServiceLocator.Database.GetConnection();

            var user = conn.QueryFirstOrDefault<Usuario>(
                "SELECT id AS Id, username AS Username, email AS Email, perfil AS Perfil FROM usuarios WHERE id = @id",
                new { id = userId });

            if (user == null)
                throw new InvalidOperationException($"Usuário com ID {userId} não encontrado para exclusão.");

            var currentUserEmail = App.Session?.UsuarioAtual?.Email;
            if (!string.IsNullOrWhiteSpace(currentUserEmail) &&
                string.Equals(user.Email, currentUserEmail, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Um administrador não pode excluir o próprio usuário logado.");
            }

            conn.Execute("DELETE FROM usuarios WHERE id = @id", new { id = userId });
            return $"Usuário com ID {userId} excluído com sucesso.";
        }
    }
}
