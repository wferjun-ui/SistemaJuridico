using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SistemaJuridico.Models
{
    public class AlvaraRegistro : INotifyPropertyChanged
    {
        private string _numeroAlvara = string.Empty;
        private DateTime? _dataExpedicao = DateTime.Today;
        private decimal _valorAutorizado;
        private decimal _valorRecebido;
        private decimal _saldoDisponivel;
        private string _observacoes = string.Empty;

        public string NumeroAlvara { get => _numeroAlvara; set => SetProperty(ref _numeroAlvara, value); }
        public DateTime? DataExpedicao { get => _dataExpedicao; set => SetProperty(ref _dataExpedicao, value); }

        public decimal ValorAutorizado
        {
            get => _valorAutorizado;
            set
            {
                if (SetProperty(ref _valorAutorizado, value))
                    RecalcularSaldo();
            }
        }

        public decimal ValorRecebido
        {
            get => _valorRecebido;
            set
            {
                if (SetProperty(ref _valorRecebido, value))
                    RecalcularSaldo();
            }
        }

        public decimal SaldoDisponivel
        {
            get => _saldoDisponivel;
            private set => SetProperty(ref _saldoDisponivel, value);
        }

        public string Observacoes { get => _observacoes; set => SetProperty(ref _observacoes, value); }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void RecalcularSaldo() => SaldoDisponivel = ValorAutorizado - ValorRecebido;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
