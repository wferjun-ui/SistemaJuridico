using SistemaJuridico.Models;
using SistemaJuridico.ViewModels;

namespace SistemaJuridico.Tests;

public class CadastroProcessoViewModelTests
{
    [Theory]
    [InlineData("1234567", "1234567")]
    [InlineData("123456789", "1234567-89")]
    [InlineData("1234567890123", "1234567-89.0123")]
    [InlineData("12345678901234567890", "1234567-89.0123.4.56.7890")]
    [InlineData("123456789012345678901234", "1234567-89.0123.4.56.7890")]
    public void FormatarNumeroProcessoParcial_DeveAplicarMascaraProgressiva(string entrada, string esperado)
    {
        var resultado = CadastroProcessoViewModel.FormatarNumeroProcessoParcial(entrada);

        Assert.Equal(esperado, resultado);
    }

    [Fact]
    public void ValidarFormulario_DeveRetornarErroQuandoDigitoVerificadorForInvalido()
    {
        var processo = NovoProcessoValido();
        processo.Numero = "1234567-00.2024.8.26.0100";

        var erro = CadastroProcessoViewModel.ValidarFormulario(processo, isProcessoSaude: false, itensSaudeCadastro: []);

        Assert.Equal("Número do processo inválido: dígito verificador CNJ não confere.", erro);
    }

    [Fact]
    public void ValidarFormulario_DeveAceitarNumeroComDigitoVerificadorValido()
    {
        var processo = NovoProcessoValido();
        processo.Numero = GerarNumeroProcessoValido("1234567", "2024", "8", "26", "0100");

        var erro = CadastroProcessoViewModel.ValidarFormulario(processo, isProcessoSaude: false, itensSaudeCadastro: []);

        Assert.Null(erro);
    }

    private static Processo NovoProcessoValido()
    {
        return new Processo
        {
            Numero = GerarNumeroProcessoValido("1234567", "2024", "8", "26", "0100"),
            Paciente = "Paciente Teste",
            Juiz = "Juiz Teste",
            TipoProcesso = "Saúde"
        };
    }

    private static string GerarNumeroProcessoValido(string sequencial, string ano, string ramo, string tribunal, string origem)
    {
        var baseNumero = $"{sequencial}{ano}{ramo}{tribunal}{origem}";
        var resto = System.Numerics.BigInteger.Parse(baseNumero) % 97;
        var digito = 98 - (int)resto;
        return $"{sequencial}-{digito:00}.{ano}.{ramo}.{tribunal}.{origem}";
    }
}
