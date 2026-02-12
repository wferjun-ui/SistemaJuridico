using SistemaJuridico.Services;
using SistemaJuridico.Infrastructure;

namespace SistemaJuridico.Infrastructure
{
    public class ViewModelLocator
    {
        public static T Resolve<T>() where T : class
        {
            return ServiceLocator.Get<T>();
        }
    }
}
