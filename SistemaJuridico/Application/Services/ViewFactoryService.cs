using SistemaJuridico.Infrastructure;
using System;
using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;

namespace SistemaJuridico.Services
{
    public class ViewFactoryService
    {
        public ViewFactoryService()
        {
        }

        public UserControl CreateView<TView, TViewModel>()
            where TView : UserControl, new()
            where TViewModel : class
        {
            var view = new TView();

            var vm = ServiceLocator.Get<TViewModel>();

            view.DataContext = vm;

            return view;
        }

        public UserControl CreateView(Type viewType, Type viewModelType)
        {
            var view = Activator.CreateInstance(viewType) as UserControl
                ?? throw new InvalidOperationException($"Não foi possível instanciar a view '{viewType.FullName}'.");

            var vm = ServiceLocator.Get(viewModelType);

            view.DataContext = vm;

            return view;
        }
    }
}
