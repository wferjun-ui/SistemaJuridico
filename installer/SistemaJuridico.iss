; Inno Setup script - instalação por usuário (sem UAC)
#define MyAppName "SistemaJuridico"
#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif
#ifndef PublishDir
  #define PublishDir "publish"
#endif
#ifndef OutputDir
  #define OutputDir "installer-out"
#endif

[Setup]
AppId={{C7D89882-0F11-43FB-A89A-09E59F89343E}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=SistemaJuridico
DefaultDirName={localappdata}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir={#OutputDir}
OutputBaseFilename=SistemaJuridico-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\SistemaJuridico.exe

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\SistemaJuridico.exe"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\SistemaJuridico.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na área de trabalho"; GroupDescription: "Opções adicionais:"; Flags: unchecked

[Run]
Filename: "{app}\SistemaJuridico.exe"; Description: "Executar {#MyAppName}"; Flags: nowait postinstall skipifsilent
