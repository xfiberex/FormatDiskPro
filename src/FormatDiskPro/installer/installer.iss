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
; No se ofrece elegir carpeta de instalación, y es DELIBERADO (T9-17).
;
; [InstallDelete] de abajo vacía {app} entero antes de copiar, cosa necesaria para actualizar entre
; versiones cuyo conjunto de archivos cambia. Eso es seguro mientras {app} sea una carpeta que crea y
; posee este instalador — pero con la página de destino visible, quien apuntara a una carpeta ya en uso
; (D:\Herramientas, un pendrive, la raíz de una unidad) perdía su contenido sin ningún aviso.
;
; Aquí no hay nada que elegir: la app no guarda datos en {app} (historial y preferencias viven en
; %AppData%) y su tamaño no justifica moverla de unidad. Se fija el destino y el borrado vuelve a ser
; una operación sobre terreno propio.
DisableDirPage=yes
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
; Mutex que crea la app (Program.cs). Permite a Setup detectar que está corriendo y
; cerrarla de forma fiable antes de actualizar, incluso elevada.
AppMutex=Global\FormatDiskPro.Instance

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[InstallDelete]
; Limpia la instalación previa ANTES de copiar los archivos nuevos. Imprescindible al
; actualizar entre versiones cuyo conjunto de archivos cambia (p. ej. 1.1.0 Windows Forms
; framework-dependent → 1.2.0 WinUI 3 self-contained): mezclar ambos deja la app inservible.
;
; Es seguro por DOS motivos, y hacen falta los dos: no hay datos de usuario en {app} (el historial y las
; preferencias viven en %AppData%), y desde T9-17 {app} no lo puede elegir quien instala
; (DisableDirPage=yes), así que siempre es una carpeta de este producto. Con la página de destino
; visible, este borrado alcanzaba a cualquier carpeta que se apuntara.
Type: filesandordirs; Name: "{app}\*"

[Files]
; Todo el resultado de la publicación self-contained (incluye el runtime .NET y los PRI de WinUI).
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Instalación interactiva: casilla "Ejecutar FormatDiskPro" en la página final (no en modo silencioso).
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent runascurrentuser
; Actualización silenciosa lanzada por la propia app (/AUTOUPDATE=1): relanza la app al terminar.
; Sin runascurrentuser → hereda la elevación de Setup (la app es requireAdministrator) y evita un 2.º UAC.
Filename: "{app}\{#MyAppExeName}"; Flags: nowait; Check: IsAutoUpdate

[CustomMessages]
spanish.ConfirmDeleteUserData=¿Quieres borrar también tus datos de FormatDiskPro (preferencias y el historial de operaciones)?%n%nEstán en:%n%1%n%nSi eliges No, se conservan por si vuelves a instalar la aplicación.
english.ConfirmDeleteUserData=Do you also want to delete your FormatDiskPro data (preferences and the operations history)?%n%nThey are stored in:%n%1%n%nIf you choose No, they are kept in case you install the application again.

[Code]
{ True cuando la app invoca el instalador para auto-actualizarse (UpdateService.LaunchInstaller silent). }
function IsAutoUpdate: Boolean;
begin
  Result := ExpandConstant('{param:AUTOUPDATE|0}') = '1';
end;

// Al desinstalar, ofrecer borrar los datos de usuario (T9-20).
//
// Viven en %AppData%\FormatDiskPro, FUERA del directorio de instalación, así que la desinstalación no
// los tocaba y quedaban en el disco sin que nadie lo mencionara. No es solo higiene: el historial es un
// registro fechado de qué unidades se formatearon, y quien desinstala una utilidad de disco puede muy
// razonablemente querer que eso se vaya con ella.
//
// Se PREGUNTA en vez de borrar: son los presets y el historial de la persona, y perderlos por reinstalar
// sería peor que dejarlos. En modo silencioso no se pregunta y se conservan, que es el valor por defecto
// menos destructivo.
//
// OJO con los comentarios de llaves en esta sección: `{ ... }` es un comentario de Pascal, pero se cierra
// con la PRIMERA llave de cierre que aparezca. Escribir una constante como la del directorio de la app
// dentro de un comentario así lo termina a mitad y el resto del texto pasa a compilarse como código
// ("'BEGIN' expected"). Por eso aquí se usan comentarios de línea.
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: String;
begin
  if CurUninstallStep <> usPostUninstall then
    Exit;

  DataDir := ExpandConstant('{userappdata}\FormatDiskPro');
  if not DirExists(DataDir) then
    Exit;

  if UninstallSilent then
    Exit;

  if MsgBox(FmtMessage(CustomMessage('ConfirmDeleteUserData'), [DataDir]),
            mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
    DelTree(DataDir, True, True, True);
end;
