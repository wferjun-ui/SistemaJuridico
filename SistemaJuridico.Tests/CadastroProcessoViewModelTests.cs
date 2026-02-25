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
    public void ValidarFormulario_DeveAceitarNumeroComFormatoParcialQuandoCamposObrigatoriosValidos()
    {
        var processo = NovoProcessoValido();
        processo.Numero = "1234567-89.0123";

        var erro = CadastroProcessoViewModel.ValidarFormulario(processo, isProcessoSaude: false, itensSaudeCadastro: []);

        Assert.Null(erro);
    }

    [Fact]
    public void ValidarFormulario_DeveRetornarErroQuandoNumeroNaoInformado()
    {
        var processo = NovoProcessoValido();
        processo.Numero = "";

        var erro = CadastroProcessoViewModel.ValidarFormulario(processo, isProcessoSaude: false, itensSaudeCadastro: []);

        Assert.Equal("Número do processo é obrigatório.", erro);
    }


    [Fact]
    public void FormatarNumeroProcesso_DeveLimitarA20DigitosEManterMascaraCnjSemValidarCompleto()
    {
        var resultado = CadastroProcessoViewModel.FormatarNumeroProcesso("ABC123456789012345678901234");

        Assert.Equal("1234567-89.0123.4.56.7890", resultado);
    }

    private static Processo NovoProcessoValido()
    {
        return new Processo
        {
            Numero = "1234567-89.0123.4.56.7890",
            Paciente = "Paciente Teste",
            Juiz = "Juiz Teste",
            TipoProcesso = "Saúde"
        };
    }
}
