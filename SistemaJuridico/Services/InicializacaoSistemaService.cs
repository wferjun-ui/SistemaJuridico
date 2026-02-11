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

    // Inicializa banco base
    var db = new DatabaseService(Path.GetDirectoryName(caminhoDb)!);
    db.Initialize();

    // ⭐ AQUI entra o versionamento
    var versionador = new DatabaseVersionService(db);
    versionador.GarantirAtualizacao();

    // Importação automática
    ImportarSeExistirJson(db);

    return caminhoDb;
}


        // =========================
        // SELECIONAR PASTA
        // =========================

        private string SelecionarPastaBanco()
        {
            using var dialog = new FolderBrowserDialog();

            dialog.Description =
                "Selecione a pasta onde ficará o banco do sistema";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                return Path.Combine(dialog.SelectedPath, "juridico.db");
            }

            return "";
        }

        // =========================
        // IMPORTAÇÃO AUTOMÁTICA
        // =========================

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
