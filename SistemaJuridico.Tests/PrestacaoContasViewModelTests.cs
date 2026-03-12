using SistemaJuridico.ViewModels;

namespace SistemaJuridico.Tests;

public class PrestacaoContasViewModelTests
{
    [Fact]
    public void Construtor_ComParametros_DeveInicializarColecoesESemLancarExcecao()
    {
        var sut = new PrestacaoContasViewModel("PROC-001", "admin");

        Assert.NotNull(sut.Contas);
        Assert.NotNull(sut.Alvaras);
        Assert.NotNull(sut.Tratamentos);
        Assert.NotNull(sut.Historico);
        Assert.NotNull(sut.ContaSelecionada);
        Assert.NotNull(sut.AdicionarContaCommand);
        Assert.NotNull(sut.SalvarContaCommand);
    }
}
