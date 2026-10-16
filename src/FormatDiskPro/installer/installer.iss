; ============================================================================
;  FormatDiskPro — script de instalador para Inno Setup 6
;  Compilar con:  iscc installer.iss
;  o vía script:  build-installer.ps1  (publica self-contained y compila)
;
;  Parámetros OBLIGATORIOS (vía /D al invocar ISCC; build-installer.ps1 los pasa):
;    /DMyAppVersion=X.Y.Z     versión a estampar: la misma que lleva el ejecutable publicado
;    /DPublishDir=<ruta>      carpeta de publicación de .NET (self-contained)
;
;  Requisitos mínimos:
;    Windows 10 v2004 (19041) o superior: el mínimo que declara la app (SupportedOSPlatformVersion
;    del .csproj). Si cambia allí, cambia MinVersion aquí abajo.
;    Arquitectura: x64.
; ============================================================================

#define MyAppName "FormatDiskPro"
#define MyAppPublisher "xfiberex"
#define MyAppURL "https://github.com/xfiberex/FormatDiskPro"
#define MyAppExeName "FormatDiskPro.exe"

; Sin valores por defecto, a propósito (AUD-065). Los que había (versión 1.1.0, publish dentro de bin\)
; llevaban versiones sin actualizarse: compilar este archivo a mano producía un instalador «1.1.0» con
; lo que hubiera en bin\, cuya versión no era la del ejecutable, y desde una ruta que pasa de MAX_PATH.
#ifndef MyAppVersion
  #error Falta /DMyAppVersion=X.Y.Z. Compila con build-installer.ps1, que la toma del .csproj.
#endif

; Carpeta con el resultado de `dotnet publish -r win-x64 --self-contained true`.
#ifndef PublishDir
  #error Falta /DPublishDir=<ruta>. Compila con build-installer.ps1, que publica fuera del repo (MAX_PATH).
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
; El mínimo de la app (SupportedOSPlatformVersion del .csproj): Windows 10 v2004, compilación 19041.
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
;
; DisableDirPage solo oculta la página: /DIR= por línea de comandos sigue fijando {app}. Por eso
; PrepareToInstall (en [Code]) se niega a seguir si {app} existe, no está vacía y no contiene
; FormatDiskPro.exe (AUD-062): este borrado nunca llega a una carpeta ajena.
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
spanish.ForeignTargetDir=La carpeta de destino ya existe, no está vacía y no contiene FormatDiskPro:%n%1%n%nEl instalador vacía la carpeta antes de copiar, así que no se instala ahí. Elige una carpeta vacía o la de una instalación anterior de FormatDiskPro.
english.ForeignTargetDir=The destination folder already exists, is not empty and does not contain FormatDiskPro:%n%1%n%nSetup empties the folder before copying, so it will not install there. Choose an empty folder or that of a previous FormatDiskPro installation.

[Code]
{ True cuando la app invoca el instalador para auto-actualizarse (UpdateService.LaunchInstaller silent). }
function IsAutoUpdate: Boolean;
begin
  Result := ExpandConstant('{param:AUTOUPDATE|0}') = '1';
end;

// True si la carpeta tiene al menos una entrada (archivo o subcarpeta).
function DirHasEntries(const Dir: String): Boolean;
var
  FindRec: TFindRec;
begin
  Result := False;
  if FindFirst(AddBackslash(Dir) + '*', FindRec) then
  try
    repeat
      if (FindRec.Name <> '.') and (FindRec.Name <> '..') then
      begin
        Result := True;
        Exit;
      end;
    until not FindNext(FindRec);
  finally
    FindClose(FindRec);
  end;
end;

// Se niega a instalar sobre una carpeta ajena (AUD-062).
//
// [InstallDelete] vacía la carpeta de la app entera antes de copiar. La página de destino está oculta,
// pero /DIR= por línea de comandos sigue fijando el destino, también en modo silencioso, y con él este
// borrado alcanzaba cualquier carpeta. PrepareToInstall corre antes de [InstallDelete] en los dos modos,
// y devolver texto aborta la instalación mostrándolo (o escribiéndolo en el log, en silencioso).
//
// Una carpeta que no existe, vacía, o con FormatDiskPro.exe (cualquier versión anterior, incluida la
// 1.1.0 de Windows Forms) es terreno propio.
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  AppDir: String;
begin
  Result := '';
  AppDir := ExpandConstant('{app}');
  if DirExists(AppDir) and DirHasEntries(AppDir)
     and not FileExists(AddBackslash(AppDir) + '{#MyAppExeName}') then
    Result := FmtMessage(CustomMessage('ForeignTargetDir'), [AppDir]);
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
