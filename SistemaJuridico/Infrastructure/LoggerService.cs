using System.Text;

namespace SistemaJuridico.Infrastructure
{
    public class LoggerService
    {
        private static readonly object _lock = new();

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

        public void Log(LogLevel nivel, string mensagem, string? usuario = null)
        {
            try
            {
                var linha = MontarLinha(nivel, mensagem, usuario);

                lock (_lock)
                {
                    File.AppendAllText(ObterArquivoLog(), linha, Encoding.UTF8);
                }
            }
            catch
            {
                // Nunca deixar log quebrar o sistema
            }
        }

        private string MontarLinha(LogLevel nivel, string mensagem, string? usuario)
        {
            return $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {nivel} | {usuario ?? "Sistema"} | {mensagem}{Environment.NewLine}";
        }

        public void Info(string msg, string? usuario = null)
            => Log(LogLevel.INFO, msg, usuario);

        public void Warn(string msg, string? usuario = null)
            => Log(LogLevel.WARN, msg, usuario);

        public void Error(string msg, Exception ex, string? usuario = null)
            => Log(LogLevel.ERROR, $"{msg} | {ex}", usuario);

        public void Audit(string msg, string? usuario = null)
            => Log(LogLevel.AUDIT, msg, usuario);
    }
}
