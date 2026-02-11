using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaJuridico.Models;

namespace SistemaJuridico.Services
{
    public class PdfRelatorioProcessoService
    {
        public void GerarPdf(RelatorioProcessoModel modelo, string caminho)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);

                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Processo: {modelo.Processo.Numero}")
                            .FontSize(18).Bold();

                        col.Item().Text($"Paciente: {modelo.Processo.Paciente}");

                        col.Item().Text($"Juiz: {modelo.Processo.Juiz}");

                        col.Item().Text($"Gerado em: {modelo.DataGeracao:dd/MM/yyyy}");

                        col.Item().LineHorizontal(1);

                        // ITENS SAÚDE
                        col.Item().Text("Itens Saúde").Bold();

                        foreach (var item in modelo.ItensSaude)
                        {
                            col.Item().Text($"• {item.Tipo} - {item.Nome}");
                        }

                        // CONTAS
                        col.Item().Text("Contas").Bold();

                        foreach (var conta in modelo.Contas)
                        {
                            col.Item().Text(
                                $"{conta.DataMovimentacao:dd/MM/yyyy} - {conta.ValorConta:C}");
                        }

                        // DILIGÊNCIAS
                        col.Item().Text("Diligências").Bold();

                        foreach (var d in modelo.Diligencias)
                        {
                            col.Item().Text($"{d.Descricao}");
                        }
                    });
                });
            })
            .GeneratePdf(caminho);
        }
    }
}
