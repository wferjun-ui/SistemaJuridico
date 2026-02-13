using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json.Linq;
using SistemaJuridico.Models;
using System.Collections.ObjectModel;

namespace SistemaJuridico.ViewModels
{
    public partial class AppStateViewModel : ObservableObject
    {
        public static AppStateViewModel Instance { get; } = new();

        [ObservableProperty]
        private Usuario? _usuarioAtual;

        [ObservableProperty]
        private Processo? _processoSelecionado;

        public ObservableCollection<Verificacao> VerificacoesProcesso { get; } = new();
        public ObservableCollection<Conta> ContasProcesso { get; } = new();
        public ObservableCollection<Conta> ContasRascunho { get; } = new();

        [ObservableProperty]
        private ProcessoEstadoAtual _estadoAtual = new();

        public bool PodeVisualizarContas => true;
        public bool PodeVisualizarHistorico => true;
        public bool PodeAcessarVerificacao => !IsVisitante;
        public bool PodeCadastrarContas => !IsVisitante;
        public bool PodeEditarContas => !IsVisitante;
        public bool PodeExcluirContas => IsAdministrador;
        public bool PodeEditarProcesso => IsAdministrador;
        public bool PodeDesfazerVerificacao => IsAdministrador;
        public bool IsVisitante => UsuarioAtual?.IsVisitanteInstitucionalNaoCadastrado == true || string.Equals(UsuarioAtual?.Perfil, "Leitura", StringComparison.OrdinalIgnoreCase);
        public bool IsAdministrador => string.Equals(UsuarioAtual?.Perfil, "Admin", StringComparison.OrdinalIgnoreCase);

        partial void OnUsuarioAtualChanged(Usuario? value)
        {
            OnPropertyChanged(nameof(PodeVisualizarContas));
            OnPropertyChanged(nameof(PodeVisualizarHistorico));
            OnPropertyChanged(nameof(PodeAcessarVerificacao));
            OnPropertyChanged(nameof(PodeCadastrarContas));
            OnPropertyChanged(nameof(PodeEditarContas));
            OnPropertyChanged(nameof(PodeExcluirContas));
            OnPropertyChanged(nameof(PodeEditarProcesso));
            OnPropertyChanged(nameof(PodeDesfazerVerificacao));
            OnPropertyChanged(nameof(IsVisitante));
            OnPropertyChanged(nameof(IsAdministrador));
        }

        public void DefinirContexto(Usuario? usuario, Processo? processo)
        {
            UsuarioAtual = usuario;
            ProcessoSelecionado = processo;
        }

        public void AtualizarVerificacoes(IEnumerable<Verificacao> verificacoes)
        {
            VerificacoesProcesso.Clear();
            foreach (var verificacao in verificacoes)
                VerificacoesProcesso.Add(verificacao);

            EstadoAtual = ProcessoEstadoAtual.FromVerificacoes(VerificacoesProcesso);
        }

        public void AtualizarContas(IEnumerable<Conta> contas)
        {
            ContasProcesso.Clear();
            foreach (var conta in contas)
                ContasProcesso.Add(conta);
        }
    }

    public class ProcessoEstadoAtual
    {
        public string StatusProcesso { get; set; } = string.Empty;
        public string Responsavel { get; set; } = string.Empty;
        public string? Pendencias { get; set; }
        public string? PrazoEspecifico { get; set; }
        public string? ProximoPrazoPadrao { get; set; }
        public string? PrescricaoGlobal { get; set; }
        public List<string> Terapias { get; set; } = new();
        public List<string> Medicamentos { get; set; } = new();
        public List<string> Cirurgias { get; set; } = new();
        public List<string> OutrosItens { get; set; } = new();

        public List<string> TerapiasEMedicamentos => Terapias.Concat(Medicamentos).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        public static ProcessoEstadoAtual FromVerificacoes(IEnumerable<Verificacao> verificacoes)
        {
            var estado = new ProcessoEstadoAtual();
            foreach (var verificacao in verificacoes.OrderByDescending(v => ParseData(v.DataHora) ?? DateTime.MinValue))
            {
                if (string.IsNullOrWhiteSpace(estado.StatusProcesso) && !string.IsNullOrWhiteSpace(verificacao.StatusProcesso))
                    estado.StatusProcesso = verificacao.StatusProcesso;

                if (string.IsNullOrWhiteSpace(estado.Responsavel) && !string.IsNullOrWhiteSpace(verificacao.Responsavel))
                    estado.Responsavel = verificacao.Responsavel;

                if (string.IsNullOrWhiteSpace(estado.Pendencias) && !string.IsNullOrWhiteSpace(verificacao.PendenciaDescricao))
                    estado.Pendencias = verificacao.PendenciaDescricao;

                if (string.IsNullOrWhiteSpace(estado.ProximoPrazoPadrao) && !string.IsNullOrWhiteSpace(verificacao.ProximoPrazo))
                    estado.ProximoPrazoPadrao = verificacao.ProximoPrazo;

                ExtrairItensSaude(verificacao.ItensSnapshotJson, estado);
            }

            return estado;
        }

        private static void ExtrairItensSaude(string? json, ProcessoEstadoAtual estado)
        {
            if (string.IsNullOrWhiteSpace(json))
                return;

            try
            {
                var token = JToken.Parse(json);
                if (token is JArray lista)
                {
                    foreach (var item in lista.OfType<JObject>())
                    {
                        var tipo = item["tipo"]?.ToString();
                        var nome = item["nome"]?.ToString();
                        AdicionarPorTipo(tipo, nome, estado);
                    }
                }
            }
            catch
            {
            }
        }

        private static void AdicionarPorTipo(string? tipo, string? nome, ProcessoEstadoAtual estado)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return;

            if (string.Equals(tipo, "terapia", StringComparison.OrdinalIgnoreCase))
                AdicionarUnico(estado.Terapias, nome);
            else if (string.Equals(tipo, "medicamento", StringComparison.OrdinalIgnoreCase))
                AdicionarUnico(estado.Medicamentos, nome);
            else if (string.Equals(tipo, "cirurgia", StringComparison.OrdinalIgnoreCase))
                AdicionarUnico(estado.Cirurgias, nome);
            else
                AdicionarUnico(estado.OutrosItens, nome);
        }

        private static void AdicionarUnico(List<string> destino, string valor)
        {
            if (!destino.Any(x => string.Equals(x, valor, StringComparison.OrdinalIgnoreCase)))
                destino.Add(valor);
        }

        private static DateTime? ParseData(string? valor)
        {
            if (DateTime.TryParse(valor, out var parsed))
                return parsed;

            return null;
        }
    }
}
