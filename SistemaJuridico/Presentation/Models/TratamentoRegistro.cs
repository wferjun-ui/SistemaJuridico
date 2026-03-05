using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SistemaJuridico.Models
{
    public class TratamentoRegistro : INotifyPropertyChanged, IDataErrorInfo
    {
        private string _descricaoTratamento = string.Empty;
        private int _quantidade;
        private decimal _valorUnitario;
        private decimal _valorTotal;
        private DateTime? _dataInicio = DateTime.Today;
        private DateTime? _dataFim = DateTime.Today;

        public string DescricaoTratamento { get => _descricaoTratamento; set => SetProperty(ref _descricaoTratamento, value); }

        public int Quantidade
        {
            get => _quantidade;
            set
            {
                if (SetProperty(ref _quantidade, value < 0 ? 0 : value))
                    RecalcularTotal();
            }
        }

        public decimal ValorUnitario
        {
            get => _valorUnitario;
            set
            {
                if (SetProperty(ref _valorUnitario, value < 0 ? 0 : value))
                    RecalcularTotal();
            }
        }

        public decimal ValorTotal
        {
            get => _valorTotal;
            private set => SetProperty(ref _valorTotal, value);
        }

        public DateTime? DataInicio { get => _dataInicio; set => SetProperty(ref _dataInicio, value); }
        public DateTime? DataFim { get => _dataFim; set => SetProperty(ref _dataFim, value); }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Error => string.Empty;

        public string this[string columnName] => columnName switch
        {
            nameof(Quantidade) when Quantidade < 0 => "Quantidade deve ser maior ou igual a zero.",
            nameof(ValorUnitario) when ValorUnitario < 0 => "Valor unitário não pode ser negativo.",
            nameof(DataFim) when DataInicio.HasValue && DataFim.HasValue && DataFim.Value < DataInicio.Value => "Data final não pode ser menor que data inicial.",
            _ => string.Empty
        };

        private void RecalcularTotal() => ValorTotal = Quantidade * ValorUnitario;

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
