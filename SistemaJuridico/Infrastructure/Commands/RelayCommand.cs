using System;
using System.Windows.Input;

namespace SistemaJuridico.Infrastructure
{
    public class RelayCommand : ICommand
    {
        private static readonly LoggerService _logger = new();

        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;
        private readonly string _name;

        public RelayCommand(Action execute, Func<bool>? canExecute = null, string? name = null)
        {
            _execute = execute;
            _canExecute = canExecute;
            _name = string.IsNullOrWhiteSpace(name) ? "RelayCommand" : name;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter)
        {
            _logger.Log(LogLevel.DEBUG, $"Iniciando comando: {_name}");

            try
            {
                _execute();
                _logger.Log(LogLevel.DEBUG, $"Comando executado: {_name}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Falha no comando: {_name}", ex);
                throw;
            }
        }

        public event EventHandler? CanExecuteChanged;

        public void Raise() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
