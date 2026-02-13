using SistemaJuridico.Infrastructure;
using System.Collections.Specialized;
using System.Windows;

namespace SistemaJuridico.Views
{
    public partial class DebugConsoleWindow : Window
    {
        public DebugConsoleWindow()
        {
            InitializeComponent();
            DataContext = DebugConsoleService.Instance;

            DebugConsoleService.Instance.Entries.CollectionChanged += OnEntriesChanged;
            Closed += (_, _) => DebugConsoleService.Instance.Entries.CollectionChanged -= OnEntriesChanged;
        }

        private void OnEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && LogList.Items.Count > 0)
            {
                LogList.ScrollIntoView(LogList.Items[^1]);
            }
        }

        private void OnClearClick(object sender, RoutedEventArgs e)
        {
            DebugConsoleService.Instance.Entries.Clear();
        }
    }
}
