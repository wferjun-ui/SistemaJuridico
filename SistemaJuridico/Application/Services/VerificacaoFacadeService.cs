using Newtonsoft.Json;
using SistemaJuridico.Infrastructure;
using SistemaJuridico.Models;
using System.Globalization;

namespace SistemaJuridico.Services
{
    public class VerificacaoFacadeService
    {
        private static readonly HashSet<string> StatusVerificacaoTecnicaIgnoradosUndo = new(StringComparer.OrdinalIgnoreCase)
        {
            "Lançamento Contábil de Lote",
            "Edição de Conta Individual",
            "Exclusão de Conta Individual",
            "Edição de Conta Antiga",
            "Exclusão de Conta Antiga",
            "Lote de Contas Desfeito",
            "Verificação Desfeita"
        };

        private readonly VerificacaoService _verificacaoService;
        private readonly ItemSaudeService _itemSaudeService;
        private readonly HistoricoService _historicoService;
        private readonly ProcessService _processService;

        public VerificacaoFacadeService()
        {
            _verificacaoService = ServiceLocator.VerificacaoService;
            _itemSaudeService = ServiceLocator.ItemSaudeService;
            _historicoService = ServiceLocator.HistoricoService;
            _processService = ServiceLocator.ProcessService;
        }

        public void CriarVerificacao(
            string processoId,
            string statusProcesso,
            string responsavel,
            string descricao,
            List<ItemSaude> itensAtuais)
        {
            var snapshot = JsonConvert.SerializeObject(itensAtuais);
            var (proximoPrazo, dataNotificacao) = CalcularPrazosVerificacao(DateTime.Today, null);

            var verificacao = new Verificacao
            {
                Id = Guid.NewGuid().ToString(),
                ProcessoId = processoId,
                DataHora = DateTime.Now.ToString("o"),
                StatusProcesso = statusProcesso,
                Responsavel = responsavel,
                DiligenciaDescricao = descricao,
                DiligenciaRealizada = false,
                DiligenciaPendente = false,
                ProximoPrazo = proximoPrazo,
                DataNotificacao = dataNotificacao,
                ItensSnapshotJson = snapshot
            };

            _verificacaoService.Inserir(verificacao);
            _itemSaudeService.SubstituirItensProcesso(processoId, itensAtuais);
            _processService.AtualizarStatus(processoId, statusProcesso);
            _historicoService.Registrar(processoId, "Nova verificação registrada", statusProcesso);
        }

        public void CriarVerificacaoCompleta(
            string processoId,
            string statusProcesso,
            string responsavel,
            string descricao,
            bool diligenciaRealizada,
            string descricaoDiligencia,
            bool possuiPendencias,
            string descricaoPendencias,
            string prazoDiligencia,
            string proximoPrazoPadrao,
            string dataNotificacao,
            List<ItemSaude> itensSnapshot)
        {
            var prazoNormalizado = NormalizarDataOpcional(prazoDiligencia);
            var proximoPrazoNormalizado = NormalizarDataOpcional(proximoPrazoPadrao);
            var notificacaoNormalizada = NormalizarDataOpcional(dataNotificacao);

            if (string.IsNullOrWhiteSpace(proximoPrazoNormalizado) || string.IsNullOrWhiteSpace(notificacaoNormalizada))
            {
                var calculado = CalcularPrazosVerificacao(DateTime.Today, prazoNormalizado);
                proximoPrazoNormalizado = string.IsNullOrWhiteSpace(proximoPrazoNormalizado) ? calculado.proximoPrazo : proximoPrazoNormalizado;
                notificacaoNormalizada = string.IsNullOrWhiteSpace(notificacaoNormalizada) ? calculado.dataNotificacao : notificacaoNormalizada;
            }

            var verificacao = new Verificacao
            {
                Id = Guid.NewGuid().ToString(),
                ProcessoId = processoId,
                DataHora = DateTime.Now.ToString("o"),
                StatusProcesso = statusProcesso,
                Responsavel = responsavel,
                DiligenciaRealizada = diligenciaRealizada,
                DiligenciaPendente = possuiPendencias,
                PendenciaDescricao = string.IsNullOrWhiteSpace(descricaoPendencias) ? null : descricaoPendencias.Trim(),
                PrazoDiligencia = prazoNormalizado,
                ProximoPrazo = proximoPrazoNormalizado,
                DataNotificacao = notificacaoNormalizada,
                DiligenciaDescricao = string.IsNullOrWhiteSpace(descricaoDiligencia) ? descricao?.Trim() : descricaoDiligencia.Trim(),
                AlteracoesTexto = descricao?.Trim(),
                ItensSnapshotJson = JsonConvert.SerializeObject(itensSnapshot)
            };

            _verificacaoService.Inserir(verificacao);
            _itemSaudeService.SubstituirItensProcesso(processoId, itensSnapshot);
            _processService.AtualizarStatus(processoId, statusProcesso);
            _historicoService.Registrar(processoId, "Nova verificação registrada", statusProcesso);
        }

        public void DesfazerUltimaVerificacaoGeral(string processoId)
        {
            if (string.IsNullOrWhiteSpace(processoId))
                throw new InvalidOperationException("ID do processo é obrigatório para desfazer verificação.");

            var verificacoes = _verificacaoService
                .ListarPorProcesso(processoId)
                .Where(v => !StatusVerificacaoTecnicaIgnoradosUndo.Contains(v.StatusProcesso ?? string.Empty))
                .OrderBy(v => ParseData(v.DataHora) ?? DateTime.MinValue)
                .ToList();

            if (verificacoes.Count <= 1)
                throw new InvalidOperationException("Não é possível desfazer o registro histórico inicial ou não há verificações gerais suficientes para desfazer.");

            var ultima = verificacoes[^1];
            var penultima = verificacoes[^2];

            _verificacaoService.Excluir(ultima.Id);

            _processService.AtualizarStatus(processoId, penultima.StatusProcesso);

            var snapshot = DesserializarItensSnapshot(penultima.ItensSnapshotJson);
            _itemSaudeService.SubstituirItensProcesso(processoId, snapshot);

            var detalhes = $"Última verificação removida ({ultima.DataHora}). Restaurada para {penultima.DataHora}.";
            _historicoService.Registrar(processoId, "Verificação Desfeita (Geral)", detalhes);
        }

        internal static (string proximoPrazo, string dataNotificacao) CalcularPrazosVerificacao(DateTime dataVerificacao, string? prazoDiligencia)
        {
            var dataBase = ParseData(prazoDiligencia) ?? CalcularProximaSegundaAposDuasSemanas(dataVerificacao.Date);
            var notificacao = dataBase.AddDays(-7);

            return (
                dataBase.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                notificacao.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        private static DateTime CalcularProximaSegundaAposDuasSemanas(DateTime dataVerificacao)
        {
            var duasSemanas = dataVerificacao.AddDays(14);
            var diasAteSegunda = duasSemanas.DayOfWeek == DayOfWeek.Monday
                ? 7
                : ((int)DayOfWeek.Monday - (int)duasSemanas.DayOfWeek + 7) % 7;

            return duasSemanas.AddDays(diasAteSegunda);
        }

        private static string? NormalizarDataOpcional(string? valor)
        {
            var data = ParseData(valor);
            return data?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private static DateTime? ParseData(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return null;

            var formatos = new[]
            {
                "yyyy-MM-dd",
                "dd/MM/yyyy",
                "dd/MM/yyyy HH:mm",
                "yyyy-MM-dd HH:mm:ss",
                "o"
            };

            if (DateTime.TryParseExact(valor.Trim(), formatos, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dataExata))
                return dataExata.Date;

            if (DateTime.TryParse(valor.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var data))
                return data.Date;

            return null;
        }

        private static List<ItemSaude> DesserializarItensSnapshot(string? itensSnapshotJson)
        {
            if (string.IsNullOrWhiteSpace(itensSnapshotJson))
                return new List<ItemSaude>();

            try
            {
                return JsonConvert.DeserializeObject<List<ItemSaude>>(itensSnapshotJson) ?? new List<ItemSaude>();
            }
            catch
            {
                return new List<ItemSaude>();
            }
        }
    }
}
