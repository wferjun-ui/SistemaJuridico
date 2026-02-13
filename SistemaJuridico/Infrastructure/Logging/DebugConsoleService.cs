using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace SistemaJuridico.Infrastructure
{
    public sealed class DebugConsoleService
    {
        private readonly object _sync = new();
        private const int MaxItems = 1000;

        public static DebugConsoleService Instance { get; } = new();

        public ObservableCollection<string> Entries { get; } = new();

        private DebugConsoleService()
        {
        }

        public void Add(string line)
        {
            void AddCore()
            {
                lock (_sync)
                {
                    Entries.Add(line);
                    if (Entries.Count > MaxItems)
                        Entries.RemoveAt(0);
                }
            }

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                AddCore();
                return;
            }

            dispatcher.BeginInvoke((Action)AddCore);
        }
    }
}
