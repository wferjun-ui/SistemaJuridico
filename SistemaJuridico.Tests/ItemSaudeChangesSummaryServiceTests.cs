using SistemaJuridico.Models;
using SistemaJuridico.Services;

namespace SistemaJuridico.Tests;

public class ItemSaudeChangesSummaryServiceTests
{
    [Fact]
    public void GerarResumo_DeveInformarAdicaoRemocaoEAlteracoes()
    {
        var anteriores = new List<ItemSaude>
        {
            new() { Tipo = "Terapia", Nome = "Fonoaudiologia", Qtd = "10", Frequencia = "Semanal", Local = "Clínica A", DataPrescricao = "2025-01-01", IsDesnecessario = false },
            new() { Tipo = "Medicamento", Nome = "Med A", Qtd = "", Frequencia = "", Local = "", DataPrescricao = "", IsDesnecessario = false }
        };

        var atuais = new List<ItemSaude>
        {
            new() { Tipo = "Terapia", Nome = "Fonoaudiologia", Qtd = "12", Frequencia = "Semanal", Local = "Clínica B", DataPrescricao = "2025-01-15", IsDesnecessario = true },
            new() { Tipo = "Cirurgia", Nome = "Procedimento X", IsDesnecessario = false }
        };

        var resumo = ItemSaudeChangesSummaryService.GerarResumo(anteriores, atuais);

        Assert.Contains("Quantidade de terapia \"Fonoaudiologia\" alterada", resumo);
        Assert.Contains("Local de terapia \"Fonoaudiologia\" alterada", resumo);
        Assert.Contains("Prescrição de terapia \"Fonoaudiologia\" alterada", resumo);
        Assert.Contains("Status \"desnecessário\" de terapia \"Fonoaudiologia\" alterado para Sim", resumo);
        Assert.Contains("Medicamento \"Med A\" removido(a)", resumo);
        Assert.Contains("Cirurgia \"Procedimento X\" adicionado(a)", resumo);
    }

    [Fact]
    public void GerarResumo_SemMudancasDeveRetornarMensagemPadrao()
    {
        var itens = new List<ItemSaude>
        {
            new() { Tipo = "Terapia", Nome = "Equoterapia", Qtd = "8", Frequencia = "Semanal", Local = "Clínica", DataPrescricao = "2025-01-01", IsDesnecessario = false }
        };

        var resumo = ItemSaudeChangesSummaryService.GerarResumo(itens, itens);

        Assert.Equal("Sem alterações estruturais nos itens de saúde.", resumo);
    }
}
