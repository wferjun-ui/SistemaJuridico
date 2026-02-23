using SistemaJuridico.Services;

namespace SistemaJuridico.Tests;

public class VerificacaoFacadeServiceTests
{
    [Fact]
    public void CalcularPrazosVerificacao_DeveRespeitarPrazoDiligenciaInformado()
    {
        var (proximoPrazo, dataNotificacao) = VerificacaoFacadeService.CalcularPrazosVerificacao(
            new DateTime(2026, 1, 10),
            "2026-02-05");

        Assert.Equal("2026-02-05", proximoPrazo);
        Assert.Equal("2026-01-29", dataNotificacao);
    }

    [Fact]
    public void CalcularPrazosVerificacao_SemPrazoDeveCalcularSegundaAposDuasSemanas()
    {
        var (proximoPrazo, dataNotificacao) = VerificacaoFacadeService.CalcularPrazosVerificacao(
            new DateTime(2026, 1, 6),
            null);

        Assert.Equal("2026-01-26", proximoPrazo);
        Assert.Equal("2026-01-19", dataNotificacao);
    }

    [Fact]
    public void CalcularPrazosVerificacao_QuandoDuasSemanasCaiNaSegundaDeveIrParaSegundaSeguinte()
    {
        var (proximoPrazo, dataNotificacao) = VerificacaoFacadeService.CalcularPrazosVerificacao(
            new DateTime(2026, 1, 5),
            null);

        Assert.Equal("2026-01-26", proximoPrazo);
        Assert.Equal("2026-01-19", dataNotificacao);
    }
}
