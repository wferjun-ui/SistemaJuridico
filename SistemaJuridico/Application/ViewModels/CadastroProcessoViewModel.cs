using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaJuridico.Models;
using SistemaJuridico.Services;
using System.Windows;

namespace SistemaJuridico.ViewModels
{
    public partial class CadastroProcessoViewModel : ObservableObject
    {
        private readonly ProcessService _service;

        public Processo NovoProcesso { get; set; } = new();

        public Action? FecharTela { get; set; }

        public CadastroProcessoViewModel(ProcessService service)
        {
            _service = service;

            NovoProcesso.StatusFase = "Conhecimento";
            NovoProcesso.UltimaAtualizacao =
                DateTime.Now.ToString("dd/MM/yyyy");
        }

        [RelayCommand]
        private void Salvar()
        {
            if (string.IsNullOrWhiteSpace(NovoProcesso.Numero))
            {
                System.Windows.MessageBox.Show("Número obrigatório.");
                return;
            }

            _service.CriarProcesso(NovoProcesso);

            System.Windows.MessageBox.Show("Processo criado.");

            FecharTela?.Invoke();
        }

        [RelayCommand]
        private void Cancelar()
        {
            FecharTela?.Invoke();
        }
    }
}

