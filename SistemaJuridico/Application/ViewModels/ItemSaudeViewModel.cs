using CommunityToolkit.Mvvm.ComponentModel;
using SistemaJuridico.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace SistemaJuridico.ViewModels
{
    public partial class ItemSaudeViewModel : ObservableObject
    {
        private readonly Func<string, IEnumerable<string>>? _sugestoesPorTipo;

        public ItemSaude Model { get; }

        public ObservableCollection<string> SugestoesNome { get; } = new();

        public ItemSaudeViewModel(
            ItemSaude model,
            Func<string, IEnumerable<string>>? sugestoesPorTipo = null)
        {
            Model = model;
            _sugestoesPorTipo = sugestoesPorTipo;
            AtualizarSugestoesNome();
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
                    AtualizarSugestoesNome();
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

        public void AtualizarSugestoesNome()
        {
            SugestoesNome.Clear();

            if (_sugestoesPorTipo == null)
                return;

            foreach (var sugestao in _sugestoesPorTipo(Tipo)
                         .Where(nome => !string.IsNullOrWhiteSpace(nome))
                         .Select(nome => nome.Trim())
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .OrderBy(nome => nome))
            {
                SugestoesNome.Add(sugestao);
            }

            if (!string.IsNullOrWhiteSpace(Nome)
                && !SugestoesNome.Any(nome => string.Equals(nome, Nome, StringComparison.OrdinalIgnoreCase)))
            {
                SugestoesNome.Add(Nome.Trim());
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
