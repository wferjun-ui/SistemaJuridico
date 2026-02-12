using CommunityToolkit.Mvvm.ComponentModel;
using SistemaJuridico.Models;

namespace SistemaJuridico.ViewModels
{
    public partial class ItemSaudeViewModel : ObservableObject
    {
        public ItemSaude Model { get; }

        public ItemSaudeViewModel(ItemSaude model)
        {
            Model = model;
        }

        // =====================
        // PROPRIEDADES BINDING
        // =====================

        public string Id => Model.Id;

        public string ProcessoId
        {
            get => Model.ProcessoId;
            set
            {
                if (Model.ProcessoId != value)
                {
                    Model.ProcessoId = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Tipo
        {
            get => Model.Tipo;
            set
            {
                if (Model.Tipo != value)
                {
                    Model.Tipo = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Nome
        {
            get => Model.Nome;
            set
            {
                if (Model.Nome != value)
                {
                    Model.Nome = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Qtd
        {
            get => Model.Qtd;
            set
            {
                if (Model.Qtd != value)
                {
                    Model.Qtd = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Frequencia
        {
            get => Model.Frequencia;
            set
            {
                if (Model.Frequencia != value)
                {
                    Model.Frequencia = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Local
        {
            get => Model.Local;
            set
            {
                if (Model.Local != value)
                {
                    Model.Local = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DataPrescricao
        {
            get => Model.DataPrescricao;
            set
            {
                if (Model.DataPrescricao != value)
                {
                    Model.DataPrescricao = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsDesnecessario
        {
            get => Model.IsDesnecessario;
            set
            {
                if (Model.IsDesnecessario != value)
                {
                    Model.IsDesnecessario = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool TemBloqueio
        {
            get => Model.TemBloqueio;
            set
            {
                if (Model.TemBloqueio != value)
                {
                    Model.TemBloqueio = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
