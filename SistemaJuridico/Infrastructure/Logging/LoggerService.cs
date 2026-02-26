using System;
using System.IO;
using System.Text;

namespace SistemaJuridico.Infrastructure
{
    public class LoggerService
    {
        private static readonly object _lock = new();
        private const LogLevel NivelMinimo = LogLevel.DEBUG;
        private const int TamanhoMaximoMensagem = 4000;

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
            var sb = new StringBuilder();
            var atual = ex;
            var nivel = 0;

            while (atual != null && nivel < 5)
            {
                if (nivel > 0)
                    sb.Append(" | INNER -> ");

                sb.Append($"{atual.GetType().FullName}: {NormalizarMensagem(atual.Message)}");

                if (!string.IsNullOrWhiteSpace(atual.StackTrace))
                    sb.Append($" | STACK: {NormalizarMensagem(atual.StackTrace)}");

                atual = atual.InnerException;
                nivel++;
            }

            return sb.ToString();
        }
    }
}
