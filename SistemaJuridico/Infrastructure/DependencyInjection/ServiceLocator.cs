using SistemaJuridico.Services;
using System;
using System.Linq;

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
        private static AuditService? _auditService;

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

        public static AuditService AuditService =>
            _auditService ??= new AuditService(Database);

        public static T Get<T>() where T : class
            => (Get(typeof(T)) as T)
               ?? throw new InvalidOperationException($"Tipo não registrado: {typeof(T).FullName}");

        public static object Get(Type type)
        {
            if (type == typeof(ProcessService)) return ProcessService;
            if (type == typeof(ContaService)) return ContaService;
            if (type == typeof(DiligenciaService)) return DiligenciaService;
            if (type == typeof(HistoricoService)) return HistoricoService;
            if (type == typeof(ItemSaudeService)) return ItemSaudeService;
            if (type == typeof(VerificacaoService)) return VerificacaoService;
            if (type == typeof(AuditService)) return AuditService;
            if (type == typeof(DatabaseService)) return Database;

            var ctor = type.GetConstructors()
                .OrderBy(c => c.GetParameters().Length)
                .FirstOrDefault();

            if (ctor == null)
                throw new InvalidOperationException($"Tipo sem construtor público: {type.FullName}");

            var args = ctor.GetParameters()
                .Select(p => ResolveParameter(p.ParameterType))
                .ToArray();

            return Activator.CreateInstance(type, args)
                ?? throw new InvalidOperationException($"Não foi possível instanciar: {type.FullName}");
        }

        private static object? ResolveParameter(Type paramType)
        {
            if (paramType == typeof(string))
                return string.Empty;

            if (paramType.IsValueType)
                return Activator.CreateInstance(paramType);

            return Get(paramType);
        }
    }
}
