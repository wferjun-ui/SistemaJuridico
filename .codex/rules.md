# Regras Arquiteturais do SistemaJuridico

## Obrigatório

- Usar MVVM manual
- Usar ServiceLocator próprio
- Não usar Microsoft.Extensions.DependencyInjection
- Sempre retornar arquivos completos
- Não criar novas camadas sem autorização
- Manter compatibilidade com multiusuário

## WPF

- Nunca duplicar partial classes
- Nunca duplicar InitializeComponent
- Evitar múltiplos XAML para mesma classe

## Navegação

- Apenas UMA View controla cada Window
- Usar NavigationCoordinatorService

## Multiusuário

- Locks obrigatórios via ProcessoEdicaoEstadoService
