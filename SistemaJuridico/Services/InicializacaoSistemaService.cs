using System;
using System.IO;

namespace SistemaJuridico.Services
{
    public class InicializacaoSistemaService
    {
        public string Inicializar()
        {
            var caminhoDb = ConfigService.ObterCaminhoBanco();

            if (string.IsNullOrEmpty(caminhoDb))
            {
                caminhoDb = SelecionarPastaBanco();

                if (string.IsNullOrEmpty(caminhoDb))
                    throw new Exception("Banco não configurado.");

                ConfigService.SalvarCaminhoBanco(caminhoDb);
            }

            var db = new DatabaseService(Path.GetDirectoryName(caminhoDb)!);
            db.Initialize();

            var versionador = new DatabaseVersionService(db);
            versionador.GarantirAtualizacao();

            ImportarSeExistirJson(db);

            return caminhoDb;
        }

        private string SelecionarPastaBanco()
        {
            var pasta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SistemaJuridico");

            Directory.CreateDirectory(pasta);
            return Path.Combine(pasta, "juridico.db");
        }

        private void ImportarSeExistirJson(DatabaseService db)
        {
            var pasta = ConfigService.ObterCaminhoBanco();
            var folder = Path.GetDirectoryName(pasta!);

            var jsonPath =
                Path.Combine(folder!, "MIGRACAO_COMPLETA_JURIDICO.json");

            if (!File.Exists(jsonPath))
                return;

            var importer = new ImportacaoJsonService(db);
            importer.ImportarArquivo(jsonPath);
        }
    }
}
