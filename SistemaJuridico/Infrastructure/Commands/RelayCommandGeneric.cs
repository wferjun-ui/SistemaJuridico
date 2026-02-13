using System;
using System.Windows.Input;

namespace SistemaJuridico.Infrastructure
{
    public class RelayCommand<T> : ICommand
    {
        private static readonly LoggerService _logger = new();

        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;
        private readonly string _name;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null, string? name = null)
        {
            _execute = execute;
            _canExecute = canExecute;
            _name = string.IsNullOrWhiteSpace(name) ? $"RelayCommand<{typeof(T).Name}>" : name;
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute((T?)parameter);
        }

        public void Execute(object? parameter)
        {
            _logger.Log(LogLevel.DEBUG, $"Iniciando comando: {_name}");

            try
            {
                _execute((T?)parameter);
                _logger.Log(LogLevel.DEBUG, $"Comando executado: {_name}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Falha no comando: {_name}", ex);
                throw;
            }
        }

        public event EventHandler? CanExecuteChanged;

        public void Raise()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
