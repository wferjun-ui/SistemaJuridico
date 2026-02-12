using SistemaJuridico.Infrastructure;
using System;
using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;

namespace SistemaJuridico.Services
{
    public class ViewFactoryService
    {
        private readonly ServiceLocator _serviceLocator;

        public ViewFactoryService(ServiceLocator serviceLocator)
        {
            _serviceLocator = serviceLocator;
        }

        public UserControl CreateView<TView, TViewModel>()
            where TView : UserControl, new()
        {
            var view = new TView();

            var vm = _serviceLocator.Get<TViewModel>();

            view.DataContext = vm;

            return view;
        }

        public UserControl CreateView(Type viewType, Type viewModelType)
        {
            var view = (UserControl)Activator.CreateInstance(viewType);

            var vm = _serviceLocator.Get(viewModelType);

            view.DataContext = vm;

            return view;
        }
    }
}
