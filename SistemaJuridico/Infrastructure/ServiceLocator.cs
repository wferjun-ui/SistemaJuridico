using SistemaJuridico.Services;

namespace SistemaJuridico.Infrastructure
{
    public static class ServiceLocator
    {
        private static DatabaseService? _database;
        private static ProcessService? _processService;
        private static ContaService? _contaService;
        private static DiligenciaService? _diligenciaService;
        private static HistoricoService? _historicoService;
        private static ItemSaudeService? _itemSaudeService;
        private static VerificacaoService? _verificacaoService;

        public static DatabaseService Database =>
            _database ??= new DatabaseService();

        public static ProcessService ProcessService =>
            _processService ??= new ProcessService(Database);

        public static ContaService ContaService =>
            _contaService ??= new ContaService(Database);

        public static DiligenciaService DiligenciaService =>
            _diligenciaService ??= new DiligenciaService(Database);

        public static HistoricoService HistoricoService =>
            _historicoService ??= new HistoricoService(Database);

        public static ItemSaudeService ItemSaudeService =>
            _itemSaudeService ??= new ItemSaudeService(Database);

        public static VerificacaoService VerificacaoService =>
            _verificacaoService ??= new VerificacaoService(Database);

        public static T Get<T>() where T : class
        {
            if (typeof(T) == typeof(ProcessService)) return (ProcessService as T)!;
            if (typeof(T) == typeof(ContaService)) return (ContaService as T)!;
            if (typeof(T) == typeof(DiligenciaService)) return (DiligenciaService as T)!;
            if (typeof(T) == typeof(HistoricoService)) return (HistoricoService as T)!;
            if (typeof(T) == typeof(ItemSaudeService)) return (ItemSaudeService as T)!;
            if (typeof(T) == typeof(VerificacaoService)) return (VerificacaoService as T)!;
            if (typeof(T) == typeof(DatabaseService)) return (Database as T)!;

            throw new InvalidOperationException($"Tipo não registrado: {typeof(T).FullName}");
        }
    }
}
