using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaJuridico.Models;

namespace SistemaJuridico.Services
{
    public class PdfRelatorioProcessoService
    {
        public void GerarPdfLista(List<RelatorioProcessoModel> modelos, string caminho)
        {
            Document.Create(container =>
            {
                foreach (var modelo in modelos)
                {
                    container.Page(page =>
                    {
                        page.Margin(40);

                        page.Header().Column(header =>
                        {
                            header.Item().AlignCenter().Text("RELATÓRIO DO PROCESSO")
                                .FontSize(18)
                                .Bold();

                            header.Item().AlignCenter().Text($"Processo nº {modelo.Processo.Numero}");
                            header.Item().LineHorizontal(1);
                        });

                        page.Content().Column(col =>
                        {
                            DadosProcesso(col, modelo);
                            ItensSaude(col, modelo);
                            Financeiro(col, modelo);
                            Diligencias(col, modelo);
                            Verificacoes(col, modelo);
                        });

                        page.Footer().Column(footer =>
                        {
                            footer.Item().LineHorizontal(1);
                            footer.Item().Text($"Gerado por {modelo.UsuarioGerador} em {modelo.DataGeracao:dd/MM/yyyy HH:mm}");
                            footer.Item().AlignRight().Text(x =>
                            {
                                x.CurrentPageNumber();
                                x.Span(" / ");
                                x.TotalPages();
                            });
                        });
                    });
                }
            }).GeneratePdf(caminho);
        }

        public void GerarPdf(RelatorioProcessoModel modelo, string caminho)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);

                    page.Header().Column(header =>
                    {
                        header.Item().AlignCenter().Text("RELATÓRIO DO PROCESSO")
                            .FontSize(18)
                            .Bold();

                        header.Item().AlignCenter().Text($"Processo nº {modelo.Processo.Numero}");

                        header.Item().LineHorizontal(1);
                    });

                    page.Content().Column(col =>
                    {
                        DadosProcesso(col, modelo);
                        ItensSaude(col, modelo);
                        Financeiro(col, modelo);
                        Diligencias(col, modelo);
                        Verificacoes(col, modelo);
                    });

                    page.Footer().Column(footer =>
                    {
                        footer.Item().LineHorizontal(1);

                        footer.Item().Text(
                            $"Gerado por {modelo.UsuarioGerador} em {modelo.DataGeracao:dd/MM/yyyy HH:mm}");

                        footer.Item().AlignRight().Text(x =>
                        {
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                    });
                });
            })
            .GeneratePdf(caminho);
        }

        // ============================
        // SEÇÕES
        // ============================

        private void DadosProcesso(ColumnDescriptor col, RelatorioProcessoModel modelo)
        {
            col.Item().PaddingTop(10).Text("Dados do Processo").Bold();

            col.Item().Text($"Paciente: {modelo.Processo.Paciente}");
            col.Item().Text($"Juiz: {modelo.Processo.Juiz}");
            col.Item().Text($"Classificação: {modelo.Processo.Classificacao}");
            col.Item().Text($"Status: {modelo.Processo.StatusFase}");
        }

        private void ItensSaude(ColumnDescriptor col, RelatorioProcessoModel modelo)
        {
            col.Item().PaddingTop(10).Text("Itens de Saúde").Bold();

            if (!modelo.ItensSaude.Any())
            {
                col.Item().Text("Nenhum item cadastrado.");
                return;
            }

            foreach (var item in modelo.ItensSaude)
            {
                col.Item().Text($"• {item.Tipo} - {item.Nome} (Qtd: {item.Qtd})");
            }
        }

        private void Financeiro(ColumnDescriptor col, RelatorioProcessoModel modelo)
        {
            col.Item().PaddingTop(10).Text("Contas Financeiras").Bold();

            decimal total = 0;

            foreach (var conta in modelo.Contas.OrderBy(c => c.DataMovimentacao))
            {
                col.Item().Text(
                    $"{conta.DataMovimentacao:dd/MM/yyyy} - {conta.Historico} - {conta.ValorConta:C}");

                total += conta.ValorConta;
            }

            col.Item().PaddingTop(5).Text($"Total movimentado: {total:C}")
                .Bold();
        }

        private void Diligencias(ColumnDescriptor col, RelatorioProcessoModel modelo)
        {
            col.Item().PaddingTop(10).Text("Diligências").Bold();

            if (!modelo.Diligencias.Any())
            {
                col.Item().Text("Nenhuma diligência registrada.");
                return;
            }

            foreach (var d in modelo.Diligencias)
            {
                col.Item().Text($"• {d.Descricao} - {(d.Concluida ? "Concluída" : "Pendente")}");
            }
        }

        private void Verificacoes(ColumnDescriptor col, RelatorioProcessoModel modelo)
        {
            col.Item().PaddingTop(10).Text("Histórico de Verificações").Bold();

            foreach (var v in modelo.Verificacoes.OrderByDescending(v => v.DataHora))
            {
                col.Item().Text(
                    $"{v.DataHora:dd/MM/yyyy HH:mm} - {v.StatusProcesso} - {v.Responsavel}");
            }
        }
    }
}
