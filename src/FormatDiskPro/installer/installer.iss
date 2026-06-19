; ============================================================================
;  FormatDiskPro — script de instalador para Inno Setup 6
;  Compilar con:  iscc installer.iss
;  o vía script:  build-installer.ps1  (publica self-contained y compila)
;
;  Parámetros opcionales (vía /D al invocar ISCC):
;    /DMyAppVersion=X.Y.Z     versión a estampar (por defecto: ver #define abajo)
;    /DPublishDir=<ruta>      carpeta de publicación de .NET (self-contained)
;
;  Requisitos mínimos:
;    Windows 10 v2004 (19041) o superior — requerido por Windows App SDK 1.8.
;    Arquitectura: x64.
; ============================================================================

#define MyAppName "FormatDiskPro"
#define MyAppPublisher "xfiberex"
#define MyAppURL "https://github.com/xfiberex/FormatDiskPro"
#define MyAppExeName "FormatDiskPro.exe"

#ifndef MyAppVersion
  #define MyAppVersion "1.1.0"
#endif

; Carpeta con el resultado de `dotnet publish -r win-x64 --self-contained true`.
; TFM: net10.0-windows10.0.19041.0 (WinUI 3 / Windows App SDK 1.8).
#ifndef PublishDir
  #define PublishDir "..\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish"
#endif

[Setup]
; AppId identifica el producto de forma única (no cambiar entre versiones: permite actualizar in-place).
AppId={{CEC07916-C9B5-4EA8-9102-3273384395AD}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
VersionInfoVersion={#MyAppVersion}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; La aplicación requiere privilegios de administrador para formatear unidades.
PrivilegesRequired=admin
OutputDir=Output
OutputBaseFilename={#MyAppName}-{#MyAppVersion}-setup
SetupIconFile=..\FormatDiskPro.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Windows App SDK 1.8 requiere como mínimo Windows 10 v2004 (build 19041).
MinVersion=10.0.19041
; Cierra la app si está en ejecución (clave para actualizaciones in-place).
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Todo el resultado de la publicación self-contained (incluye el runtime .NET).
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent runascurrentuser
