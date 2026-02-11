using System;
using System.Windows.Controls;

namespace SistemaJuridico.Services
{
    public class NavigationService
    {
        private ContentControl _region;

        public void Configure(ContentControl region)
        {
            _region = region;
        }

        public void Navigate(UserControl view)
        {
            if (_region == null)
                throw new InvalidOperationException("NavigationService não configurado.");

            _region.Content = view;
        }

        public void Navigate<TView, TViewModel>(ViewFactoryService factory)
            where TView : UserControl, new()
        {
            var view = factory.CreateView<TView, TViewModel>();
            Navigate(view);
        }
    }
}
