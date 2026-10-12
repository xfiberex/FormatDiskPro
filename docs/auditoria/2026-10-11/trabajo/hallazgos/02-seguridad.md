# Área 02 · Seguridad · hallazgos provisionales

> **VERSIÓN PARCIAL (en curso).** Primera pasada escrita para no perder el trabajo: cubre `Services/`,
> `Core/` (construcción de comandos), el flujo de actualización, `installer.iss` y `app.manifest`.
> Faltan por revisar: `release.ps1`, `tools/capture-screenshots.ps1`, `.github/workflows/*`,
> `dependabot.yml`, `SECURITY.md`, `.mcp.json`, `.claude/`, `skills-lock.json`, `.agents/`, el resto de
> `UI/` (P/Invoke) y el barrido de secretos en el historial de git. Los hallazgos de abajo ya están
> cruzados con `ROADMAP.md` y `CONTEXT.md`.

Commit auditado: `72f2902` (v1.29.0). Revisión estática; no se levantó la app.

## Hallazgos

### P-02-01 · El instalador se verifica y se ejecuta por ruta dentro de una carpeta que el usuario sin elevar controla
**Área:** Seguridad · **Severidad:** Alto · **Confianza:** Alta · **Verificación:** estática (más ACL medida en esta máquina) · **Esfuerzo:** Medio
**Ubicación:** `src/FormatDiskPro/Services/UpdateService.cs:217-227` (carpeta), `:202-206` y `:331-350` (verificación por ruta), `:399-404` (hash, cierra el archivo), `:523-528` (`LaunchInstaller`, por ruta); `src/FormatDiskPro/UI/MainWindow.HelpAndUpdates.cs:235-242`
**Estado:** nuevo (ni `ROADMAP.md` ni `CONTEXT.md` lo recogen; `T1-09` trató el *nombre* del asset, no la carpeta ni el intervalo entre verificar y ejecutar)
**Problema:** la descarga va a `%TEMP%\FormatDiskPro_update`, que es la carpeta temporal **del usuario**, y todo el flujo trabaja con la ruta, no con un handle:

```csharp
string dir = Path.Combine(Path.GetTempPath(), "FormatDiskPro_update");   // :219
Directory.CreateDirectory(dir);                                            // :220  reutiliza la que haya
try { foreach (var old in Directory.GetFiles(dir)) File.Delete(old); }     // :223
...
await VerifyInstallerAsync(path, release.ChecksumUrl, ct);                 // :206  abre, calcula SHA-256, cierra
...
string path = await _services.Updates.DownloadAsync(rel, progress, _cts!.Token);   // HelpAndUpdates:235
_services.History.Log($"UPDATE DOWNLOADED {rel.Version}: {path}");                 // :236  E/S síncrona en %AppData%
_services.Updates.LaunchInstaller(path, silent: true);                             // :242  ShellExecute de la RUTA
```

Entre que `ComputeSha256Async` cierra el archivo y `Process.Start` lo vuelve a abrir no hay nada que impida sustituirlo, y lo que se ejecuta es lo que haya en esa ruta en ese momento, no lo que se verificó. La comprobación SHA-256 (`T2-06`, `T2-07`, `T1-08`) sigue siendo correcta, pero no queda ligada a lo que se lanza.
**Impacto:** cualquier proceso que corra con la cuenta del usuario **sin elevar** (nivel de integridad medio) puede hacer que la app, que corre elevada, ejecute otro binario como administrador y sin aviso de UAC cuando el usuario acepta una actualización. Es justo la frontera que el propio proyecto llama «la ruta con premio» (`ROADMAP.md:3670`: datos escritos sin elevación y consumidos por un proceso elevado). El intervalo no es una carrera ajustada: entre verificar y ejecutar hay una escritura síncrona en `history.log`, un archivo que ese mismo proceso también controla (ver P-02-03). Segunda consecuencia de la misma causa: si `FormatDiskPro_update` ya existe como unión de directorios hacia otra carpeta, `:223` borra como administrador los archivos de esa otra carpeta y `:275` escribe allí el instalador.
Alcance: solo afecta al caso habitual de una cuenta administradora con UAC (mismo usuario, token dividido). Si la elevación se hace con las credenciales de otra cuenta, `%TEMP%` es el de esa cuenta y el usuario estándar no llega. Microsoft no trata esa frontera como límite de seguridad, pero aquí es la única que separa el código del usuario del formateo de discos, y `T1-08` ya se clasificó como Tier 1 con una precondición más difícil (manipulación en tránsito).
CVSS v4.0: `AV:L/AC:L/AT:P/PR:L/UI:P/VC:H/VI:H/VA:H/SC:N/SI:N/SA:N`.
**Evidencia:** lectura del código citado. ACL real de la carpeta que dejó la propia app en esta máquina al actualizarse a la v1.29.0 (`icacls`, cuentas enmascaradas):

```
%TEMP%\FormatDiskPro_update                             SYSTEM:(I)(OI)(CI)(F)  Administradores:(I)(OI)(CI)(F)  <EQUIPO>\<usuario>:(I)(OI)(CI)(F)
%TEMP%\FormatDiskPro_update\FormatDiskPro-1.29.0-setup.exe   SYSTEM:(I)(F)  Administradores:(I)(F)  <EQUIPO>\<usuario>:(I)(F)
```

Propietario del `.exe`: `BUILTIN\Administradores` (lo creó el proceso elevado), pero toda la DACL es heredada y concede control total a la cuenta del usuario; sin etiqueta de integridad explícita. La sustitución en sí no se reprodujo (revisión estática).
**Solución propuesta:**
1. Descargar a una carpeta que solo puedan escribir administradores, con toda su ascendencia fuera del alcance del usuario (por ejemplo `%ProgramData%\FormatDiskPro\update`), creada con DACL explícita y protegida (SYSTEM y Administradores, sin herencia). Antes de usarla, comprobar que no es un punto de reanálisis y que su propietario es Administradores o SYSTEM; si ya existe y no lo cumple, no usarla (los usuarios pueden crear carpetas en `%ProgramData%`).
2. Ligar verificación y ejecución: abrir el archivo una sola vez con `FileShare.Read`, calcular el hash sobre ese `FileStream` y mantenerlo abierto hasta que `Process.Start` haya devuelto. Mientras el handle viva nadie puede abrirlo para escribir, borrarlo ni renombrarlo.
3. No borrar «lo que haya» en la carpeta: borrar solo el archivo concreto que se va a escribir.

```csharp
await using var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
string actual = Convert.ToHexString(await SHA256.HashDataAsync(locked, ct));
if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase)) throw ...;
LaunchInstaller(path, silent: true);   // con `locked` todavía abierto
```

**Criterio de aceptación:** `icacls` sobre la carpeta de descarga no concede escritura a la cuenta sin elevar; una prueba que intenta abrir el instalador para escritura después de verificarlo y antes de lanzarlo falla; una carpeta de descarga preexistente con otro propietario, o que sea un punto de reanálisis, se rechaza con un motivo registrado; las pruebas actuales de `UpdateServiceTests` siguen pasando.

### P-02-02 · Los enlaces y el historial se abren con `ShellExecute` desde el proceso elevado
**Área:** Seguridad · **Severidad:** Medio · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Medio
**Ubicación:** `src/FormatDiskPro/Services/UpdateService.cs:541-549` (`OpenUrl`), `src/FormatDiskPro/Services/History.cs:77-86` (`Open`); llamantes: `UI/AboutDialog.xaml.cs:33-34`, `UI/WhatsNewDialog.xaml.cs:39`, `UI/MainWindow.HelpAndUpdates.cs:163`, `UI/HistoryDialog.xaml.cs:200`
**Estado:** nuevo (`T8-03` tocó estos dos métodos por sus `catch` vacíos, no por el nivel de privilegio con que abren)
**Problema:** `Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })` y `Process.Start(new ProcessStartInfo(path) { UseShellExecute = true })` se llaman desde un proceso `requireAdministrator`. El programa que resuelve el shell (navegador predeterminado, editor asociado a `.log`) nace como hijo y hereda el token de administrador. Además, `OpenUrl` no valida el esquema: en dos de sus llamadas recibe `rel.HtmlUrl`, que sale del JSON de la API (`UpdateService.cs:123`).
**Impacto:** (1) «Apoyar el proyecto», «GitHub» y «Ver en GitHub» pueden dejar al usuario navegando (PayPal incluido) en un navegador con privilegios de administrador si no había ya una instancia abierta: todo lo que ese navegador descargue y abra, sus extensiones y cualquier fallo del propio navegador pasan a tener permisos de administrador. (2) La asociación de `https` y de `.log` se resuelve con las clases por usuario (`HKCU\Software\Classes`), que un proceso sin elevar puede cambiar: el manejador que ponga se ejecuta elevado cuando el usuario pulsa uno de esos botones (misma frontera que P-02-01, con un clic del usuario). (3) Si `html_url` llegara manipulado, `ShellExecute` aceptaría cualquier cosa ejecutable, no solo una dirección web; exige romper TLS o GitHub, así que es solo defensa en profundidad.
**Evidencia:** lectura del código. Que un hijo lanzado con `ShellExecute` hereda el token del proceso elevado es el comportamiento documentado de Windows. Qué navegadores acaban realmente elevados depende de si ya hay una instancia abierta: va a «Cosas a comprobar en ejecución».
**Solución propuesta:** abrir enlaces y archivos **sin elevación**, a través del shell del escritorio (la vía documentada es `IShellDispatch2.ShellExecute` obtenido de la vista del escritorio; la alternativa corta es lanzar `%SystemRoot%\explorer.exe "<destino>"` por ruta absoluta, que delega en el Explorador ya abierto). En `OpenUrl`, aceptar solo `https` y los hosts esperados (`github.com`, `www.paypal.me`) con `Uri.TryCreate`; lo demás, devolver `false` y enseñar la dirección, que es lo que la UI ya hace.
**Criterio de aceptación:** con la app elevada y sin navegador abierto, pulsar «GitHub» abre un navegador cuyo proceso tiene nivel de integridad medio (Process Explorer o `whoami /groups` desde ese contexto); lo mismo para el editor de «Abrir archivo»; una prueba unitaria rechaza `file:///C:/x.exe`, `C:\x.exe` y `http://` en `OpenUrl`.

### P-02-03 · El proceso elevado escribe, mueve y borra por ruta dentro de `%AppData%\FormatDiskPro`, que el usuario sin elevar controla
**Área:** Seguridad · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática (más ACL medida) · **Esfuerzo:** Medio
**Ubicación:** `src/FormatDiskPro/Services/History.cs:98-107` (crear carpeta, rotar, añadir), `:137-140` (borrar y reescribir), `:155-156` (mover con sobrescritura); `src/FormatDiskPro/Services/AppSettings.cs:205` (mover con sobrescritura), `:222-223` (crear carpeta y escribir)
**Estado:** nuevo (las pasadas anteriores siguieron el **contenido** de `settings.json` hasta los comandos, `T9-09`; ninguna miró las **operaciones de archivo** sobre esa carpeta)
**Problema:** todas esas operaciones siguen puntos de reanálisis y se hacen con privilegios de administrador sobre una carpeta en la que la cuenta sin elevar tiene control total. Si `%AppData%\FormatDiskPro` es una unión de directorios hacia otra carpeta, la app crea o sobrescribe allí `settings.json`, `history.log`, `history.1.log` y `settings.corrupt.json`.
**Impacto:** escritura y sobrescritura como administrador de archivos con esos nombres fijos en un directorio elegido por un proceso sin privilegios: sirve para pisar la configuración de otro programa que use esos nombres en una ubicación protegida o para dejar archivos donde el usuario no puede escribir. El contenido solo es controlable en parte (cadenas dentro del JSON, líneas de log). Riesgo acotado y misma precondición que P-02-01, por eso Bajo.
**Evidencia:** lectura del código y `icacls` de la carpeta real: `%AppData%\FormatDiskPro  SYSTEM:(I)(OI)(CI)(F)  Administradores:(I)(OI)(CI)(F)  <EQUIPO>\<usuario>:(I)(OI)(CI)(F)`.
**Solución propuesta:** antes de operar, comprobar que la carpeta de datos no es un punto de reanálisis (`(File.GetAttributes(dir) & FileAttributes.ReparsePoint) == 0`) y, si lo es, no escribir y registrarlo. Alternativa de fondo, coherente con «corre siempre elevada»: mover el estado a una carpeta que solo escriban administradores, migrando el contenido existente.
**Criterio de aceptación:** con `%AppData%\FormatDiskPro` sustituida por una unión hacia otra carpeta, la app no crea ni modifica ningún archivo en el destino, y lo deja dicho.

### P-02-04 · `powershell.exe` y `chkdsk.exe` se lanzan por nombre, no por ruta absoluta
**Área:** Seguridad · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/FormatDiskPro/Services/DiskService.cs:166`, `src/FormatDiskPro/Services/ReinitDrive.cs:101`, `src/FormatDiskPro/Services/FormatProcess.cs:57` (`"powershell.exe"`); `src/FormatDiskPro/Services/CheckDisk.cs:54` (`"chkdsk.exe"`)
**Estado:** nuevo (las dos revisiones de «inyección de comandos» miraron los argumentos, no la resolución del ejecutable)
**Problema:** con `UseShellExecute = false` (`ProcessRunner.cs:71`), `CreateProcess` busca un nombre sin ruta en este orden: carpeta de la aplicación, **directorio de trabajo**, `System32`, carpeta de Windows y `PATH`. `powershell.exe` no está en `System32` sino en `System32\WindowsPowerShell\v1.0`, así que solo se encuentra por `PATH`, después del directorio de trabajo. El proyecto ya lo resuelve bien para `format.com` (`FormatProcess.cs:86`: `Path.Combine(Environment.SystemDirectory, "format.com")`); los otros cuatro sitios no.
**Impacto:** si el directorio de trabajo del proceso es una carpeta escribible por terceros (la app lanzada desde una terminal situada en una unidad extraíble o en Descargas), un `powershell.exe` o `chkdsk.exe` colocado allí se ejecuta como administrador en cuanto se selecciona una unidad. Con los accesos directos del instalador el directorio de trabajo es `{app}`, que solo escriben administradores: de ahí que sea Bajo.
**Evidencia:** lectura del código; el orden de búsqueda es el documentado para `CreateProcess` con `lpApplicationName` nulo.
**Solución propuesta:** rutas absolutas en los cuatro sitios, desde un único punto:

```csharp
static readonly string PowerShellExe = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");
static readonly string ChkdskExe     = Path.Combine(Environment.SystemDirectory, "chkdsk.exe");
```

**Criterio de aceptación:** una prueba recorre los `ProcessSpec` que construyen los servicios (ya se capturan con `FakeProcessRunner`) y exige `Path.IsPathFullyQualified(spec.FileName)`.

## Hipótesis por verificar
(pendiente de completar)

## Cosas a comprobar en ejecución
- P-02-01: tras aceptar una actualización, `icacls` de la carpeta de descarga y del instalador, y si el archivo admite escritura desde un proceso sin elevar antes de que arranque el instalador.
- P-02-02: nivel de integridad del navegador y del editor abiertos desde la app, con y sin instancia previa.

## Puntos fuertes
(pendiente de completar)

## Para otras áreas
(pendiente de completar)
