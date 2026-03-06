using System;
using System.IO;
using System.Text;

namespace SistemaJuridico.Infrastructure
{
    public class LoggerService
    {
        private static readonly object _lock = new();
        private const LogLevel NivelMinimo = LogLevel.DEBUG;
        private const int TamanhoMaximoMensagem = 12000;
        private const int TamanhoMaximoStackPorExcecao = 1200;

        private readonly string _pastaLogs;

        public LoggerService()
        {
            _pastaLogs = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SistemaJuridico",
                "Logs");

            Directory.CreateDirectory(_pastaLogs);
        }

        private string ObterArquivoLog()
        {
            var nome = $"log_{DateTime.Now:yyyyMMdd}.log";
            return Path.Combine(_pastaLogs, nome);
        }

        public void Log(LogLevel nivel, string mensagem)
        {
            if (nivel < NivelMinimo)
                return;

            try
            {
                var usuario = SessaoUsuarioService.Instance.NomeUsuario;
                var mensagemNormalizada = NormalizarMensagem(mensagem);

                var linha = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {nivel} | {usuario} | {mensagemNormalizada}{Environment.NewLine}";

                lock (_lock)
                {
                    File.AppendAllText(ObterArquivoLog(), linha, Encoding.UTF8);
                }

                DebugConsoleService.Instance.Add(linha.TrimEnd());
            }
            catch
            {
                // Logger nunca pode derrubar o sistema
            }
        }

        public void Debug(string msg) => Log(LogLevel.DEBUG, msg);

        public void Info(string msg) => Log(LogLevel.INFO, msg);

        public void Warn(string msg) => Log(LogLevel.WARN, msg);

        public void Audit(string msg) => Log(LogLevel.AUDIT, msg);

        public void Error(string msg, Exception ex)
            => Log(LogLevel.ERROR, $"{msg} | {CompactarExcecao(ex)}");

        private static string NormalizarMensagem(string? mensagem)
        {
            var texto = (mensagem ?? string.Empty)
                .Replace("\r", " ")
                .Trim();

            if (texto.Length <= TamanhoMaximoMensagem)
                return texto;

            return $"{texto[..TamanhoMaximoMensagem]}...";
        }

        private static string CompactarExcecao(Exception ex)
        {
            var raiz = ObterExcecaoRaiz(ex);
            var sb = new StringBuilder();
            sb.Append($"ROOT -> {DescreverExcecao(raiz)}");

            var atual = ex;
            var nivel = 0;

            while (atual != null && nivel < 5)
            {
                sb.Append($" | CHAIN[{nivel}] -> {DescreverExcecao(atual)}");

                atual = atual.InnerException;
                nivel++;
            }

            return sb.ToString();
        }

        private static Exception ObterExcecaoRaiz(Exception ex)
        {
            var atual = ex;
            while (atual.InnerException != null)
                atual = atual.InnerException;

            return atual;
        }

        private static string DescreverExcecao(Exception ex)
        {
            var stack = NormalizarStack(ex.StackTrace);
            var origem = string.IsNullOrWhiteSpace(ex.Source) ? "(sem origem)" : ex.Source;
            var metodo = ex.TargetSite?.ToString() ?? "(método indisponível)";

            return $"{ex.GetType().FullName}: {NormalizarMensagem(ex.Message)} | SOURCE: {origem} | TARGET: {NormalizarMensagem(metodo)} | STACK: {stack}";
        }

        private static string NormalizarStack(string? stack)
        {
            if (string.IsNullOrWhiteSpace(stack))
                return "(sem stacktrace)";

            var stackNormalizada = stack
                .Replace("\r", " ")
                .Replace("\n", " > ")
                .Trim();

            if (stackNormalizada.Length <= TamanhoMaximoStackPorExcecao)
                return stackNormalizada;

            return $"{stackNormalizada[..TamanhoMaximoStackPorExcecao]}...";
        }
    }
}
