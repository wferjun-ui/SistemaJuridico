using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SistemaJuridico.Models
{
    public class ContaRegistro : INotifyPropertyChanged, IDataErrorInfo
    {
        private string _id = Guid.NewGuid().ToString();
        private string _descricao = string.Empty;
        private decimal _valor;
        private DateTime? _dataMovimento = DateTime.Today;
        private string _tipoMovimento = "Receita";
        private string _observacoes = string.Empty;
        private string _status = "Rascunho";
        private string _responsavel = string.Empty;

        public string Id { get => _id; set => SetProperty(ref _id, value); }
        public string Descricao { get => _descricao; set => SetProperty(ref _descricao, value); }
        public decimal Valor { get => _valor; set => SetProperty(ref _valor, value); }
        public DateTime? DataMovimento { get => _dataMovimento; set => SetProperty(ref _dataMovimento, value); }
        public string TipoMovimento { get => _tipoMovimento; set => SetProperty(ref _tipoMovimento, value); }
        public string Observacoes { get => _observacoes; set => SetProperty(ref _observacoes, value); }
        public string Status { get => _status; set => SetProperty(ref _status, value); }
        public string Responsavel { get => _responsavel; set => SetProperty(ref _responsavel, value); }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Error => string.Empty;

        public string this[string columnName] => columnName switch
        {
            nameof(Descricao) when string.IsNullOrWhiteSpace(Descricao) => "Descrição é obrigatória.",
            nameof(Valor) when Valor < 0 => "Valor não pode ser negativo.",
            nameof(Valor) when Valor == 0 => "Valor deve ser maior que zero.",
            nameof(DataMovimento) when DataMovimento is null => "Data do movimento é obrigatória.",
            _ => string.Empty
        };

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return;

            field = value;
            OnPropertyChanged(propertyName);
        }
    }
}
