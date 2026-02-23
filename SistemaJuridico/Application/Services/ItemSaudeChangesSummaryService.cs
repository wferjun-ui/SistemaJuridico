using SistemaJuridico.Models;
using System.Text;

namespace SistemaJuridico.Services
{
    public static class ItemSaudeChangesSummaryService
    {
        public static string GerarResumo(IEnumerable<ItemSaude>? itensAnteriores, IEnumerable<ItemSaude>? itensAtuais)
        {
            var anteriores = (itensAnteriores ?? Enumerable.Empty<ItemSaude>()).ToList();
            var atuais = (itensAtuais ?? Enumerable.Empty<ItemSaude>()).ToList();

            var mapaAnteriores = anteriores
                .Where(i => !string.IsNullOrWhiteSpace(i.Nome))
                .GroupBy(i => GerarChave(i), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var mapaAtuais = atuais
                .Where(i => !string.IsNullOrWhiteSpace(i.Nome))
                .GroupBy(i => GerarChave(i), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var alteracoes = new List<string>();

            foreach (var atual in mapaAtuais.Values.OrderBy(i => i.Tipo).ThenBy(i => i.Nome))
            {
                var chave = GerarChave(atual);
                if (!mapaAnteriores.TryGetValue(chave, out var anterior))
                {
                    alteracoes.Add($"{RotularTipo(atual.Tipo)} \"{atual.Nome}\" adicionado(a).");
                    continue;
                }

                RegistrarMudancaCampo(alteracoes, atual, "quantidade", anterior.Qtd, atual.Qtd);
                RegistrarMudancaCampo(alteracoes, atual, "frequência", anterior.Frequencia, atual.Frequencia);
                RegistrarMudancaCampo(alteracoes, atual, "local", anterior.Local, atual.Local);
                RegistrarMudancaCampo(alteracoes, atual, "prescrição", anterior.DataPrescricao, atual.DataPrescricao);

                if (anterior.IsDesnecessario != atual.IsDesnecessario)
                {
                    alteracoes.Add($"Status \"desnecessário\" de {RotularTipo(atual.Tipo).ToLowerInvariant()} \"{atual.Nome}\" alterado para {(atual.IsDesnecessario ? "Sim" : "Não")}.");
                }
            }

            foreach (var anterior in mapaAnteriores.Values.OrderBy(i => i.Tipo).ThenBy(i => i.Nome))
            {
                var chave = GerarChave(anterior);
                if (!mapaAtuais.ContainsKey(chave))
                    alteracoes.Add($"{RotularTipo(anterior.Tipo)} \"{anterior.Nome}\" removido(a).");
            }

            if (alteracoes.Count == 0)
                return "Sem alterações estruturais nos itens de saúde.";

            var sb = new StringBuilder();
            for (var i = 0; i < alteracoes.Count; i++)
            {
                if (i > 0)
                    sb.Append(' ');

                sb.Append(alteracoes[i]);
            }

            return sb.ToString();
        }

        private static void RegistrarMudancaCampo(List<string> alteracoes, ItemSaude atual, string campo, string? valorAnterior, string? valorAtual)
        {
            var anterior = (valorAnterior ?? string.Empty).Trim();
            var novo = (valorAtual ?? string.Empty).Trim();

            if (string.Equals(anterior, novo, StringComparison.Ordinal))
                return;

            var descricaoAnterior = string.IsNullOrWhiteSpace(anterior) ? "N/A" : anterior;
            var descricaoNova = string.IsNullOrWhiteSpace(novo) ? "N/A" : novo;

            alteracoes.Add($"{PrimeiraMaiuscula(campo)} de {RotularTipo(atual.Tipo).ToLowerInvariant()} \"{atual.Nome}\" alterada de \"{descricaoAnterior}\" para \"{descricaoNova}\".");
        }

        private static string GerarChave(ItemSaude item)
            => $"{item.Tipo?.Trim().ToUpperInvariant()}|{item.Nome?.Trim().ToUpperInvariant()}";

        private static string RotularTipo(string? tipo)
            => string.IsNullOrWhiteSpace(tipo) ? "Item" : tipo.Trim();

        private static string PrimeiraMaiuscula(string texto)
            => string.IsNullOrWhiteSpace(texto)
                ? string.Empty
                : char.ToUpperInvariant(texto[0]) + texto[1..];
    }
}
