using System;
using System.IO;
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

        public void Log(LogLevel nivel, string mensagem)
        {
            try
            {
                var usuario = SessaoUsuarioService.Instance.NomeUsuario;

                var linha = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {nivel} | {usuario} | {mensagem}{Environment.NewLine}";

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

        public void Info(string msg) => Log(LogLevel.INFO, msg);

        public void Warn(string msg) => Log(LogLevel.WARN, msg);

        public void Audit(string msg) => Log(LogLevel.AUDIT, msg);

        public void Error(string msg, Exception ex)
            => Log(LogLevel.ERROR, $"{msg} | {ex}");
    }
}
