using SistemaJuridico.Services;

namespace SistemaJuridico.Tests;

public class ActiveSessionServiceTests
{
    [Fact]
    public void RecordUserActivity_DeveInserirENaoDuplicarMesmoEmail()
    {
        var pasta = CriarPastaTemporaria();
        try
        {
            var db = new DatabaseService(pasta);
            db.Initialize();
            var service = new ActiveSessionService(db);

            service.RecordUserActivity("usuario@teste.local", "Usuário 1", "p1", "0001", "Paciente A");
            service.RecordUserActivity("usuario@teste.local", "Usuário Atualizado", "p2", "0002", "Paciente B");

            var recentes = service.GetRecentUserActivity(60);
            Assert.Single(recentes);

            var atividade = recentes[0];
            Assert.Equal("Usuário Atualizado", atividade.UserName);
            Assert.Equal("p2", atividade.LastProcessId);
            Assert.Equal("0002", atividade.LastProcessNumero);
            Assert.Equal("Paciente B", atividade.LastProcessPaciente);
        }
        finally
        {
            Directory.Delete(pasta, recursive: true);
        }
    }

    [Fact]
    public void GetRecentUserActivity_DeveRespeitarJanelaDeTempo()
    {
        var pasta = CriarPastaTemporaria();
        try
        {
            var db = new DatabaseService(pasta);
            db.Initialize();
            var service = new ActiveSessionService(db);

            service.RecordUserActivity("usuario@teste.local", "Usuário 1");

            var recentes = service.GetRecentUserActivity(1);
            Assert.Single(recentes);

            var antigos = service.GetRecentUserActivity(0);
            Assert.Single(antigos);
        }
        finally
        {
            Directory.Delete(pasta, recursive: true);
        }
    }

    private static string CriarPastaTemporaria()
    {
        var pasta = Path.Combine(Path.GetTempPath(), "SistemaJuridicoTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(pasta);
        return pasta;
    }
}
