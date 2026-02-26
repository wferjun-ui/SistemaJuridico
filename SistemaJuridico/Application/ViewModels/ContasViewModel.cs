using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaJuridico.Models;
using SistemaJuridico.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using System.Text.Json;

namespace SistemaJuridico.ViewModels
{
    public partial class ContasViewModel : ObservableObject
    {
        private readonly ContaService _service;
        private readonly VerificacaoService _verificacaoService;
        private readonly AuditService _auditService;
        private readonly HistoricoService _historicoService;
        private readonly string _processoId;
        private readonly AppStateViewModel _appState;

        public ObservableCollection<Conta> Contas => _appState.ContasProcesso;
        public ObservableCollection<Conta> ContasRascunho => _appState.ContasRascunho;
        public ObservableCollection<string> TerapiasMedicamentosDisponiveis { get; } = new();
        public ObservableCollection<ContaHistoricoLinha> HistoricoContas { get; } = new();

        [ObservableProperty]
        private Conta? _contaSelecionada;

        [ObservableProperty]
        private Conta _edicaoConta = new();

        [ObservableProperty]
        private string _terapiaManual = string.Empty;

        [ObservableProperty]
        private string _tipoLancamentoSelecionado = "Alvará";

        [ObservableProperty]
        private string _modoMovimentoConta = "Anexo";

        [ObservableProperty]
        private string _movimentoContaDigitado = string.Empty;

        public bool PodeCadastrar => _appState.PodeCadastrarContas;
        public bool PodeEditar => _appState.PodeEditarContas;
        public bool PodeExcluir => _appState.PodeExcluirContas;
        public bool IsAlvara => string.Equals(EdicaoConta.TipoLancamento, "Alvará", StringComparison.OrdinalIgnoreCase);
        public bool IsTratamento => string.Equals(EdicaoConta.TipoLancamento, "Tratamento", StringComparison.OrdinalIgnoreCase);
        public bool IsDespesaGeral => string.Equals(EdicaoConta.TipoLancamento, "Despesa Geral", StringComparison.OrdinalIgnoreCase);
        public bool IsValorContaHabilitado => !IsAlvara;
        public bool IsCampoMovimentoVisivel => IsAlvara;
        public bool IsCampoNfAlvaraVisivel => IsAlvara;
        public bool ExibirCampoTerapiaManual => IsTratamento && string.Equals(EdicaoConta.TerapiaMedicamentoNome, "OUTRO", StringComparison.OrdinalIgnoreCase);
        public bool ExibirFormularioContas => PodeCadastrar;
        public bool IsMovimentoOpcional => !IsAlvara;
        public bool ExibirCampoMovimentoDigitado => IsMovimentoOpcional && string.Equals(ModoMovimentoConta, "Digitar", StringComparison.OrdinalIgnoreCase);
        public bool ExibirCamposReferencia => IsTratamento || IsDespesaGeral;

        public ContasViewModel() : this(string.Empty)
        {
        }

        public ContasViewModel(string processoId)
        {
            _processoId = processoId;
            _appState = AppStateViewModel.Instance;
            _appState.DefinirContexto(App.Session.UsuarioAtual, _appState.ProcessoSelecionado);

            var db = new DatabaseService();
            _service = new ContaService(db);
            _verificacaoService = new VerificacaoService(db);
            _auditService = new AuditService(db);
            _historicoService = new HistoricoService(db);

            Carregar();
            CarregarRascunhos();
            NovaConta();
        }

        partial void OnEdicaoContaChanged(Conta value)
        {
            OnPropertyChanged(nameof(IsAlvara));
            OnPropertyChanged(nameof(IsTratamento));
            OnPropertyChanged(nameof(IsDespesaGeral));
            OnPropertyChanged(nameof(IsValorContaHabilitado));
            OnPropertyChanged(nameof(IsCampoMovimentoVisivel));
            OnPropertyChanged(nameof(IsCampoNfAlvaraVisivel));
            OnPropertyChanged(nameof(ExibirCampoTerapiaManual));
            OnPropertyChanged(nameof(ExibirFormularioContas));
            OnPropertyChanged(nameof(IsMovimentoOpcional));
            OnPropertyChanged(nameof(ExibirCampoMovimentoDigitado));
            OnPropertyChanged(nameof(ExibirCamposReferencia));
        }


        partial void OnModoMovimentoContaChanged(string value)
        {
            if (IsMovimentoOpcional)
                EdicaoConta.MovProcesso = string.Equals(value, "Digitar", StringComparison.OrdinalIgnoreCase)
                    ? MovimentoContaDigitado
                    : "Anexo";

            OnPropertyChanged(nameof(ExibirCampoMovimentoDigitado));
        }

        partial void OnMovimentoContaDigitadoChanged(string value)
        {
            if (IsMovimentoOpcional && string.Equals(ModoMovimentoConta, "Digitar", StringComparison.OrdinalIgnoreCase))
                EdicaoConta.MovProcesso = value;
        }
        partial void OnTipoLancamentoSelecionadoChanged(string value)
        {
            EdicaoConta.TipoLancamento = value;
            AplicarRegraTipoLancamento(EdicaoConta);
            OnPropertyChanged(nameof(IsAlvara));
            OnPropertyChanged(nameof(IsTratamento));
            OnPropertyChanged(nameof(IsDespesaGeral));
            OnPropertyChanged(nameof(IsValorContaHabilitado));
            OnPropertyChanged(nameof(IsCampoMovimentoVisivel));
            OnPropertyChanged(nameof(IsCampoNfAlvaraVisivel));
            OnPropertyChanged(nameof(ExibirCampoTerapiaManual));
            OnPropertyChanged(nameof(IsMovimentoOpcional));
            OnPropertyChanged(nameof(ExibirCampoMovimentoDigitado));
            OnPropertyChanged(nameof(ExibirCamposReferencia));
            OnPropertyChanged(nameof(ValorAlvaraTexto));
            OnPropertyChanged(nameof(ValorContaTexto));
        }

        [RelayCommand]
        private void Carregar()
        {
            Contas.Clear();
            HistoricoContas.Clear();

            if (string.IsNullOrWhiteSpace(_processoId))
                return;

            var contasOrdenadas = _service.ListarPorProcesso(_processoId)
                .OrderBy(c => ParseData(c.DataMovimentacao) ?? DateTime.MinValue)
                .ToList();

            foreach (var c in contasOrdenadas)
                Contas.Add(c);

            _appState.AtualizarContas(contasOrdenadas);

            var verificacoes = _verificacaoService.ListarPorProcesso(_processoId);
            _appState.AtualizarVerificacoes(verificacoes);

            TerapiasMedicamentosDisponiveis.Clear();
            foreach (var item in _appState.EstadoAtual.TerapiasEMedicamentos)
                TerapiasMedicamentosDisponiveis.Add(item);
            TerapiasMedicamentosDisponiveis.Add("OUTRO");

            decimal saldo = 0m;
            foreach (var conta in contasOrdenadas)
            {
                saldo += conta.ValorAlvara - conta.ValorConta;
                HistoricoContas.Add(new ContaHistoricoLinha(conta, saldo));
            }
        }

        [RelayCommand]
        private void NovaConta()
        {
            EdicaoConta = new Conta
            {
                ProcessoId = _processoId,
                Responsavel = App.Session.UsuarioAtual?.Nome ?? "Sistema",
                TipoLancamento = "Alvará"
            };
            TipoLancamentoSelecionado = "Alvará";
            ModoMovimentoConta = "Anexo";
            MovimentoContaDigitado = string.Empty;
            TerapiaManual = string.Empty;
        }

        [RelayCommand]
        private void AdicionarRascunho()
        {
            if (!PodeCadastrar)
            {
                System.Windows.MessageBox.Show("Seu perfil não possui permissão para cadastrar contas.");
                return;
            }

            if (!ValidarConta(EdicaoConta))
                return;

            PrepararContaParaPersistencia(EdicaoConta, definirComoLancado: false);

            if (!string.IsNullOrWhiteSpace(EdicaoConta.Id) && Contas.Any(x => x.Id == EdicaoConta.Id))
                EdicaoConta.Id = Guid.NewGuid().ToString();

            ContasRascunho.Add(CloneConta(EdicaoConta));
            PersistirRascunhos();
            NovaConta();
        }

        [RelayCommand]
        private void RemoverRascunho(Conta? conta)
        {
            if (conta is null)
                return;

            ContasRascunho.Remove(conta);
            PersistirRascunhos();
        }

        [RelayCommand]
        private void EditarRascunho(Conta? conta)
        {
            if (conta is null)
                return;

            if (!PodeEditar)
            {
                System.Windows.MessageBox.Show("Seu perfil não possui permissão para editar contas.");
                return;
            }

            EdicaoConta = CloneConta(conta);
            TipoLancamentoSelecionado = EdicaoConta.TipoLancamento;

            if (IsMovimentoOpcional)
            {
                var mov = EdicaoConta.MovProcesso ?? string.Empty;
                var isAnexo = string.Equals(mov, "Anexo", StringComparison.OrdinalIgnoreCase);
                ModoMovimentoConta = isAnexo ? "Anexo" : "Digitar";
                MovimentoContaDigitado = isAnexo ? string.Empty : mov;
            }
        }

        [RelayCommand]
        private void LimparRascunhos()
        {
            ContasRascunho.Clear();
            PersistirRascunhos();
        }

        [RelayCommand]
        private void ConfirmarRascunhos()
        {
            if (!PodeCadastrar)
            {
                System.Windows.MessageBox.Show("Seu perfil não possui permissão para cadastrar contas.");
                return;
            }

            if (ContasRascunho.Count == 0)
            {
                System.Windows.MessageBox.Show("Não há lançamentos em rascunho para confirmar.");
                return;
            }

            foreach (var conta in ContasRascunho)
            {
                conta.ProcessoId = _processoId;
                conta.StatusConta = "lancado";
                _service.Inserir(conta);
                _historicoService.Registrar(_processoId, "Lançamento contábil incluído", MontarResumoConta(conta));
                _auditService.Registrar(
                    "Conta.Criada",
                    "processo",
                    _processoId,
                    $"Nova conta {conta.TipoLancamento} em {conta.DataMovimentacao}: +{conta.ValorAlvara} -{conta.ValorConta}");
            }

            ContasRascunho.Clear();
            PersistirRascunhos();
            Carregar();
        }

        [RelayCommand]
        private void ConfirmarRascunho(Conta? conta)
        {
            if (conta is null)
                return;

            if (!PodeCadastrar)
            {
                System.Windows.MessageBox.Show("Seu perfil não possui permissão para cadastrar contas.");
                return;
            }

            conta.ProcessoId = _processoId;
            conta.StatusConta = "lancado";
            _service.Inserir(conta);

            _historicoService.Registrar(_processoId, "Lançamento contábil incluído", MontarResumoConta(conta));
            _auditService.Registrar(
                "Conta.Criada",
                "processo",
                _processoId,
                $"Nova conta {conta.TipoLancamento} em {conta.DataMovimentacao}: +{conta.ValorAlvara} -{conta.ValorConta}");

            ContasRascunho.Remove(conta);
            PersistirRascunhos();
            Carregar();
        }

        [RelayCommand]
        private void EditarContaHistorico(ContaHistoricoLinha? linha)
        {
            if (linha is null)
                return;

            ContaSelecionada = linha.Conta;
            EditarConta();
        }

        [RelayCommand]
        private void ExcluirContaHistorico(ContaHistoricoLinha? linha)
        {
            if (linha is null)
                return;

            ContaSelecionada = linha.Conta;
            ExcluirConta();
        }

        [RelayCommand]
        private void LancarContaHistorico(ContaHistoricoLinha? linha)
        {
            if (linha is null)
                return;

            ContaSelecionada = linha.Conta;
            FecharConta();
        }

        [RelayCommand]
        private void EditarConta()
        {
            if (ContaSelecionada == null)
                return;

            if (!PodeEditar)
            {
                System.Windows.MessageBox.Show("Seu perfil não possui permissão para editar contas.");
                return;
            }

            if (!ContaSelecionada.PodeEditar)
            {
                System.Windows.MessageBox.Show("Conta já fechada.");
                return;
            }

            EdicaoConta = CloneConta(ContaSelecionada);
            TipoLancamentoSelecionado = EdicaoConta.TipoLancamento;
            if (IsMovimentoOpcional)
            {
                var mov = EdicaoConta.MovProcesso ?? string.Empty;
                var isAnexo = string.Equals(mov, "Anexo", StringComparison.OrdinalIgnoreCase);
                ModoMovimentoConta = isAnexo ? "Anexo" : "Digitar";
                MovimentoContaDigitado = isAnexo ? string.Empty : mov;
            }
        }

        [RelayCommand]
        private void SalvarEdicaoConta()
        {
            if (ContaSelecionada == null)
                return;

            if (!PodeEditar)
            {
                System.Windows.MessageBox.Show("Seu perfil não possui permissão para editar contas.");
                return;
            }

            if (!ValidarConta(EdicaoConta))
                return;

            PrepararContaParaPersistencia(EdicaoConta, definirComoLancado: false);

            _service.Atualizar(EdicaoConta);
            _historicoService.Registrar(_processoId, "Conta individual editada", MontarResumoConta(EdicaoConta));
            _auditService.Registrar(
                "Conta.Editada",
                "processo",
                _processoId,
                $"Conta {EdicaoConta.Id} atualizada");

            Carregar();
            NovaConta();
        }

        [RelayCommand]
        private void ExcluirConta()
        {
            if (ContaSelecionada == null)
                return;

            if (!PodeExcluir)
            {
                System.Windows.MessageBox.Show("Somente administradores podem excluir contas.");
                return;
            }

            if (System.Windows.MessageBox.Show("Excluir conta?", "Confirma", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _historicoService.Registrar(_processoId, "Conta individual excluída", MontarResumoConta(ContaSelecionada));
                _service.Excluir(ContaSelecionada.Id);
                _auditService.Registrar(
                    "Conta.Excluida",
                    "processo",
                    _processoId,
                    $"Conta {ContaSelecionada.Id} removida");
                Carregar();
            }
        }


        [RelayCommand]
        private void ExportarPrestacaoContasPdf()
        {
            if (HistoricoContas.Count == 0)
            {
                System.Windows.MessageBox.Show("Não há contas para exportar.");
                return;
            }

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Arquivo PDF (*.pdf)|*.pdf",
                FileName = $"prestacao-contas-{DateTime.Now:yyyyMMdd-HHmm}.pdf"
            };

            if (dlg.ShowDialog() != true)
                return;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    var totalCreditos = HistoricoContas.Sum(x => x.Conta.ValorAlvara);
                    var totalDebitos = HistoricoContas.Sum(x => x.Conta.ValorConta);
                    var saldoFinal = totalCreditos - totalDebitos;

                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(28);
                    page.Header().Column(col =>
                    {
                        col.Item().Text("Prestação de Contas").Bold().FontSize(18);
                        col.Item().Text($"Processo: {_processoId}");
                        col.Item().Text($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}");
                        col.Item().PaddingTop(6).LineHorizontal(1);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.4f);
                            columns.RelativeColumn(2.6f);
                            columns.RelativeColumn(1.3f);
                            columns.RelativeColumn(1.3f);
                            columns.RelativeColumn(1.4f);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Text("Data").Bold();
                            h.Cell().Text("Tipo").Bold();
                            h.Cell().Text("Histórico").Bold();
                            h.Cell().AlignRight().Text("Entrada").Bold();
                            h.Cell().AlignRight().Text("Saída").Bold();
                            h.Cell().AlignRight().Text("Saldo").Bold();
                        });

                        foreach (var linha in HistoricoContas)
                        {
                            table.Cell().Text(linha.Conta.DataMovimentacao);
                            table.Cell().Text(linha.Conta.TipoLancamento);
                            table.Cell().Text(linha.Conta.Historico);
                            table.Cell().AlignRight().Text(t =>
                            {
                                var cor = linha.Conta.ValorAlvara > 0 ? Colors.Green.Darken1 : Colors.Grey.Medium;
                                t.Span(linha.Conta.ValorAlvara.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))).FontColor(cor);
                            });
                            table.Cell().AlignRight().Text(t =>
                            {
                                var cor = linha.Conta.ValorConta > 0 ? Colors.Red.Darken1 : Colors.Grey.Medium;
                                t.Span(linha.Conta.ValorConta.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))).FontColor(cor);
                            });
                            table.Cell().AlignRight().Text(linha.SaldoParcial.ToString("C", CultureInfo.GetCultureInfo("pt-BR")));
                        }
                    });

                    page.Footer().PaddingTop(10).Column(col =>
                    {
                        col.Item().LineHorizontal(1);
                        col.Item().PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem().Text($"Total de Créditos: {totalCreditos.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))}").FontColor(Colors.Green.Darken2);
                            r.RelativeItem().AlignCenter().Text($"Total de Débitos: {totalDebitos.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))}").FontColor(Colors.Red.Darken2);
                            r.RelativeItem().AlignRight().Text($"Saldo Final: {saldoFinal.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))}").Bold();
                        });
                    });
                });
            }).GeneratePdf(dlg.FileName);

            System.Windows.MessageBox.Show("PDF de prestação de contas gerado com sucesso.");
        }

        [RelayCommand]
        private void FecharConta()
        {
            if (ContaSelecionada == null)
                return;

            _service.FecharConta(ContaSelecionada.Id);
            _historicoService.Registrar(_processoId, "Conta fechada", $"{MontarResumoConta(ContaSelecionada)}. Status: fechada.");
            _auditService.Registrar("Conta.Fechada", "processo", _processoId, $"Conta {ContaSelecionada.Id} fechada");
            Carregar();
        }

        private bool ValidarConta(Conta conta)
        {
            if (string.IsNullOrWhiteSpace(conta.TipoLancamento))
            {
                System.Windows.MessageBox.Show("Tipo de lançamento obrigatório.");
                return false;
            }

            if (!DateTime.TryParse(conta.DataMovimentacao, out _))
            {
                System.Windows.MessageBox.Show("Data da movimentação inválida.");
                return false;
            }

            if (IsAlvara)
            {
                if (string.IsNullOrWhiteSpace(conta.MovProcesso) || string.Equals(conta.MovProcesso, "Anexo", StringComparison.OrdinalIgnoreCase))
                {
                    System.Windows.MessageBox.Show("Movimento processual digitado é obrigatório para Alvará.");
                    return false;
                }

                if (!EhMovimentoValido(conta.MovProcesso))
                {
                    System.Windows.MessageBox.Show("Movimento processual deve conter apenas números e pontos.");
                    return false;
                }

                if (conta.ValorAlvara <= 0)
                {
                    System.Windows.MessageBox.Show("Valor do Alvará deve ser maior que zero.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(conta.NumNfAlvara))
                {
                    System.Windows.MessageBox.Show("Número de NF/Alvará é obrigatório para lançamentos do tipo Alvará.");
                    return false;
                }
            }
            else
            {
                conta.MovProcesso = string.Equals(ModoMovimentoConta, "Digitar", StringComparison.OrdinalIgnoreCase)
                    ? MovimentoContaDigitado?.Trim()
                    : "Anexo";

                if (string.Equals(ModoMovimentoConta, "Digitar", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(conta.MovProcesso))
                {
                    System.Windows.MessageBox.Show("Informe o número do movimento processual quando o modo for Digitar.");
                    return false;
                }

                if (string.Equals(ModoMovimentoConta, "Digitar", StringComparison.OrdinalIgnoreCase) && !EhMovimentoValido(conta.MovProcesso))
                {
                    System.Windows.MessageBox.Show("Movimento processual deve conter apenas números e pontos.");
                    return false;
                }

                if (conta.ValorConta <= 0)
                {
                    System.Windows.MessageBox.Show("Valor da Conta deve ser maior que zero.");
                    return false;
                }

                if (IsTratamento)
                {
                    if (string.IsNullOrWhiteSpace(conta.TerapiaMedicamentoNome))
                    {
                        System.Windows.MessageBox.Show("Informe a terapia/medicamento para o tipo Tratamento.");
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(conta.Quantidade))
                    {
                        System.Windows.MessageBox.Show("Quantidade é obrigatória para Tratamento.");
                        return false;
                    }
                }

                if (ExibirCamposReferencia)
                {
                    if (!string.IsNullOrWhiteSpace(conta.MesReferencia) && (!int.TryParse(conta.MesReferencia, out var mes) || mes < 1 || mes > 12))
                    {
                        System.Windows.MessageBox.Show("Mês de referência inválido. Informe valor entre 1 e 12.");
                        return false;
                    }

                    if (!string.IsNullOrWhiteSpace(conta.AnoReferencia) && (!int.TryParse(conta.AnoReferencia, out var ano) || ano < 1900 || ano > 3000))
                    {
                        System.Windows.MessageBox.Show("Ano de referência inválido.");
                        return false;
                    }
                }
            }

            if (ExibirCampoTerapiaManual && string.IsNullOrWhiteSpace(TerapiaManual))
            {
                System.Windows.MessageBox.Show("Informe a terapia/medicamento manual.");
                return false;
            }

            return true;
        }

        private void AplicarRegraTipoLancamento(Conta conta)
        {
            if (string.Equals(conta.TipoLancamento, "Alvará", StringComparison.OrdinalIgnoreCase))
            {
                conta.ValorConta = 0m;
                conta.TerapiaMedicamentoNome = null;
                conta.Quantidade = null;
                conta.MesReferencia = null;
                conta.AnoReferencia = null;
                conta.Observacoes = null;
            }
            else if (string.Equals(conta.TipoLancamento, "Tratamento", StringComparison.OrdinalIgnoreCase))
            {
                conta.ValorAlvara = 0m;
            }
            else if (string.Equals(conta.TipoLancamento, "Despesa Geral", StringComparison.OrdinalIgnoreCase))
            {
                conta.ValorAlvara = 0m;
                conta.Quantidade = null;
            }
        }

        private void PrepararContaParaPersistencia(Conta conta, bool definirComoLancado)
        {
            AplicarRegraTipoLancamento(conta);

            if (ExibirCampoTerapiaManual)
                conta.TerapiaMedicamentoNome = TerapiaManual.Trim();

            conta.Historico = MontarHistoricoConta(conta);
            conta.Responsavel = string.IsNullOrWhiteSpace(conta.Responsavel)
                ? App.Session.UsuarioAtual?.Nome ?? "Sistema"
                : conta.Responsavel.Trim();

            conta.StatusConta = definirComoLancado ? "lancado" : "rascunho";
        }

        private static bool EhMovimentoValido(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return false;

            return valor.All(c => char.IsDigit(c) || c == '.');
        }

        private static string MontarHistoricoConta(Conta conta)
        {
            if (string.Equals(conta.TipoLancamento, "Alvará", StringComparison.OrdinalIgnoreCase))
                return $"Recebimento de Alvará {(string.IsNullOrWhiteSpace(conta.NumNfAlvara) ? string.Empty : $"(NF/Alvará: {conta.NumNfAlvara})")}".Trim();

            if (string.Equals(conta.TipoLancamento, "Tratamento", StringComparison.OrdinalIgnoreCase))
            {
                var historico = conta.TerapiaMedicamentoNome?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(conta.Quantidade))
                    historico += $" - {conta.Quantidade} un.";
                if (!string.IsNullOrWhiteSpace(conta.MesReferencia) && !string.IsNullOrWhiteSpace(conta.AnoReferencia))
                    historico += $" - Ref: {conta.MesReferencia}/{conta.AnoReferencia}";
                if (!string.IsNullOrWhiteSpace(conta.Observacoes))
                    historico += $" ({conta.Observacoes.Trim()})";
                return historico.Trim();
            }

            if (string.Equals(conta.TipoLancamento, "Despesa Geral", StringComparison.OrdinalIgnoreCase))
                return $"Despesa Geral: {conta.TerapiaMedicamentoNome ?? string.Empty} {(string.IsNullOrWhiteSpace(conta.NumNfAlvara) ? string.Empty : $"(NF/Alvará: {conta.NumNfAlvara})")}".Trim();

            return conta.Historico?.Trim() ?? string.Empty;
        }

        public void AtualizarValorAlvaraTexto(string texto)
        {
            EdicaoConta.ValorAlvara = ParseMoeda(texto);
            OnPropertyChanged(nameof(ValorAlvaraTexto));
        }

        public void AtualizarValorContaTexto(string texto)
        {
            EdicaoConta.ValorConta = ParseMoeda(texto);
            OnPropertyChanged(nameof(ValorContaTexto));
        }

        public string ValorAlvaraTexto => FormatarMoeda(EdicaoConta.ValorAlvara);
        public string ValorContaTexto => FormatarMoeda(EdicaoConta.ValorConta);

        private static decimal ParseMoeda(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return 0m;

            var limpo = new string(texto.Where(c => char.IsDigit(c) || c == ',' || c == '.').ToArray());
            if (decimal.TryParse(limpo, NumberStyles.Any, CultureInfo.GetCultureInfo("pt-BR"), out var valor))
                return valor;

            return 0m;
        }

        private static string FormatarMoeda(decimal valor)
        {
            if (valor <= 0m)
                return string.Empty;

            return valor.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
        }

        private static DateTime? ParseData(string? valor)
        {
            if (DateTime.TryParse(valor, out var parsed))
                return parsed;

            return null;
        }

        private static Conta CloneConta(Conta origem)
        {
            return new Conta
            {
                Id = origem.Id,
                ProcessoId = origem.ProcessoId,
                TipoLancamento = origem.TipoLancamento,
                Historico = origem.Historico,
                DataMovimentacao = origem.DataMovimentacao,
                MovProcesso = origem.MovProcesso,
                NumNfAlvara = origem.NumNfAlvara,
                ValorAlvara = origem.ValorAlvara,
                ValorConta = origem.ValorConta,
                TerapiaMedicamentoNome = origem.TerapiaMedicamentoNome,
                Quantidade = origem.Quantidade,
                MesReferencia = origem.MesReferencia,
                AnoReferencia = origem.AnoReferencia,
                StatusConta = origem.StatusConta,
                Responsavel = origem.Responsavel,
                Observacoes = origem.Observacoes
            };
        }

        private void CarregarRascunhos()
        {
            ContasRascunho.Clear();

            var caminho = ObterCaminhoRascunho();
            if (!File.Exists(caminho))
                return;

            try
            {
                var conteudo = File.ReadAllText(caminho);
                var itens = JsonSerializer.Deserialize<List<Conta>>(conteudo) ?? new List<Conta>();
                foreach (var item in itens)
                    ContasRascunho.Add(item);
            }
            catch
            {
                File.Delete(caminho);
            }
        }

        private void PersistirRascunhos()
        {
            var caminho = ObterCaminhoRascunho();
            var pasta = Path.GetDirectoryName(caminho);
            if (!string.IsNullOrWhiteSpace(pasta))
                Directory.CreateDirectory(pasta);

            var json = JsonSerializer.Serialize(ContasRascunho.ToList());
            File.WriteAllText(caminho, json);
        }

        private string ObterCaminhoRascunho()
        {
            var basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SistemaJuridico", "contas-rascunho");
            var usuario = App.Session.UsuarioAtual?.Id ?? "anonimo";
            var processo = string.IsNullOrWhiteSpace(_processoId) ? "sem-processo" : _processoId;
            return Path.Combine(basePath, $"{usuario}-{processo}.json");
        }

        private static string MontarResumoConta(Conta conta)
        {
            return $"Data {conta.DataMovimentacao}, tipo {conta.TipoLancamento}, mov. {conta.MovProcesso ?? "N/A"}, " +
                   $"entrada {conta.ValorAlvara.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))}, " +
                   $"saída {conta.ValorConta.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))}, histórico: {conta.Historico}";
        }
    }

    public class ContaHistoricoLinha
    {
        public ContaHistoricoLinha(Conta conta, decimal saldoParcial)
        {
            Conta = conta;
            SaldoParcial = saldoParcial;
        }

        public Conta Conta { get; }
        public decimal SaldoParcial { get; }
    }
}
