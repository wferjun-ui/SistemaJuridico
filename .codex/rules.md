# Regras Arquiteturais – SistemaJuridico

## Arquitetura Geral
- Padrão MVVM manual deve ser preservado.
- Não usar Microsoft.Extensions.DependencyInjection.
- A implementação atual de ServiceLocator deve permanecer compatível.
- Manter compatibilidade com multiusuário e lock de edição.

## WPF
- Não duplicar partial classes nem InitializeComponent.
- Cada XAML deve mapear para exatamente uma classe code-behind.
- Mudar namespace só com justificativa.

## Navegação
- NavigationCoordinatorService deve orquestrar navegação.
- Somente uma View controla uma Window.
- Não criar janelas “host” redundantes.

## Multiusuário
- Bloqueios devem ser gerenciados por ProcessoEdicaoEstadoService.
- Locks devem ser liberados ao fechar View.

## Código
- Sempre retornar arquivos completos nas respostas.
- Não sugerir refatoração massiva sem autorização.

## Pull Requests
- O título deve indicar “Automated fix” ou “IA assistida”.
- O corpo deve explicar a intenção e listar arquivos afetados.
