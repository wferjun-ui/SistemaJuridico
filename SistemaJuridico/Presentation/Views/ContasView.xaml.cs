using SistemaJuridico.ViewModels;
using System.Windows;
using System.Windows.Controls;
using DatePicker = System.Windows.Controls.DatePicker;
using UserControl = System.Windows.Controls.UserControl;

namespace SistemaJuridico.Views
{
    public partial class ContasView : UserControl
    {
        public static readonly DependencyProperty ProcessoIdProperty =
            DependencyProperty.Register(
                "ProcessoId",
                typeof(string),
                typeof(ContasView),
                new PropertyMetadata(string.Empty, OnProcessoIdChanged));

        public string ProcessoId
        {
            get => (string)GetValue(ProcessoIdProperty);
            set => SetValue(ProcessoIdProperty, value);
        }

        public ContasView()
        {
            InitializeComponent();
            DataContext = new ContasViewModel();
        }


        private void DatePicker_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not DatePicker datePicker)
                return;

            DatePickerInputHelper.Configure(datePicker);
        }

        private static void OnProcessoIdChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is ContasView view && e.NewValue is string id)
                view.DataContext = new ContasViewModel(id);
        }
    }
}
