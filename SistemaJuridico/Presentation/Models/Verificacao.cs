using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SistemaJuridico.Presentation.Models
{
    public class Verificacao : INotifyPropertyChanged, IDataErrorInfo
    {
        private int _id;
        private string _tipoVerificacao = string.Empty;
        private DateTime? _dataSolicitacao;
        private DateTime? _dataResposta;
        private string _status = string.Empty;
        private string _observacoes = string.Empty;
        private int _quantidadeTratamentos;
        private string _responsavel = string.Empty;
        private string _resultado = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string TipoVerificacao
        {
            get => _tipoVerificacao;
            set => SetProperty(ref _tipoVerificacao, value);
        }

        public DateTime? DataSolicitacao
        {
            get => _dataSolicitacao;
            set
            {
                if (SetProperty(ref _dataSolicitacao, value))
                    OnPropertyChanged(nameof(DataResposta));
            }
        }

        public DateTime? DataResposta
        {
            get => _dataResposta;
            set => SetProperty(ref _dataResposta, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string Observacoes
        {
            get => _observacoes;
            set => SetProperty(ref _observacoes, value);
        }

        public int QuantidadeTratamentos
        {
            get => _quantidadeTratamentos;
            set => SetProperty(ref _quantidadeTratamentos, value);
        }

        public string Responsavel
        {
            get => _responsavel;
            set => SetProperty(ref _responsavel, value);
        }

        public string Resultado
        {
            get => _resultado;
            set => SetProperty(ref _resultado, value);
        }

        public string Error => string.Empty;

        public string this[string columnName]
        {
            get
            {
                return columnName switch
                {
                    nameof(TipoVerificacao) when string.IsNullOrWhiteSpace(TipoVerificacao) => "Tipo de verificação é obrigatório.",
                    nameof(DataSolicitacao) when DataSolicitacao is null => "Data de solicitação é obrigatória.",
                    nameof(Responsavel) when string.IsNullOrWhiteSpace(Responsavel) => "Responsável é obrigatório.",
                    nameof(DataResposta) when DataResposta.HasValue && DataSolicitacao.HasValue && DataResposta.Value.Date < DataSolicitacao.Value.Date => "Data de resposta não pode ser menor que a solicitação.",
                    nameof(QuantidadeTratamentos) when QuantidadeTratamentos < 0 => "Quantidade de tratamentos não pode ser negativa.",
                    _ => string.Empty
                };
            }
        }

        public bool IsValid()
        {
            return string.IsNullOrEmpty(this[nameof(TipoVerificacao)])
                && string.IsNullOrEmpty(this[nameof(DataSolicitacao)])
                && string.IsNullOrEmpty(this[nameof(Responsavel)])
                && string.IsNullOrEmpty(this[nameof(DataResposta)])
                && string.IsNullOrEmpty(this[nameof(QuantidadeTratamentos)]);
        }

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(name);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
