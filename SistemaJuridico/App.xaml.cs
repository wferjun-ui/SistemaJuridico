using SistemaJuridico.Services;
using System;
using System.IO;
using System.Windows;

namespace SistemaJuridico
{
    public partial class App : Application
    {
        public static DatabaseService DB { get; private set; } = null!;
        public static SessionService Session { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string baseFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SistemaJuridico");

            DB = new DatabaseService(baseFolder);
            DB.Initialize();

            Session = new SessionService();
        }
    }
}

