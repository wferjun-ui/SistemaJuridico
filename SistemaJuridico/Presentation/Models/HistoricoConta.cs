using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SistemaJuridico.Models
{
    public class HistoricoConta : INotifyPropertyChanged
    {
        private DateTime _dataEvento = DateTime.Now;
        private string _descricaoEvento = string.Empty;
        private string _usuario = string.Empty;
        private string _detalhes = string.Empty;

        public DateTime DataEvento { get => _dataEvento; set => SetProperty(ref _dataEvento, value); }
        public string DescricaoEvento { get => _descricaoEvento; set => SetProperty(ref _descricaoEvento, value); }
        public string Usuario { get => _usuario; set => SetProperty(ref _usuario, value); }
        public string Detalhes { get => _detalhes; set => SetProperty(ref _detalhes, value); }

        public event PropertyChangedEventHandler? PropertyChanged;

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
