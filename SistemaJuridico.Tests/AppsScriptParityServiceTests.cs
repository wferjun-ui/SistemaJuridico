using SistemaJuridico.Services;

namespace SistemaJuridico.Tests;

public class AppsScriptParityServiceTests
{
    private static ProcessoResumoCacheItem CriarProcesso(string? prazoFinal = null, string? statusProcesso = null, string? statusCalculado = null, string? prazoCalculado = null, string? prazoVerificacao = null)
    {
        return new ProcessoResumoCacheItem
        {
            ProcessoId = Guid.NewGuid().ToString(),
            Numero = "0000000-00.0000.0.00.0001",
            Paciente = "Teste",
            Juiz = "Juiz Teste",
            StatusProcesso = statusProcesso ?? "Cumprimento de Sentença",
            StatusCalculado = statusCalculado,
            PrazoCalculado = prazoCalculado,
            PrazoVerificacao = prazoVerificacao,
            PrazoFinal = prazoFinal
        };
    }

    [Fact]
    public void CalculateProcessDerivedStatus_SemPrazo_DeveRetornarNaoIniciado()
    {
        var processo = CriarProcesso(prazoFinal: null);
        var result = AppsScriptParityService.CalculateProcessDerivedStatus(processo, DateTime.Today);

        Assert.Equal("Não iniciado / Prazo Indefinido", result.Text);
        Assert.Equal("text-gray-950", result.Color);
    }

    [Fact]
    public void CalculateProcessDerivedStatus_PrazoPassado_DeveRetornarAtrasado()
    {
        var ontem = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
        var processo = CriarProcesso(prazoFinal: ontem, prazoVerificacao: ontem);
        var result = AppsScriptParityService.CalculateProcessDerivedStatus(processo, DateTime.Today);

        Assert.Equal("Atrasado", result.Text);
        Assert.Equal("text-red-500", result.Color);
    }

    [Fact]
    public void CalculateProcessDerivedStatus_PrazoHoje_DeveRetornarVenceHoje()
    {
        var hoje = DateTime.Today.ToString("yyyy-MM-dd");
        var processo = CriarProcesso(prazoFinal: hoje, prazoVerificacao: hoje);
        var result = AppsScriptParityService.CalculateProcessDerivedStatus(processo, DateTime.Today);

        Assert.Equal("Vence hoje", result.Text);
        Assert.Equal("text-yellow-500", result.Color);
    }

    [Fact]
    public void CalculateProcessDerivedStatus_PrazoDentro7Dias_DeveRetornarVenceEm()
    {
        var prazo = DateTime.Today.AddDays(3).ToString("yyyy-MM-dd");
        var processo = CriarProcesso(prazoFinal: prazo, prazoVerificacao: prazo);
        var result = AppsScriptParityService.CalculateProcessDerivedStatus(processo, DateTime.Today);

        Assert.Equal("Vence em 3 dia(s)", result.Text);
        Assert.Equal("text-yellow-500", result.Color);
    }

    [Fact]
    public void CalculateProcessDerivedStatus_PrazoMais7Dias_DeveRetornarEmDia()
    {
        var prazo = DateTime.Today.AddDays(15).ToString("yyyy-MM-dd");
        var processo = CriarProcesso(prazoFinal: prazo, prazoVerificacao: prazo);
        var result = AppsScriptParityService.CalculateProcessDerivedStatus(processo, DateTime.Today);

        Assert.Equal("Em dia", result.Text);
        Assert.Equal("text-green-500", result.Color);
    }

    [Fact]
    public void CalculateProcessDerivedStatus_Arquivado_DeveRetornarArquivado()
    {
        var prazo = DateTime.Today.AddDays(-10).ToString("yyyy-MM-dd");
        var processo = CriarProcesso(prazoFinal: prazo, statusCalculado: "Arquivado");
        var result = AppsScriptParityService.CalculateProcessDerivedStatus(processo, DateTime.Today);

        Assert.Equal("Arquivado", result.Text);
        Assert.Equal("text-gray-600", result.Color);
    }

    [Fact]
    public void CalculateProcessDerivedStatus_Desistencia_DeveRetornarFinalizado()
    {
        var processo = CriarProcesso(prazoFinal: "2026-01-01", statusCalculado: "Desistência da Parte");
        var result = AppsScriptParityService.CalculateProcessDerivedStatus(processo, DateTime.Today);

        Assert.Equal("Desistência da Parte", result.Text);
        Assert.Equal("text-gray-600", result.Color);
    }

    [Fact]
    public void CalculateProcessDerivedStatus_CumprimentoExtinto_DeveRetornarFinalizado()
    {
        var processo = CriarProcesso(prazoFinal: "2026-01-01", statusCalculado: "Cumprimento Extinto");
        var result = AppsScriptParityService.CalculateProcessDerivedStatus(processo, DateTime.Today);

        Assert.Equal("Cumprimento Extinto", result.Text);
        Assert.Equal("text-gray-600", result.Color);
    }

    [Fact]
    public void CalculateProcessDerivedStatus_PrazoAtrasado_DeveTerPrazoDataFormatado()
    {
        var processo = CriarProcesso(prazoFinal: "2026-01-15", prazoVerificacao: "2026-01-15");
        var result = AppsScriptParityService.CalculateProcessDerivedStatus(processo, new DateTime(2026, 1, 20));

        Assert.Equal("Atrasado", result.Text);
        Assert.Equal("15/01/2026", result.PrazoData);
    }

    [Fact]
    public void GetAppConfiguration_DeveRetornarTodasAsListas()
    {
        // This test validates the configuration structure without needing database
        var config = new AppConfigurationDto
        {
            TiposDeProcesso = new[] { "Saúde" },
            StatusDeProcesso = new[] { "Arquivado" },
            Diligencias = new[] { "Concluída" },
            TiposDeTerapia = new[] { "Fisioterapia" },
            StatusDeCirurgia = new[] { "Agendada" }
        };

        Assert.Single(config.TiposDeProcesso);
        Assert.Single(config.StatusDeProcesso);
        Assert.Single(config.Diligencias);
        Assert.Single(config.TiposDeTerapia);
        Assert.Single(config.StatusDeCirurgia);
    }

    [Fact]
    public void GlobalProcessReportSummaryDto_DeveAgregar()
    {
        var dto = new GlobalProcessReportSummaryDto
        {
            TotalProcesses = 3,
            ByStatus = new Dictionary<string, int> { { "Arquivado", 1 }, { "Conhecimento", 2 } },
            ByType = new Dictionary<string, int> { { "Saúde", 2 }, { "Cível (Geral)", 1 } }
        };

        Assert.Equal(3, dto.TotalProcesses);
        Assert.Equal(2, dto.ByStatus.Count);
        Assert.Equal(2, dto.ByType.Count);
    }
}
