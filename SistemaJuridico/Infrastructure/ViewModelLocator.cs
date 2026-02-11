using SistemaJuridico.Services;

namespace SistemaJuridico.Infrastructure
{
    public class ViewModelLocator
    {
        public static ServiceLocator ServiceLocator { get; set; }

        public static T Resolve<T>()
        {
            return ServiceLocator.Get<T>();
        }
    }
}
