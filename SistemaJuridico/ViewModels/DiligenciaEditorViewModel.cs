using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaJuridico.Models;
using SistemaJuridico.Services;
using System;
using System.Windows;

namespace SistemaJuridico.ViewModels
{
    public partial class DiligenciaEditorViewModel : ObservableObject
    {
        private readonly ProcessoFacadeService _facade;
        private readonly string _processoId;

        public DiligenciaEditorViewModel(
            string processoId,
            ProcessoFacadeService facade)
        {
            _processoId = processoId;
            _facade = facade;

            DataPrazo = DateTime.Today;
        }

        [ObservableProperty]
        private string descricao = "";

        [ObservableProperty]
        private DateTime dataPrazo;

        public bool Salvou { get; private set; }

        [RelayCommand]
        private void Salvar(Window janela)
        {
            if (string.IsNullOrWhiteSpace(Descricao))
            {
                MessageBox.Show("Informe a descrição.");
                return;
            }

            var diligencia = new Diligencia
            {
                Id = Guid.NewGuid().ToString(),
                ProcessoId = _processoId,
                Descricao = Descricao,
                Prazo = DataPrazo,
                Concluida = false
            };

            _facade.InserirDiligencia(diligencia);

            Salvou = true;
            janela.Close();
        }

        [RelayCommand]
        private void Cancelar(Window janela)
        {
            janela.Close();
        }
    }
}
