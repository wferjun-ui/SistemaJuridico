namespace SistemaJuridico.Services
{
    public static class ConfigService
    {
        private static string ConfigPath =>
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "SistemaJuridico",
                "config.txt");

        public static void SalvarCaminhoBanco(string caminho)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(ConfigPath)!);

            File.WriteAllText(ConfigPath, caminho);
        }

        public static string ObterCaminhoBanco()
        {
            if (!File.Exists(ConfigPath))
                return "";

            return File.ReadAllText(ConfigPath);
        }
    }
}
