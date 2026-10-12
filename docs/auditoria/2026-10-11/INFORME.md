# Auditoría · FormatDiskPro · 2026-10-11

> Foto fija del proyecto en esta fecha. El plan ejecutable está en [`ROADMAP.md`](../../../ROADMAP.md)
> (Parte 3, tareas `AUD-001` a `AUD-071`); el porqué de las decisiones, en [`CONTEXT.md`](../../../CONTEXT.md);
> lo que cambió en cada versión, en [`CHANGELOG.md`](../../../CHANGELOG.md).

## 0. Alcance acordado

- **Commit auditado:** `72f29022bac5fc602f425de1e231045ce9994593` (rama `master`, árbol limpio, igual que `origin/master`) · v1.29.0, publicada el 2026-09-22.
- **Tipo:** re-auditoría. Hubo tres pasadas previas (2026-08-13, 2026-08-26 y 2026-09-18), todas cerradas, con una sola tarea abierta (`T10-02`, bloqueada a propósito). Ninguna dejó un informe en `docs/auditoria/`, así que no hay notas anteriores con las que comparar.
- **Áreas incluidas (12):** 1 Código · 2 Seguridad · 3 Rendimiento · 5 Accesibilidad · 6 UI/UX · 7 Arquitectura, datos y resiliencia · 8 QA · 9 Refactorización · 10 Redacción · 11 Documentación · 12 DevOps · 13 Legal.
- **Áreas excluidas:** 4 SEO (aplicación de escritorio, sin superficie web) · 14 IA y agentes (el producto no integra modelos; la configuración de agentes versionada en el repo se revisó dentro de 2 y 12).
- **Zonas excluidas:** `.agents/skills/` (contenido de terceros; solo su procedencia y licencia), el texto de `LICENSE`, `bin/`, `obj/` y `.codegraph/`.
- **Profundidad:** exhaustiva, sobre el repositorio completo (21.130 líneas de código en 179 archivos).
- **Contexto asumido:** utilidad gratuita para público general, distribuida solo por GitHub Releases, Windows 10/11 x64, informe en español. **La jurisdicción no se indicó:** el área 13 aplica solo el núcleo común.
- **Esquema de IDs:** se conservan los `T{tier}-{n}` y los tiers temáticos existentes; lo nuevo entra como `AUD-NNN`.

### Verificación por área

| Área | Estática | En ejecución |
|---|---|---|
| 1 Código | Sí | Sí: suite de UI, pruebas destructivas y reproducción de los servicios que lanzan procesos |
| 2 Seguridad | Sí | Parcial: SCA, gitleaks sobre todo el historial, GitHub en solo lectura, ACL reales. La sustitución del instalador no se reprodujo |
| 3 Rendimiento | Sí | Sí: arranque, memoria y coste por bloque medidos |
| 5 Accesibilidad | Sí | Parcial: árbol de UI Automation y orden de tabulación de la app real. Sin Narrador ni alto contraste del sistema |
| 6 UI/UX | Sí | Sí: galería de la app real en español (claro y oscuro) y en inglés, francés, portugués e italiano |
| 7 Arquitectura | Sí | Sí: cancelaciones y extracción de la unidad sobre la USB de pruebas |
| 8 QA | Sí | Sí: las dos suites completas, con cobertura |
| 9 Refactorización | Sí | No aplica |
| 10 Redacción | Sí | Contraste con las capturas de los cinco idiomas |
| 11 Documentación | Sí | Contraste con la línea base medida |
| 12 DevOps | Sí | Parcial: guardas de `release.ps1 -DryRun` y estado real del repositorio en GitHub |
| 13 Legal | Sí | No aplica (se cotejó la licencia real del paquete en la caché de NuGet) |

### Cómo se hizo, y dónde no fue como estaba previsto

- **Subagentes.** Se acordó repartir la revisión estática entre siete subagentes `auditor-area`. Se cortaron tres veces por el límite de sesión. Dejaron completas las áreas 12 y 13 y parciales las 1, 2, 5, 6, 9, 10 y 11; las 3, 7 y 8 no llegaron a empezar. **El coordinador terminó la revisión en la sesión principal:** leyó entero el código que toca disco, procesos, red y arranque, verificó línea a línea cada candidato de los subagentes antes de aceptarlo y revisó él las áreas 3, 7 y 8.
- **Muestreo que resulta de eso.** El coordinador no releyó enteros `MainWindow.FormatOptions.cs`, `MainWindow.Preferences.cs`, `HealthDialog`, `SaveFileDialog.cs`, `Core/PartitionPlan.cs`, `Core/ReinitPlan.cs` ni `Core/SmartInfo.cs`; los subagentes de las áreas 1, 5 y 6 declararon haberlos leído, pero no terminaron de contrastar sus notas. Las pruebas se leyeron por muestra (fixture, selección de la unidad, pruebas destructivas) más el nombre de todos los métodos.
- **Verificación independiente.** Los tres hallazgos propuestos como Altos pasaron por el subagente `verificador-hallazgos`, que confirmó dos y rebajó uno a Medio. Su veredicto está en la sección 6.
- **Hardware.** El usuario autorizó expresamente las pruebas destructivas sobre una USB de pruebas vacía (disco físico 6: `G:` FAT32 de 1 GB y `I:` exFAT de 28,3 GB). Ningún otro disco se tocó: las particiones de todos los discos se compararon antes y después y salieron idénticas. La USB quedó como se encontró y **expulsada** por la última prueba.

## 1. Resumen ejecutivo

El núcleo del proyecto está en buen estado: compila sin advertencias, las 690 unitarias y las 43 pruebas de UI pasan (incluido el ciclo destructivo completo sobre hardware real), `Core/` tiene un 97,7 % de cobertura, no hay secretos en el historial ni dependencias vulnerables, el arranque sigue en medio segundo tras el salto al Windows App SDK 2.x y las tareas de seguridad cerradas en auditorías anteriores siguen resueltas.

Lo que esta pasada encuentra no está en el camino normal, sino en sus bordes:

1. **Lo que pasa alrededor de una operación destructiva cuando algo se desvía:** cancelar, que la unidad cambie mientras se confirma, que una consulta a PowerShell no responda.
2. **La frontera entre el proceso elevado y los archivos que el usuario sin elevar controla:** la carpeta de descarga de la actualización y los enlaces que se abren con el shell.
3. **El control más importante de la ventana, que es invisible para un lector de pantalla.**
4. **Los casos raros del corte de versión** y la deriva de los documentos vivos, que han crecido hasta 265 KB y 330 KB.

Son 71 hallazgos: **0 críticos, 2 altos, 22 medios y 47 bajos.** Los cinco más graves:

| ID | Hallazgo |
|---|---|
| **AUD-001** | Alto. Cancelar un formato completo lanzado con `Format-Volume` no lo detiene: sigue hasta el final mientras la app dice «Cancelado» |
| **AUD-003** | Alto. El selector de unidad no tiene nombre accesible y todas sus opciones se anuncian con el mismo texto |
| **AUD-002** | Medio. El instalador de la actualización se verifica y después se ejecuta por ruta desde una carpeta que el usuario sin elevar controla |
| **AUD-013** | Medio. No se revalida la identidad de la unidad entre la confirmación y la operación destructiva |
| **AUD-006** | Medio. «Comprobar y reparar» se queda colgado si la unidad está en uso |

## 2. Puntuación por área

La nota parte de 10 y resta por hallazgo abierto (Alto −1,5 · Medio −0,5 · Bajo −0,1), con tope de 7 si hay algún Alto. Mide hallazgos abiertos frente a la rúbrica, no la calidad percibida. No hay nota anterior porque ninguna auditoría previa dejó informe.

| Área | Nota | Anterior | Δ | Verificación | Justificación |
|---|---:|---|---|---|---|
| 1 Código | 6,5 | – | – | estática + ejecución | 6 medios (chkdsk, expulsión, borrado seguro, consultas sin límite, error en CLIXML) y 5 bajos |
| 2 Seguridad | 8,5 | – | – | estática + ejecución parcial | 3 medios y 2 bajos; sin secretos ni dependencias vulnerables |
| 3 Rendimiento | 10,0 | – | – | ejecución (medido) | 1 bajo; arranque y memoria sin regresión |
| 5 Accesibilidad | 7,0 | – | – | estática + ejecución parcial | 1 alto (tope) y 1 medio; el resto de controles tiene nombre y el orden de foco es coherente |
| 6 UI/UX | 9,5 | – | – | estática + ejecución | 3 bajos; cinco tiers previos dejaron poco |
| 7 Arquitectura | 7,0 | – | – | estática + ejecución | 1 alto (tope), 2 medios, 4 bajos |
| 8 QA | 9,0 | – | – | ejecución + estática | 2 medios, 1 bajo |
| 9 Refactorización | 9,5 | – | – | estática | 3 bajos |
| 10 Redacción | 9,5 | – | – | estática + capturas | 6 bajos sobre 326 claves × 5 idiomas |
| 11 Documentación | 8,5 | – | – | estática | 3 medios, 2 bajos |
| 12 DevOps | 6,5 | – | – | estática + ejecución parcial | 4 medios y 15 bajos, casi todos en casos raros del corte |
| 13 Legal | 9,0 | – | – | estática (solo núcleo común) | 1 medio, 5 bajos |
| 4 SEO · 14 IA | N/E | – | – | – | No aplican |

## 3. Hallazgos

Ordenados por severidad y, dentro de cada una, por área. Las rutas son relativas a la raíz del repo; `src/…` abrevia `src/FormatDiskPro/…`. Las evidencias están en [`evidencias/`](evidencias/).

### Severidad alta

### AUD-001 · Cancelar un formato completo lanzado con `Format-Volume` no lo detiene
**Área:** Arquitectura y resiliencia · **Severidad:** Alto · **Confianza:** Confirmado · **Verificación:** ejecución, estática · **Esfuerzo:** Medio
**Ubicación:** `src/Services/FormatProcess.cs:55-67` (cancelación en `:61`) · `src/UI/MainWindow.xaml.cs:586`, `:604-618`, `:879-889` · `src/Localization/Localization.cs:453` · evidencias: `evidencias/cancelar-format-volume.txt`, `evidencias/cancelar-format-com.txt`
**Estado:** nuevo
**Problema:** al cancelar, la app mata el árbol de `powershell.exe` (`proc.Kill(entireProcessTree: true)`), pero el formato lo ejecuta el proveedor de almacenamiento de Windows, fuera de ese árbol. Van por esta vía todos los formatos rápidos (que duran segundos), los que llevan compresión y los completos de exFAT y ReFS (`useFormatCom = !quickFormat && !compress && fs is "NTFS" or "FAT32" or "FAT"`); el resto usa `format.com`. El caso que importa, el largo, exige desmarcar «Formato rápido», que viene marcado. La app muestra «Operación cancelada», escribe `FORMAT CANCELLED`, rehabilita los controles y no vuelve a mirar la unidad.
**Impacto:** quien cancela porque se ha dado cuenta de que eligió la unidad equivocada cree que ha parado a tiempo, y el formato sigue hasta el final. Con los controles activos puede además lanzar otra operación sobre el mismo disco o retirar la unidad a mitad. El aviso del propio diálogo de cancelar («la unidad puede quedar en un estado no utilizable») describe lo contrario de lo que ocurre por esta vía. exFAT es el sistema que la app recomienda para USB de más de 32 GB, donde un formato completo dura lo bastante para querer cancelarlo. No es Crítico porque el borrado ya lo había confirmado el usuario tecleando la letra.
**Evidencia:** sobre la partición de 1 GB de la USB de pruebas, con el script exacto de `FormatLogic.BuildVolumeScript` (`Format-Volume … -FileSystem exFAT -Full`) lanzado como lo lanza la app. A los 4 s se mató el árbol; el volumen siguió bloqueado y a los 50 s reapareció formateado en exFAT con la etiqueta nueva (antes era FAT32). La traza prueba que la operación siguió y terminó; no mide cuánto se escribió entre tanto. Segundo caso, misma prueba con `format.com` (formato completo NTFS): la cancelación sí lo detiene y la unidad queda **sin sistema de archivos** (`IsReady = False`), cosa que la app tampoco dice ni refleja, porque tras cancelar no recarga la lista.
**Solución propuesta:** (1) no dar por cancelado lo que no se puede cancelar: mientras el formato siga, mantener la operación abierta con un estado propio («Windows está terminando el formato; no retires la unidad») y sondear el volumen hasta que vuelva; o deshabilitar Cancelar en esa vía y decirlo antes de empezar. (2) Valorar llevar también exFAT completo por `format.com`, que sí se detiene. (3) Tras cualquier cancelación, recargar las unidades y decir en qué estado queda la que se estaba formateando.
**Criterio de aceptación:** cancelar un formato completo exFAT sobre la USB de pruebas deja la app ocupada hasta que el volumen vuelve, o el botón no está disponible; tras cancelar uno NTFS, el estado dice que la unidad quedó sin formato y la tarjeta lo refleja. Una prueba de UI con opt-in destructivo cubre ambos casos.

### AUD-003 · El selector de unidad no tiene nombre accesible y todas sus opciones se anuncian igual
**Área:** Accesibilidad · **Severidad:** Alto · **Confianza:** Confirmado · **Verificación:** ejecución, estática · **Esfuerzo:** Bajo
**Ubicación:** `src/UI/MainWindow.xaml:167-179` · `src/UI/DriveViewModel.cs:6-40` · `src/UI/MainWindow.Preferences.cs:219` (solo recibe un texto de sugerencia; los demás combos reciben nombre en `:189-190` y `:221-224`) · evidencias: `evidencias/uia-arbol-principal.txt` (línea 23), `evidencias/uia-selector-desplegado.txt`
**Estado:** nuevo. Misma causa raíz que `T7-07`, que la corrigió en el historial y en los presets y no tocó este control.
**Problema:** `DrivePicker` no tiene `Header`, `AutomationProperties.Name` ni `LabeledBy`, y sus ítems son objetos `DriveViewModel` sin `ToString()`. UI Automation expone el combo sin nombre y cada unidad como «FormatDiskPro.UI.DriveViewModel». Al cambiar la selección tampoco se anuncia nada.
**Impacto:** con un lector de pantalla, el control no dice qué es ni qué ofrece: obliga a elegir a ciegas y a comprobar el resultado fuera de él. Hay rodeo, y por eso dificulta la tarea en vez de impedirla: tras elegir, el botón primario nombra la letra («Formatear F:»), la tarjeta expone sus datos con nombre y la confirmación dice la unidad y exige teclearla. Se mantiene en Alto, en el borde inferior, porque es un fallo de nivel A en el primer paso obligatorio de todas las operaciones, que es el caso del ejemplo de la rúbrica («formulario sin etiquetas»). WCAG 2.2 · 4.1.2 Nombre, función, valor (A) y 1.3.1 (A).
**Evidencia:** volcado del árbol de UIA de la app real: `ComboBox | name=[] | id=[DrivePicker]`, único control interactivo sin nombre de los 49 elementos; al desplegarlo, todos sus ítems tienen `name=[FormatDiskPro.UI.DriveViewModel]`. El propio proyecto lo había anotado como obstáculo de pruebas en `tests/FormatDiskPro.UiTests/MainWindowActions.cs:39-47`. No se probó con un lector de pantalla.
**Solución propuesta:** `AutomationProperties.LabeledBy="{x:Bind UnitGroupLbl}"` en el combo, y nombre por ítem: `public override string ToString() => DisplayText;` en `DriveViewModel` (o `AutomationProperties.Name="{x:Bind DisplayText}"` en la plantilla).
**Criterio de aceptación:** una prueba de UI exige `Name` no vacío en `DrivePicker` y que el `Name` de cada ítem contenga su letra; el volcado de UIA no lista ningún control interactivo sin nombre.

### Severidad media

### AUD-002 · El instalador de la actualización se verifica y después se ejecuta por ruta desde una carpeta que el usuario sin elevar controla
**Área:** Seguridad · **Severidad:** Medio · **Confianza:** Alta · **Verificación:** estática (más la ACL medida en esta máquina) · **Esfuerzo:** Medio
**Ubicación:** `src/Services/UpdateService.cs:217-227` (carpeta), `:202-214` y `:331-350` (verificación), `:399-404` (hash: abre y cierra), `:523-528` (`LaunchInstaller`) · `src/UI/MainWindow.HelpAndUpdates.cs:235-243`
**Estado:** nuevo. `T1-09` trató el nombre del asset; `T1-08`, `T2-06` y `T2-07`, la verificación en sí, que sigue correcta.
**Problema:** la descarga va a `%TEMP%\FormatDiskPro_update`, que es la carpeta temporal del usuario, y todo el flujo trabaja con la ruta y no con un handle:

```csharp
string dir = Path.Combine(Path.GetTempPath(), "FormatDiskPro_update");   // UpdateService.cs:219
try { foreach (var old in Directory.GetFiles(dir)) File.Delete(old); }     // :223
await VerifyInstallerAsync(path, release.ChecksumUrl, ct);                 // :206  abre, calcula SHA-256 y cierra
string path = await _services.Updates.DownloadAsync(rel, progress, ...);   // HelpAndUpdates.cs:235
_services.History.Log($"UPDATE DOWNLOADED {rel.Version}: {path}");         // :236
_services.Updates.LaunchInstaller(path, silent: true);                     // :242  ShellExecute de la ruta
```

Entre que se cierra el archivo tras calcular el hash y que `Process.Start` lo vuelve a abrir no hay nada que impida sustituirlo. Lo que se ejecuta es lo que haya en esa ruta en ese momento.
**Impacto:** un proceso que corra con la cuenta del usuario sin elevar puede hacer que la app, que corre elevada, ejecute otro binario como administrador y sin aviso de UAC cuando el usuario acepta una actualización. Es la frontera que el propio proyecto llama «la ruta con premio» (`ROADMAP.md:3665-3671`). Segunda consecuencia de la misma causa: si `FormatDiskPro_update` ya existe como unión hacia otra carpeta, la línea 223 borra como administrador los archivos de esa carpeta. Alcance: afecta a cuentas administradoras con UAC, que es la configuración doméstica habitual; si la elevación se hace con otra cuenta, `%TEMP%` es el de esa cuenta y el usuario estándar no llega. Microsoft no trata esa frontera como límite de seguridad; el proyecto sí, y la garantía que anuncia el README («verificación del instalador antes de ejecutarlo») depende de ella.
**Por qué Medio y no Alto.** Se propuso como Alto y el verificador independiente lo rebajó: el atacante tiene que estar ya ejecutando código en la sesión de la víctima, esperar a que acepte una actualización y ganar la carrera, y lo que gana se queda dentro de la frontera de UAC del mismo usuario. Eso es riesgo acotado. `T1-08`, que el proyecto clasificó como Tier 1, cubría a un atacante externo que partía de cero.
CVSS v4.0: `AV:L/AC:L/AT:P/PR:L/UI:P/VC:H/VI:H/VA:H/SC:N/SI:N/SA:N` (el equivalente en CVSS 3.1 da 6,7).
**Evidencia:** lectura del código. ACL de la carpeta que dejó la propia app al actualizarse (`icacls`, cuentas enmascaradas): `SYSTEM:(I)(OI)(CI)(F)  Administradores:(I)(OI)(CI)(F)  <equipo>\<usuario>:(I)(OI)(CI)(F)`, toda heredada. El historial confirma la ruta. La sustitución no se reprodujo.
**Solución propuesta:** (1) descargar a una carpeta que solo escriban administradores (por ejemplo bajo `%ProgramData%\FormatDiskPro\update`), creada con DACL explícita y protegida, comprobando que no es un punto de reanálisis y que su propietario es Administradores o SYSTEM; (2) ligar verificación y ejecución: abrir el archivo una sola vez con `FileShare.Read`, calcular el hash sobre ese `FileStream` y mantenerlo abierto hasta que `Process.Start` haya devuelto; (3) borrar solo el archivo que se va a escribir, no «lo que haya» en la carpeta.
**Criterio de aceptación:** `icacls` sobre la carpeta de descarga no concede escritura a la cuenta sin elevar; una prueba que intenta abrir el instalador para escritura después de verificarlo y antes de lanzarlo falla; una carpeta preexistente con otro propietario o que sea un punto de reanálisis se rechaza con un motivo registrado; `UpdateServiceTests` sigue pasando.

### AUD-004 · Los enlaces y el historial se abren con el shell desde el proceso elevado, y `OpenUrl` no valida lo que abre
**Área:** Seguridad · **Severidad:** Medio · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Medio
**Ubicación:** `src/Services/UpdateService.cs:541-549` (`OpenUrl`) · `src/Services/History.cs:77-86` (`Open`) · llamantes: `src/UI/AboutDialog.xaml.cs:33-34`, `src/UI/WhatsNewDialog.xaml.cs:39`, `src/UI/MainWindow.HelpAndUpdates.cs:163`, `src/UI/HistoryDialog.xaml.cs:200`
**Estado:** nuevo. `T8-03` tocó estos métodos por sus `catch` vacíos, no por el privilegio con que abren.
**Problema:** `Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })` desde un proceso `requireAdministrator`: el programa que resuelve el shell (navegador, editor asociado a `.log`) hereda el token de administrador. `OpenUrl` recibe además `rel.HtmlUrl`, que sale del JSON de la API, sin comprobar el esquema.
**Impacto:** «Apoyar el proyecto», «GitHub» y «Ver en GitHub» pueden dejar al usuario navegando con privilegios de administrador si no había un navegador ya abierto. La asociación de `https` y de `.log` se resuelve con las clases del usuario (`HKCU`), que un proceso sin elevar puede cambiar: el manejador que ponga se ejecuta elevado cuando el usuario pulsa uno de esos botones. Misma frontera que AUD-002, con un clic.
**Evidencia:** lectura del código. Qué navegadores acaban elevados depende de cada uno y no se comprobó.
**Solución propuesta:** abrir enlaces y archivos sin elevación a través del shell del escritorio (`IShellDispatch2.ShellExecute` desde la vista del escritorio, o `%SystemRoot%\explorer.exe "<destino>"` por ruta absoluta). En `OpenUrl`, aceptar solo `https` y los hosts esperados con `Uri.TryCreate`.
**Criterio de aceptación:** con la app elevada y sin navegador abierto, «GitHub» abre un navegador con integridad media; una prueba unitaria rechaza `file:///C:/x.exe`, `C:\x.exe` y `http://`.

### AUD-005 · El canal privado de vulnerabilidades que anuncia `SECURITY.md` está desactivado en GitHub
**Área:** Seguridad · **Severidad:** Medio · **Confianza:** Confirmado · **Verificación:** ejecución (API de GitHub, solo lectura) · **Esfuerzo:** Bajo
**Ubicación:** `.github/SECURITY.md:20-22` · configuración del repositorio
**Estado:** nuevo. «Plantillas de GitHub» figura como revisado sin hallazgos (2026-08-26), pero se revisó el texto, no la configuración.
**Problema:** la política manda usar «Security → Report a vulnerability» y da el enlace directo. `gh api repos/xfiberex/FormatDiskPro/private-vulnerability-reporting` devuelve `{"enabled":false}`.
**Impacto:** quien encuentre un fallo en una herramienta que corre elevada y se auto-actualiza no tiene el canal privado prometido; le queda abrir un issue público, que es lo que la política pide evitar.
**Evidencia:** respuesta de la API el 2026-10-11.
**Solución propuesta:** activar *Private vulnerability reporting* en *Settings → Code security*. De paso, activar las alertas de Dependabot (ver AUD-066).
**Criterio de aceptación:** la API devuelve `enabled: true` y el enlace de `SECURITY.md` abre el formulario desde una cuenta sin permisos.

### AUD-006 · «Comprobar y reparar» se queda colgado si la unidad está en uso
**Área:** Código · **Severidad:** Medio · **Confianza:** Confirmado · **Verificación:** ejecución (reproducción del servicio), estática · **Esfuerzo:** Bajo
**Ubicación:** `src/Services/CheckDisk.cs:60-67` · evidencia: `evidencias/chkdsk-reparar-unidad-en-uso.txt`
**Estado:** nuevo
**Problema:** el servicio escribe una sola «N» en la entrada estándar de `chkdsk` y no la cierra. Con el volumen en uso, `chkdsk /f` hace **dos** preguntas: si se fuerza el desmontaje y, al decir que no, si se programa la comprobación para el próximo reinicio. La segunda se queda esperando una respuesta que nunca llega.
**Impacto:** basta un archivo abierto en la unidad para que la reparación no termine nunca: barra a cero, sin mensaje, hasta que el usuario cancela. `FormatProcess` ya resuelve el mismo problema cerrando la entrada (`FormatProcess.cs:99`).
**Evidencia:** `chkdsk G: /f` sobre la USB de pruebas con un archivo abierto, lanzado con los mismos argumentos y la misma «N»: no terminó en 30 s; al cerrar la entrada salió con código 3. La salida muestra las dos preguntas.
**Solución propuesta:** cerrar `StandardInput` después de escribir la respuesta (el fin de entrada hace que `chkdsk` salga), o no escribir nada y cerrarla sin más. Con el código 3 la app ya dice «no se pudo comprobar (¿está en uso?)».
**Criterio de aceptación:** una prueba con `FakeProcessRunner` comprueba que la entrada queda cerrada; sobre la USB con un archivo abierto, «Comprobar y reparar» termina en segundos con el mensaje de unidad en uso.

### AUD-007 · Un `chkdsk` de solo lectura que encuentra errores se presenta como «no se pudo comprobar»
**Área:** Código · **Severidad:** Medio · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/Services/CheckDisk.cs:27-33` · `src/UI/MainWindow.Operations.cs:283`, `:293-304` · `src/Localization/Localization.cs:362` · `tests/FormatDiskPro.Tests/CheckDiskTests.cs:19`
**Estado:** nuevo
**Problema:** `Interpret` trata el código 3 como `Failed` siempre. Según la tabla de códigos de salida de `chkdsk` de Microsoft, 3 significa «no se pudo comprobar, no se pudieron corregir los errores, **o no se corrigieron porque no se indicó /f**». En el modo «Solo comprobar» ese es el código con el que se informa de que hay errores. La salida de `chkdsk`, que distingue los casos, se descarta (`var (code, _) = …`). La prueba actual fija el comportamiento (`[InlineData(3, false, CheckResult.Failed)]`).
**Impacto:** a quien tiene un sistema de archivos dañado se le dice «No se pudo comprobar la unidad (¿está en uso?)» en vez de «Se encontraron errores. Usa Comprobar y reparar». El diagnóstico falla justo cuando hay algo que diagnosticar.
**Evidencia:** lectura del código y del texto; no se reprodujo con un volumen dañado.
**Solución propuesta:** en solo lectura, tratar el 3 como «errores encontrados o no se pudo comprobar» y decidir con la salida, que ya se captura; ofrecer la salida completa en un desplegable del diálogo de resultado.
**Criterio de aceptación:** la prueba de `Interpret` distingue los dos modos para el código 3, y el diálogo de resultado permite leer la salida de `chkdsk`.

### AUD-008 · «Expulsar» da la unidad por expulsada sin comprobarlo
**Área:** Código · **Severidad:** Medio · **Confianza:** Confirmado · **Verificación:** ejecución (reproducción del servicio), estática · **Esfuerzo:** Bajo
**Ubicación:** `src/Services/DiskService.cs:145-156` · `src/UI/MainWindow.Operations.cs:120-131` · evidencia: `evidencias/expulsar.txt`
**Estado:** nuevo
**Problema:** `EjectAsync` devuelve `true` si PowerShell sale con código 0, y `InvokeVerb('Eject')` no falla aunque Windows no pueda expulsar. La app escribe «Unidad expulsada» y registra `EJECT`.
**Impacto:** con un archivo abierto, la app dice que es seguro retirar la unidad cuando no lo es. Es el mensaje del que depende no perder datos.
**Evidencia:** el script de `DiskService.cs:149-152` sobre la USB de pruebas con un archivo abierto: salió con 0 en 0,5 s y la unidad seguía montada 6 s después. Sin nada abierto, la expulsión sí funciona (volúmenes desmontados a los 3 s).
**Solución propuesta:** tras invocar el verbo, sondear unos segundos si el volumen sigue montado (`DriveInfo.IsReady` o `Get-Volume`) y devolver `false` si sigue; el texto de fallo ya existe (`eject.fail`).
**Criterio de aceptación:** con un archivo abierto en la USB, «Expulsar» muestra el aviso de que no se pudo y no registra `EJECT`.

### AUD-009 · El borrado seguro puede no ejecutarse y aun así registrarse y anunciarse como hecho
**Área:** Código · **Severidad:** Medio · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/UI/MainWindow.xaml.cs:641`, `:662-664` · `src/Services/SecureWipe.cs:85`, `:94`
**Estado:** nuevo
**Problema:** `SecureWipe.RunAsync` devuelve `-1` si la unidad no está lista, y quien llama descarta el valor: `await _services.Wipe.RunAsync(...)`. El flujo sigue hasta `FORMAT OK … wipe=True passes=N` y el diálogo de éxito.
**Impacto:** quien pidió «Borrado seguro» recibe la confirmación y el registro de auditoría de un borrado que no se hizo, y los datos anteriores siguen siendo recuperables. Ocurre si el volumen no está accesible justo después de formatear (unidad retirada, montaje fallido).
**Evidencia:** lectura del código; ninguna prueba ejerce este camino (ver AUD-016).
**Solución propuesta:** tratar cualquier valor distinto de 0 como fallo (estado de error, `WIPE FAIL` en el historial, diálogo propio), o hacer que el servicio lance.
**Criterio de aceptación:** con un `ISecureWipe` falso que devuelve `-1`, la operación termina en error y el historial no contiene `wipe=True`.

### AUD-010 · Las consultas a PowerShell de `DiskService` no tienen límite de tiempo ni cancelación, y redirigen una salida de error que nadie lee
**Área:** Código · **Severidad:** Medio · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/Services/DiskService.cs:160-194`
**Estado:** nuevo
**Problema:** `RunCapturedAsync` espera con `WaitForExitAsync()` sin token ni plazo, y pide redirigir `stderr` (`capture: true`) pero solo lee `stdout`. Si PowerShell no responde, o escribe más de lo que cabe en la tubería de error, la espera no termina.
**Impacto:** una unidad averiada que deja colgadas las consultas de almacenamiento, que es el tipo de unidad para el que existe esta herramienta, deja sin efecto y sin mensaje lo que dependa de la consulta: «Formatear» espera a `IsDiskReadOnlyAsync` antes de abrir la confirmación, y «Reinicializar», a `GetDiskNumberAsync`. La ventana sigue respondiendo, pero el botón pulsado no hace nada.
**Evidencia:** lectura del código. La salida de error de PowerShell con `-EncodedCommand` es CLIXML y ocupa 1,7 KB para un solo error (ver AUD-011); la tubería admite 4 KB.
**Solución propuesta:** un plazo por consulta (10 a 15 s) con `CancellationTokenSource` que mate el proceso, y leer o no redirigir `stderr`. Que el fallo por plazo llegue a la interfaz como «no se pudo consultar la unidad».
**Criterio de aceptación:** con un `IProcessRunner` falso que no termina, cada método de `DiskService` devuelve su valor de «no se sabe» dentro del plazo.

### AUD-011 · El diálogo de error de un formateo fallido enseña el XML interno de PowerShell
**Área:** Código · **Severidad:** Medio · **Confianza:** Confirmado · **Verificación:** ejecución (reproducción del servicio), estática · **Esfuerzo:** Bajo
**Ubicación:** `src/UI/MainWindow.xaml.cs:604-607`, `:673` · `src/Services/FormatProcess.cs:63-67` · `src/Services/ReinitDrive.cs:147` · evidencia: `evidencias/format-volume-fallido-stderr.txt`
**Estado:** nuevo
**Problema:** con `-EncodedCommand`, PowerShell 5.1 escribe los errores en `stderr` serializados como CLIXML. La app usa ese texto tal cual como cuerpo del diálogo (`output = string.IsNullOrWhiteSpace(se) ? so : se;`), y `ReinitDrive` lo guarda como detalle en el historial.
**Impacto:** cuando falla un formato rápido, que es la vía por defecto, el usuario lee `#< CLIXML <Objs Version="1.1.0.1" xmlns=…><Obj S="progress"…` seguido del mensaje real troceado con `_x000D__x000A_`. Es el momento en que más falta hace un texto claro.
**Evidencia:** el script de `BuildVolumeScript` contra una letra inexistente, lanzado como lo lanza la app: 1.682 caracteres de CLIXML, con dos registros de progreso por delante del error.
**Solución propuesta:** extraer el texto de los nodos `<S S="Error">` (deshaciendo los escapes `_xHHHH_`) antes de mostrarlo, en una función pura de `Core/` con su prueba; o añadir al script un `try/catch` que escriba `$_.Exception.Message` por `stdout`.
**Criterio de aceptación:** una prueba pasa el contenido de la evidencia a la función y obtiene «Format-Volume : No se encontraron objetos MSFT_Volume…» sin etiquetas.

### AUD-012 · La guarda «no reinicializar el disco de Windows» falla abierta si no se puede leer el disco del sistema
**Área:** Arquitectura y resiliencia · **Severidad:** Medio · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/UI/MainWindow.Operations.cs:347-354`
**Estado:** nuevo
**Problema:** `if (targetDisk is null || (sysDisk is not null && targetDisk == sysDisk))`. Si la consulta del disco del sistema falla (`sysDisk == null`), la comparación se salta y el flujo sigue hacia `Clear-Disk`.
**Impacto:** el caso que la guarda existe para impedir, una partición extraíble en el mismo disco físico que Windows (Windows instalado en un medio extraíble), deja de estar cubierto exactamente cuando las consultas de almacenamiento fallan. Es muy poco probable y el daño sería total; es posible que Windows rechace por su cuenta limpiar el disco de arranque, pero no se pudo comprobar sin riesgo.
**Evidencia:** lectura del código. Por la rúbrica: probabilidad muy baja, impacto máximo.
**Solución propuesta:** rechazar también cuando `sysDisk is null`, con un mensaje propio («no se pudo comprobar que la unidad no comparte disco con Windows»). Extraer la decisión a una función pura (ver AUD-015).
**Criterio de aceptación:** una prueba unitaria de la función extraída devuelve «rechazar» para `(target: 3, system: null)`.

### AUD-013 · No se revalida la identidad de la unidad entre la confirmación y la operación destructiva
**Área:** Arquitectura y resiliencia · **Severidad:** Medio · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Medio
**Ubicación:** `src/UI/MainWindow.xaml.cs:443`, `:471-478`, `:511-517`, `:249` · `src/UI/MainWindow.Operations.cs:330-354`, `:438-452` · `src/Services/ReinitDrive.cs:84` · `src/Core/FormatLogic.cs:53`
**Estado:** nuevo
**Problema:** todas las comprobaciones (unidad lista, no es la de sistema, es extraíble, no comparte disco con Windows) se hacen antes de abrir el diálogo de confirmación. Después, la operación se lanza solo con la **letra** capturada: `RunFormatAsync(driveItem.Letter, …)`, y en Reinicializar el script vuelve a resolver el disco por letra (`Get-Partition -DriveLetter {letter} | Get-Disk`). Con el diálogo abierto la app no está ocupada, así que la lista sí se refresca por detrás, pero el diálogo y la operación siguen con lo capturado.
**Impacto:** si entre abrir la confirmación y pulsar el botón la letra pasa a otro dispositivo (un concentrador USB que reconecta, una unidad que se retira y otra que toma su letra), se formatea o se reinicializa lo que haya ahora en esa letra con los datos de la anterior en pantalla. En Reinicializar, además, sin volver a comprobar que es extraíble. Probabilidad baja, daño irreversible sobre el disco equivocado.
**Evidencia:** lectura del flujo. No se reprodujo (requeriría intercambiar letras con el diálogo abierto).
**Solución propuesta:** capturar una identidad estable al abrir la confirmación (número de disco, `UniqueId` o número de serie y tamaño) y comprobarla justo antes de ejecutar; en Reinicializar, pasar al script el número de disco ya verificado y que el propio script aborte si `BusType`, `IsSystem`, `IsBoot` o el tamaño no coinciden. Si la lista cambia con una confirmación abierta, cerrarla.
**Criterio de aceptación:** con una identidad distinta de la capturada, la operación no se lanza y lo dice; una prueba unitaria cubre la comparación.

### AUD-014 · Dos regiones activas declaradas nunca avisan al lector de pantalla, y un error no se anuncia
**Área:** Accesibilidad · **Severidad:** Medio · **Confianza:** Media · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/UI/HistoryDialog.xaml:32-33` y `HistoryDialog.xaml.cs:104-107` · `src/UI/PresetsDialog.xaml:22-23` y `PresetsDialog.xaml.cs:199-203` · estados sin anuncio: `src/UI/MainWindow.Operations.cs:124`, `:174`, `src/UI/MainWindow.HelpAndUpdates.cs:128`
**Estado:** nuevo
**Problema:** el recuento del historial y el error de nombre de preset llevan `AutomationProperties.LiveSetting`, pero nada dispara `LiveRegionChanged`. El propio código lo explica para el único sitio donde sí se hace (`MainWindow.xaml.cs:557`: «el evento es lo que dispara la lectura»). Tampoco se anuncian los cambios de estado de «Expulsar», «Quitar protección» y «Buscando actualizaciones».
**Impacto:** quien usa lector de pantalla no se entera de cuántos resultados deja un filtro ni de que el nombre de un preset se rechazó. WCAG 2.2 · 4.1.3 Mensajes de estado (AA).
**Evidencia:** `RaiseLiveRegionChanged` solo se llama desde `MainWindow.xaml.cs:561`. Confianza Media porque no se escucharon los eventos con la app en marcha; lo confirmaría Accessibility Insights en modo de eventos.
**Solución propuesta:** mover `RaiseLiveRegionChanged` a un ayudante compartido y llamarlo al cambiar `CountText` y `ErrorText`; usar `SetStatusAndAnnounce` en los tres estados.
**Criterio de aceptación:** Accessibility Insights registra un evento `LiveRegionChanged` al filtrar el historial y al rechazar un nombre de preset.

### AUD-015 · Las guardas que deciden si una operación destructiva procede no tienen ninguna prueba que ejerza su rechazo
**Área:** QA · **Severidad:** Medio · **Confianza:** Alta · **Verificación:** estática, ejecución (cobertura) · **Esfuerzo:** Medio
**Ubicación:** `src/UI/MainWindow.xaml.cs:448-462`, `:931-935` · `src/UI/MainWindow.Operations.cs:330-354` · `tests/FormatDiskPro.UiTests/MainWindowTests.cs:45`
**Estado:** nuevo
**Problema:** el rechazo de formatear el disco de sistema y los tres filtros de Reinicializar viven dentro de manejadores de la ventana, con 0 % de cobertura unitaria (1.988 líneas de `UI/`). Tienen prueba la comparación de letras (`DriveLetterTests`) y el estado del menú Herramientas sobre el disco de sistema; no la tiene ningún rechazo. Por eso AUD-012 pudo pasar inadvertido.
**Impacto:** la promesa central del producto («el disco de sistema siempre protegido») depende de código que ninguna prueba ejercita en su rama negativa. No llega a Alto por la rúbrica porque la ruta no está sin ninguna prueba: la pieza de comparación y un caso de UI sí existen.
**Evidencia:** cobertura medida y lista de los 43 métodos de prueba de UI.
**Solución propuesta:** extraer a `Core/` una función pura por operación (`OperationGuard.CanFormat(...)`, `CanReinit(...)`) que reciba hechos (tipo de unidad, letra de sistema, números de disco) y devuelva un motivo tipado, como ya hace `PartitionPlan.Validate`; cubrirla con pruebas, incluidos los nulos.
**Criterio de aceptación:** `Core/` contiene las decisiones de rechazo con cobertura del 100 % de sus ramas, y los manejadores solo traducen el motivo a un diálogo.

### AUD-016 · El borrado seguro no tiene ninguna prueba automatizada
**Área:** QA · **Severidad:** Medio · **Confianza:** Alta · **Verificación:** ejecución (cobertura), estática · **Esfuerzo:** Medio
**Ubicación:** `src/Services/SecureWipe.cs:80-146` · `tests/FormatDiskPro.Tests/SecureWipeTests.cs:9` · `tests/FormatDiskPro.UiTests/DestructiveLifecycleTests.cs:233`
**Estado:** nuevo
**Problema:** `SecureWipe.RunAsync` tiene un 15 % de cobertura (6 de 41 líneas): las unitarias cubren solo las funciones puras y declaran que el bucle «es E/S y no se cubre», y el ciclo destructivo de UI desmarca la casilla. `CapacityVerifier` resolvió lo mismo con una costura (`RunInAsync`) y llega al 98 %.
**Impacto:** el patrón que se escribe en cada pasada, la limpieza al cancelar y el valor de retorno (AUD-009) no los vigila nada. Por la rúbrica sería Alto si fuera una ruta sin ninguna prueba y crítica; se deja en Medio porque es una función opcional añadida al formateo y sus partes puras sí están cubiertas.
**Evidencia:** cobertura por archivo (`trabajo/hallazgos/03-07-08-coordinador.md`).
**Solución propuesta:** la misma costura que `CapacityVerifier`: un `RunInAsync(dir, bytes, passes, …)` probado sobre una carpeta temporal (contenido de la última pasada, borrado del directorio, cancelación a mitad), y activar el borrado en una variante del ciclo destructivo de UI.
**Criterio de aceptación:** `SecureWipe.cs` supera el 90 % de cobertura y existe una prueba que falla si la última pasada no es aleatoria.

### AUD-017 · El requisito de sistema documentado contradice al instalador, y varios archivos siguen citando el Windows App SDK 1.8
**Área:** Documentación · **Severidad:** Medio · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `README.md:94`, `:190` · `.github/CONTRIBUTING.md:13` · `src/installer/installer.iss:11`, `:25`, `:66-67` · `src/FormatDiskPro.csproj:5`, `:43`
**Estado:** nuevo (el salto de versión es de `T13-20`, que no tocó estos textos)
**Problema:** el README y la guía de contribución prometen «Windows 10 1809+»; el instalador exige la 2004 (`MinVersion=10.0.19041`) y el `.csproj` no declara mínimo propio. El comentario que justifica ese mínimo y la tabla de arquitectura del README citan el SDK 1.8; el proyecto usa el 2.5.1.
**Impacto:** quien tenga Windows 10 1809–1909 y lea el README descarga 62 MB para encontrarse el rechazo del instalador. No hay un único sitio que diga cuál es el mínimo real ni por qué.
**Evidencia:** fragmentos citados.
**Solución propuesta:** decidir el mínimo (de hecho, a 2026-10-11, es 19041), declararlo en el `.csproj` (`SupportedOSPlatformVersion`) y alinear README, CONTRIBUTING, `MinVersion` y los tres comentarios.
**Criterio de aceptación:** una búsqueda de «1809», «19041» y «1.8» en el repo devuelve un único valor coherente.

### AUD-018 · `CONTEXT.md` §2 y §3 vuelven a estar desfasados y se contradicen
**Área:** Documentación · **Severidad:** Medio · **Confianza:** Alta · **Verificación:** estática, línea base · **Esfuerzo:** Bajo
**Ubicación:** `CONTEXT.md:44-63`, `:80-92`, `:158`, `:162`, `:164-165`
**Estado:** regresión de `T9-14` (estado duplicado que caduca) y de `T9-15` (árbol de archivos)
**Problema:** §3 dice «626 / 626 unitarias» cuando la línea base del 2026-10-11 mide 690 y su propio desglose no suma esa cifra; «Tiers abiertos: Ninguno» junto a «Tareas abiertas: 1 … Tier 10, abierto»; la frase «`T10-02`, bloqueada a propósito» está escrita dos veces seguidas; y `T2-10` figura como descartada cuando el CI existe. El árbol de §2 omite `Core/PlanRejection.cs`, `UI/HighContrast.cs` y `UI/SaveFileDialog.cs`, y afirma que ninguna parcial de `MainWindow` pasa de 800 líneas (`MainWindow.xaml.cs` tiene 986).
**Impacto:** es el archivo que `CLAUDE.md` manda leer al empezar cada sesión; lo que diga mal se hereda. Es la tercera vez que ocurre por la misma causa: cifras copiadas a mano en prosa.
**Evidencia:** línea base del 2026-10-11 (`trabajo/linea-base.md`).
**Solución propuesta:** dejar en §3 solo lo que no se pueda obtener con un comando y enlazar el resto; para las cifras, una línea con el comando que las da. Regenerar el árbol de §2 a partir de `git ls-files`.
**Criterio de aceptación:** ninguna cifra de §3 contradice la salida de `dotnet test`, y el árbol de §2 coincide con `git ls-files src/`.

### AUD-019 · `CONTEXT.md` y `ROADMAP.md` han crecido hasta dejar de cumplir su función
**Área:** Documentación · **Severidad:** Medio · **Confianza:** Alta · **Verificación:** estática (medido) · **Esfuerzo:** Medio
**Ubicación:** `CONTEXT.md` (3.387 líneas, 264.920 bytes; el registro de cambios ocupa 2.994) · `ROADMAP.md` (3.680 líneas, 330.663 bytes; `:350-369`) · `.claude/CLAUDE.md`
**Estado:** nuevo
**Problema:** `CLAUDE.md` ordena leer `CONTEXT.md` al iniciar cada sesión, y el archivo pesa 265 KB: casi todo es el registro razonado de cada tarea cerrada. El `ROADMAP.md` conserva enteras más de cien tareas cerradas, sus tiers 11 a 13 están en orden inverso, y su índice no incluye el Tier 13 y suma 108 tareas de 128. `CONTEXT.md` no tiene sección de falsos positivos verificados, así que cada auditoría los redescubre.
**Impacto:** el contexto «vivo» ya no cabe en una lectura; lo importante (§4, decisiones) queda enterrado bajo el historial; y en esta misma auditoría los subagentes consumieron buena parte de su presupuesto buscando en esos dos archivos.
**Evidencia:** `wc -l -c` y lectura del índice.
**Solución propuesta:** mover el registro de cambios de `CONTEXT.md` a `docs/contexto-historico.md`, dejando las últimas entradas y un enlace; mover los tiers cerrados del `ROADMAP.md` a `docs/roadmap-historico.md`, dejando índice, abiertas y decisiones; regenerar el índice. Esta auditoría ya añade la sección de falsos positivos.
**Criterio de aceptación:** `CONTEXT.md` baja de 500 líneas sin perder §1 a §7, el índice del `ROADMAP.md` cuadra con sus tiers y ningún enlace interno queda roto.

### AUD-020 · El corte no comprueba en qué rama está ni si va por detrás de `origin`
**Área:** DevOps · **Severidad:** Medio · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `release.ps1:421-426`, `:745-749`, `:805`
**Estado:** nuevo. Relacionado con `T9-02`, que no cubre este paso.
**Problema:** el script acepta cualquier rama y nunca hace `git fetch` ni compara con `origin`. Si `origin` tiene commits que el clon local no tiene, todo el corte se ejecuta (pruebas, cambio de versión, instalador, commit y tag) y falla al subir la rama, con el único mensaje de la fase de publicación que no dice cómo seguir.
**Impacto:** corte a medias con recuperación no guiada. Desde que el repositorio es público hay fusiones en remoto (el 2026-10-11 hay un PR de Dependabot abierto), así que el caso ya no es raro. Si después se usa `-ResumeRelease`, se publica un release cuyo tag no está en `master`.
**Evidencia:** en todo el script no aparece `fetch`, `rev-list` ni `merge-base`.
**Solución propuesta:** al principio de las validaciones, `git fetch origin $branch`, abortar si `git rev-list --count HEAD..origin/$branch` es mayor que 0 y avisar si la rama no es `master`. En `-ResumeRelease`, comprobar que el tag está contenido en `origin/$branch`.
**Criterio de aceptación:** con un commit de más en `origin/master`, `release.ps1 -DryRun` aborta antes de las pruebas diciendo cuántos commits faltan.

### AUD-021 · `-ResumeRelease` publica el instalador que haya en la carpeta de salida sin comprobar que es el del tag
**Área:** DevOps · **Severidad:** Medio · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `release.ps1:473-474`, `:731-742`, `:771-781`, `:847`
**Estado:** nuevo. Es la premisa de `T9-01` reapareciendo por el camino que añadió `T9-02`; `T9-01` sigue resuelta en el camino normal.
**Problema:** al retomar, el script solo exige que existan el `.exe` y su `.sha256`, y declara que el estado del árbol da igual. Nada ata ese archivo al commit etiquetado, y en ninguno de los dos caminos se recalcula el hash para compararlo con el contenido del `.sha256` antes de subirlo.
**Impacto:** si entre el corte fallido y el reintento se vuelve a compilar el instalador, se publica un binario que no sale del commit etiquetado y nada avisa. Si el par se desempareja, se publica un release que la app descarga, no verifica y borra: la auto-actualización queda rota para todos hasta publicar otra versión.
**Evidencia:** las líneas citadas solo contienen `Test-Path` y lectura de tamaño.
**Solución propuesta:** antes de `gh release create`, recalcular `Get-FileHash` y abortar si no coincide con el `.sha256`; anotar el hash en el mensaje del tag y exigir en `-ResumeRelease` que el instalador presente sea ese.
**Criterio de aceptación:** con el `.exe` sustituido tras etiquetar, `-ResumeRelease` aborta nombrando la discrepancia.

### AUD-022 · La compilación solo es reproducible para el Windows App SDK
**Área:** DevOps · **Severidad:** Medio · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Medio
**Ubicación:** raíz del repo (no existen `global.json`, `nuget.config` ni `packages.lock.json`) · `.github/workflows/ci.yml:49`, `:62-64` · `src/installer/build-installer.ps1:183-190`
**Estado:** nuevo. `#44` y `CONTEXT.md` §4 solo fijan `Microsoft.WindowsAppSDK`.
**Problema:** flotan tres piezas a las que aplica el argumento que el proyecto ya escribió para no dejar flotar el SDK de Windows: la versión del SDK de .NET (`10.0.x` en CI, la instalada en local), el parche del runtime que se empaqueta en cada instalador y lo que restaura NuGet (el metapaquete 2.5.1 arrastra diez subpaquetes, cada uno con su versión).
**Impacto:** no se puede reconstruir el instalador de una versión publicada ni decir qué runtime lleva sin abrirlo. Los parches de seguridad del runtime solo llegan a los usuarios con una versión nueva de una app que corre elevada, y nada registra cuál lleva cada una. Con `-warnaserror`, una banda nueva del SDK puede poner en rojo el CI sin que el repo haya cambiado.
**Evidencia:** `git ls-files` y el `.nuspec` del paquete en la caché de NuGet.
**Solución propuesta:** `global.json` con `rollForward: latestPatch`; `RestorePackagesWithLockFile` con `--locked-mode` en CI y en el corte; `nuget.config` con solo nuget.org; y que el corte deje escrita la versión del runtime empaquetado.
**Criterio de aceptación:** dos compilaciones del mismo commit usan el mismo SDK y los mismos paquetes, y cada release declara el runtime que contiene.

### AUD-023 · La configuración de agentes de IA está versionada en un repositorio público sin fijar ni vigilar
**Área:** DevOps · **Severidad:** Medio · **Confianza:** Media · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `.mcp.json:3-10` · `.claude/settings.json:2-13` · `.claude/CLAUDE.md` · `.agents/skills/*/SKILL.md` · `skills-lock.json` · `.github/CONTRIBUTING.md:47-48` · `.github/PULL_REQUEST_TEMPLATE.md:28`
**Estado:** nuevo
**Problema:** el repo versiona cuatro cosas que un agente de código ejecuta u obedece al abrir el proyecto: un servidor MCP que arranca `codegraph` resuelto por `PATH`, ocho herramientas preautorizadas, la orden de leer los `SKILL.md` y nueve `SKILL.md` de un tercero cuyo hash, guardado en `skills-lock.json`, nada comprueba. La guía de contribución y la plantilla de PR señalan como delicado solo `.github/workflows/`.
**Impacto:** un PR externo que cambie esos archivos modifica lo que el agente del mantenedor ejecuta o cree al revisar esa rama, en una terminal que con frecuencia está elevada y en la máquina que guarda la credencial con la que se publica el instalador.
**Evidencia:** archivos citados. No se encontró texto malicioso en ellos. Confianza Media: no se comprobó si el cliente vuelve a pedir aprobación al cambiar el comando de un servidor ya aprobado.
**Solución propuesta:** añadir esas rutas a la lista de «revisar con cuidado» de la guía y de la plantilla; un paso de CI que compare cada `SKILL.md` con su hash bloqueado; documentar qué es `codegraph` y de dónde se instala.
**Criterio de aceptación:** un PR que toque esas rutas queda señalado y el CI detecta un `SKILL.md` que no corresponde a su hash.

### AUD-024 · Los avisos de terceros declaran licencias que no son las de lo que se redistribuye · *requiere revisión legal*
**Área:** Legal · **Severidad:** Medio · **Confianza:** Alta para los hechos, Media para la consecuencia · **Verificación:** estática (licencia cotejada en la caché de NuGet) · **Esfuerzo:** Medio
**Ubicación:** `THIRD-PARTY-NOTICES.txt:23`, `:30-40`, `:50-54` · `src/installer/installer.iss:95`
**Estado:** ya conocido como «revisado, sin hallazgos» (`T9-19` y `ROADMAP.md:3650-3652`); se reabre por un hecho nuevo: el salto al SDK 2.x (`T13-20`) y la licencia real del paquete.
**Problema:** el aviso afirma «Todas las licencias de (A) son MIT» y da para el Windows App SDK la licencia del repositorio de GitHub. El paquete que se redistribuye, `Microsoft.WindowsAppSDK` 2.5.1, trae su propio `license.txt`: «MICROSOFT SOFTWARE LICENSE TERMS — MICROSOFT WINDOWS APP SDK», que no es MIT, y un `NOTICE.txt` de terceros que no se reproduce. Inno Setup figura como «solo construcción, no se redistribuye», pero su motor de instalación y su desinstalador viajan en cada instalador. Tampoco se recogen los avisos de terceros del runtime de .NET.
**Impacto:** el texto que la app enseña en *Ayuda → Avisos de terceros* declara una licencia que no es la del componente. De fondo, la conclusión «compatible con GPLv3» deja de estar sostenida por lo escrito: un tercero que redistribuya el instalador bajo GPLv3 no puede ofrecer el código fuente de esos binarios. El remedio habitual es un permiso adicional de la sección 7 de la GPLv3; decidirlo es una cuestión jurídica.
**Evidencia:** `license.txt` y `.nuspec` del paquete en `%USERPROFILE%\.nuget\packages\microsoft.windowsappsdk\2.5.1\`.
**Solución propuesta:** citar en cada entrada la licencia que acompaña al artefacto redistribuido, mover Inno Setup al grupo de lo redistribuido, incluir o enlazar los avisos del runtime y del SDK, y decidir con asesoramiento si se añade el permiso adicional.
**Criterio de aceptación:** cada binario de terceros del instalador queda cubierto por una entrada con su licencia real, y el proyecto declara por escrito cómo encaja con su GPLv3.

### Severidad baja

#### Seguridad

### AUD-025 · El proceso elevado escribe, mueve y borra por ruta dentro de `%AppData%\FormatDiskPro`, que el usuario sin elevar controla
**Área:** Seguridad · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática (más la ACL medida) · **Esfuerzo:** Medio
**Ubicación:** `src/Services/History.cs:98-107`, `:137-140`, `:155-156` · `src/Services/AppSettings.cs:205`, `:222-223`
**Estado:** nuevo. Las pasadas anteriores siguieron el contenido de `settings.json` hasta los comandos (`T9-09`); ninguna miró las operaciones de archivo.
**Problema:** esas operaciones siguen puntos de reanálisis y se hacen como administrador sobre una carpeta en la que la cuenta sin elevar tiene control total.
**Impacto:** si la carpeta es una unión hacia otra, la app crea o sobrescribe allí `settings.json`, `history.log`, `history.1.log` y `settings.corrupt.json`. El contenido solo es controlable en parte. Misma precondición que AUD-002, con menos premio.
**Evidencia:** lectura del código y ACL de la carpeta real: control total heredado para la cuenta del usuario.
**Solución propuesta:** antes de operar, comprobar que la carpeta no es un punto de reanálisis y negarse si lo es; o mover el estado a una carpeta que solo escriban administradores, migrando lo existente.
**Criterio de aceptación:** con la carpeta sustituida por una unión, la app no crea ni modifica ningún archivo en el destino y lo deja dicho.

### AUD-026 · `powershell.exe` y `chkdsk.exe` se lanzan por nombre, no por ruta absoluta
**Área:** Seguridad · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/Services/DiskService.cs:166` · `src/Services/ReinitDrive.cs:101` · `src/Services/FormatProcess.cs:57` · `src/Services/CheckDisk.cs:54` · contraste: `src/Services/FormatProcess.cs:86`
**Estado:** nuevo. Las revisiones de inyección de comandos miraron los argumentos, no la resolución del ejecutable.
**Problema:** con `UseShellExecute = false`, un nombre sin ruta se busca en la carpeta de la app, el directorio de trabajo, `System32`, Windows y `PATH`. `powershell.exe` no está en `System32`, así que solo se encuentra por `PATH`, después del directorio de trabajo. El proyecto ya lo resuelve bien para `format.com`.
**Impacto:** si la app se lanza con el directorio de trabajo en una carpeta escribible por terceros, un `powershell.exe` colocado allí se ejecuta como administrador al seleccionar una unidad. Con los accesos directos del instalador el directorio es `{app}`, de ahí que sea Bajo.
**Evidencia:** lectura del código.
**Solución propuesta:** rutas absolutas desde un único punto: `Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe")` y `…"chkdsk.exe"`.
**Criterio de aceptación:** una prueba recorre los `ProcessSpec` que capturan los servicios y exige `Path.IsPathFullyQualified(spec.FileName)`.

#### Código

### AUD-027 · Un `settings.json` válido con un nulo donde se espera una lista impide arrancar la app
**Área:** Código · **Severidad:** Bajo · **Confianza:** Confirmado · **Verificación:** ejecución, estática · **Esfuerzo:** Bajo
**Ubicación:** `src/Services/AppSettings.cs:137-160` · `src/UI/MainWindow.FormatOptions.cs:444` · `src/App.xaml.cs:26`, `:43-68` · evidencia: `evidencias/arranque-presets-null-historial.txt`
**Estado:** nuevo. Afina `T9-08`, que cubrió el JSON ilegible, no el legible con valores imposibles.
**Problema:** `Load` normaliza pasadas, tamaño y sistema de archivos, pero no `UserPresets`. Con `"UserPresets": null`, el constructor de `MainWindow` lanza en `FillPresets` y, como todavía no hay ventana, la red de `App` no puede mostrar nada.
**Impacto:** la app muere en cada arranque (código `0xC000027B`) sin ventana ni mensaje, hasta que alguien edita o borra el archivo a mano. Contradice lo que promete la propia clase («un settings.json dañado nunca impide arrancar»). Solo ocurre con un archivo editado a mano o escrito por otra versión.
**Evidencia:** reproducido con el publish de la v1.29.0: proceso terminado a los pocos segundos y `CRASH: NullReferenceException … FillPresets` en el historial. Con `Language` o `Theme` a `null` arranca bien.
**Solución propuesta:** en `Load`, `loaded.UserPresets ??= []` y descartar presets nulos o sin nombre; y un `try/catch` en `OnLaunched` que, si no se pudo crear la ventana, muestre un mensaje nativo y salga.
**Criterio de aceptación:** una prueba carga un JSON con `UserPresets: null` y obtiene una lista vacía; la app arranca con ese archivo.

### AUD-028 · El diálogo con el resultado de una operación se descarta si hay otro abierto
**Área:** Código · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/UI/MainWindow.xaml.cs:318-337`, `:664`, `:673`, `:879-889` · `src/UI/MainWindow.Operations.cs:475`, `:496-499`
**Estado:** nuevo. No reabre `T13-19` (un solo diálogo a la vez): señala un caso que su razonamiento no contempla.
**Problema:** `ShowOneAsync` descarta el segundo diálogo porque «responde a una acción que el usuario puede repetir». El diálogo de resultado no responde a ninguna acción repetible. Basta tener abierta la pregunta «¿Cancelar?» cuando la operación termina.
**Impacto:** el aviso de éxito, el de error o el de «se crearon 1 de 2 particiones» no llega a verse; solo queda `DIALOG SKIPPED` en el historial.
**Evidencia:** lectura del flujo: mientras una operación corre, el único diálogo que se puede abrir es el de cancelar.
**Solución propuesta:** para los diálogos de resultado, esperar a que se cierre el abierto en vez de descartar; o cerrar el de «¿Cancelar?» cuando la operación termina.
**Criterio de aceptación:** una prueba de UI abre «¿Cancelar?» durante un benchmark, espera a que termine y comprueba que el resultado se muestra al cerrar la pregunta.

### AUD-029 · Conectar o retirar cualquier dispositivo restablece la etiqueta y las opciones que se estaban configurando
**Área:** Código · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/UI/MainWindow.xaml.cs:249`, `:411-435` · `src/UI/MainWindow.DriveInfo.cs:31-49`, `:58-62`
**Estado:** nuevo
**Problema:** el aviso de cambio de dispositivo recarga la lista entera; al vaciarla y rellenarla, `DrivePicker_SelectionChanged` se dispara dos veces y vuelve a escribir la etiqueta del volumen, las opciones de sistema de archivos y la casilla de FAT32 pequeña. Además, si la unidad recién seleccionada no está lista, el campo conserva la etiqueta de la anterior.
**Impacto:** quien ha escrito una etiqueta y enchufa otro dispositivo la pierde sin aviso; y puede formatear una unidad sin etiqueta con la etiqueta de otra.
**Evidencia:** lectura del código.
**Solución propuesta:** al recargar, no tocar las opciones si la unidad seleccionada es la misma (misma letra e identidad); vaciar el campo cuando la unidad no está lista.
**Criterio de aceptación:** una prueba de UI escribe una etiqueta, pulsa F5 y la etiqueta sigue ahí.

### AUD-030 · Si falla el lanzamiento del instalador, la interfaz se queda deshabilitada
**Área:** Código · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/UI/MainWindow.HelpAndUpdates.cs:241-243`, `:250-261` · `src/UI/MainWindow.xaml.cs:861`
**Estado:** nuevo
**Problema:** `_closingForUpdate = true` se fija antes de `LaunchInstaller`. Si `Process.Start` lanza, el `catch` muestra el error pero `EndOperation` sale pronto por ese indicador y no rehabilita los controles.
**Impacto:** tras el aviso de error, todos los controles quedan apagados; solo se puede cerrar la app.
**Evidencia:** lectura del código.
**Solución propuesta:** poner el indicador después de que `LaunchInstaller` haya devuelto, o devolverlo a `false` en el `catch`.
**Criterio de aceptación:** con un `IUpdateService` falso cuyo `LaunchInstaller` lanza, los controles vuelven a estar activos.

### AUD-031 · El resultado que muestra el historial se deduce de palabras sueltas del mensaje, incluida la etiqueta del usuario
**Área:** Código · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/Core/HistoryEntry.cs:297-317` · `src/UI/MainWindow.xaml.cs:663`
**Estado:** nuevo
**Problema:** `ParseResult` busca `CANCELLED`, `ERROR`, `FAIL` y `OK`, en ese orden, como palabra en cualquier parte de la línea. La línea de un formato correcto incluye la etiqueta que escribió el usuario (`label='…'`).
**Impacto:** un formato correcto con la etiqueta `MI FAIL DISK` aparece en el historial como fallo.
**Evidencia:** lectura del código.
**Solución propuesta:** decidir el resultado solo con el prefijo de la línea (la operación y la palabra que la sigue), no con todo el mensaje.
**Criterio de aceptación:** `FORMAT OK G: … label='A FAIL B'` se clasifica como correcto en una prueba.

#### Rendimiento

### AUD-032 · El trabajo por bloque y las continuaciones de los servicios se ejecutan en el hilo de la interfaz
**Área:** Rendimiento · **Severidad:** Bajo · **Confianza:** Alta para el coste por bloque (medido), Media para el benchmark · **Verificación:** ejecución (micro-medición), estática · **Esfuerzo:** Bajo
**Ubicación:** `src/Services/CapacityVerifier.cs:110-122`, `:143-172` · `src/Services/SecureWipe.cs:118-134` · `src/Services/BenchmarkRunner.cs:211-226` · llamadas: `src/UI/MainWindow.Operations.cs:65`, `:557`, `src/UI/MainWindow.xaml.cs:641` · `src/Program.cs:24-25`
**Estado:** nuevo
**Problema:** ningún `await` de `Services/` lleva `ConfigureAwait(false)` y la ventana los invoca desde el hilo de UI, así que cada continuación vuelve a él: el relleno del patrón, la comparación y, en el benchmark, una vuelta por el despachador por cada operación de 4 KiB.
**Impacto:** medido por bloque de 8 MB en .NET 10: `FillPattern` 4,12 ms, `RandomNumberGenerator.Fill` 1,26 ms, `SequenceEqual` 0,36 ms. En una USB (30 a 150 MB/s) es entre el 2 % y el 8 % del hilo: irrelevante. A 500 MB/s es el 28 %, y hacia 1,8 GB/s el hilo se satura y pasa a limitar la operación. Las IOPS de 4 KiB del benchmark pueden salir a la baja en unidades rápidas; eso no se midió.
**Evidencia:** `trabajo/scripts/bloque.cs` (resultado en `trabajo/hallazgos/03-07-08-coordinador.md`).
**Solución propuesta:** `ConfigureAwait(false)` en los `await` de `Services/`, o envolver la llamada en `Task.Run`; `Progress<T>` ya devuelve los informes al hilo de UI.
**Criterio de aceptación:** una prueba que ejecuta `RunAsync` bajo un `SynchronizationContext` contador registra 0 `Post` durante las fases de E/S.

#### UI/UX

### AUD-033 · Durante una operación, los tres botones de la barra rápida siguen activos y no hacen nada
**Área:** UI/UX · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/UI/MainWindow.xaml.cs:942-968` · `src/UI/MainWindow.xaml:116-132` · `src/UI/MainWindow.Operations.cs:136`, `:525` · `src/UI/MainWindow.HelpAndUpdates.cs:34`
**Estado:** nuevo (consecuencia no vista de `T11-02`)
**Problema:** `SetControlsEnabled(false)` apaga los tres menús pero no `BtnHealth`, `BtnBenchmark` ni `BtnHistory`; sus manejadores salen con `if (_isBusy) return;` sin decir nada.
**Impacto:** tres botones con aspecto de activos que no responden durante minutos u horas.
**Evidencia:** lectura del código.
**Solución propuesta:** deshabilitarlos en `SetControlsEnabled`, como los menús que duplican.
**Criterio de aceptación:** durante una operación, los tres botones tienen `IsEnabled = false`.

### AUD-034 · Guardar un preset sin nombre responde «Ya existe un preset con ese nombre»
**Área:** UI/UX · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/UI/PresetsDialog.xaml.cs:89-93`, `:199-203` · `src/Core/Presets.cs:62-67`
**Estado:** nuevo
**Problema:** `IsNameAvailable` devuelve `false` para un nombre vacío o de más de 40 caracteres, y el diálogo solo tiene un mensaje de error: el de nombre duplicado.
**Impacto:** el mensaje no dice qué hay que corregir.
**Evidencia:** lectura del código.
**Solución propuesta:** distinguir los tres motivos (vacío, demasiado largo, duplicado) con su texto.
**Criterio de aceptación:** cada motivo de rechazo muestra un texto distinto, en los cinco idiomas.

### AUD-035 · La confirmación de «Reinicializar» no identifica el disco físico ni las otras unidades que se van a perder
**Área:** UI/UX · **Severidad:** Bajo · **Confianza:** Confirmado · **Verificación:** ejecución (captura), estática · **Esfuerzo:** Bajo
**Ubicación:** `src/Localization/Localization.cs:368` · `src/UI/MainWindow.Operations.cs:432-441` · evidencia: `evidencias/galeria-pt/reinit-light.png`
**Estado:** nuevo
**Problema:** el texto dice que se borrará «TODO el disco físico de la unidad G: (todas sus particiones)», pero no nombra el disco (modelo, tamaño) ni las demás letras afectadas. En la captura, el disco tiene además la partición `I:` de 28 GB y el diálogo no la menciona.
**Impacto:** quien elige una partición pequeña de un medio con varias puede no caer en que pierde también las otras. La propia app crea medios de dos particiones.
**Evidencia:** captura de la app real sobre la USB de pruebas (dos particiones).
**Solución propuesta:** añadir al resumen el modelo y el tamaño del disco y la lista de unidades que desaparecerán («Disco de 29,3 GB · se perderán G: e I:»).
**Criterio de aceptación:** sobre un disco con dos particiones, la confirmación nombra las dos letras y el tamaño del disco.

#### Arquitectura y resiliencia

### AUD-036 · Una operación cancelada o a medias no deja un registro fiel ni actualiza la lista de unidades
**Área:** Arquitectura y resiliencia · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/UI/MainWindow.Operations.cs:67-72`, `:285-290`, `:455-460` · `src/UI/MainWindow.xaml.cs:249`, `:612-618`, `:643-659`, `:676-691`
**Estado:** nuevo. Afina `T5-03`, que cubrió el fallo parcial de Reinicializar, no su cancelación.
**Problema:** al cancelar Reinicializar no se escribe nada en el historial ni se recarga la lista, aunque el disco ya puede estar borrado; los avisos de dispositivo que llegan durante la operación se descartan y nadie los repite. Un formato correcto seguido de un borrado cancelado solo deja `WIPE CANCELLED`, y una excepción en el borrado se registra como `FORMAT ERROR`. Verificar y chkdsk cancelados tampoco dejan línea.
**Impacto:** el registro de auditoría de operaciones destructivas no cuenta lo que pasó, y la tarjeta de la unidad muestra un estado que ya no existe.
**Evidencia:** lectura del código.
**Solución propuesta:** registrar siempre el desenlace (`REINIT CANCELLED`, `FORMAT OK` antes del borrado, `WIPE ERROR`) y llamar a `LoadDrives()` en `EndOperation`.
**Criterio de aceptación:** cada desenlace de cada operación deja exactamente una línea que lo nombra, y la lista se recarga al terminar.

### AUD-037 · Los ajustes se escriben sin atomicidad, se degradan a valores por defecto ante un error de lectura y no hay instancia única
**Área:** Arquitectura y resiliencia · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Medio
**Ubicación:** `src/Services/AppSettings.cs:162-165`, `:217-229` · `src/UI/MainWindow.HelpAndUpdates.cs:93-94` · `src/Program.cs:13-19`
**Estado:** nuevo. Afina `T9-08`.
**Problema:** `Save` usa `File.WriteAllText` (trunca y escribe). `Load` devuelve valores por defecto ante cualquier excepción que no sea de formato, y el arranque guarda siempre. El mutex de `Program` se crea sin comprobar si ya existía, así que pueden correr varias instancias.
**Impacto:** un error transitorio de lectura al arrancar (archivo bloqueado, segunda instancia escribiendo) acaba con los presets del usuario sobrescritos por los valores por defecto. Dos instancias elevadas pueden además operar a la vez sobre la misma unidad.
**Evidencia:** lectura del código.
**Solución propuesta:** escribir a un temporal y sustituir con `File.Replace`; no guardar en el arranque si la carga falló por E/S; y, si ya hay una instancia, activar su ventana y salir.
**Criterio de aceptación:** una prueba simula un fallo de E/S en la lectura y comprueba que el archivo original no se sobrescribe; lanzar la app dos veces deja una sola ventana.

### AUD-038 · Si el proceso muere, los archivos temporales de borrado, verificación y benchmark se quedan en la unidad
**Área:** Arquitectura y resiliencia · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/Services/SecureWipe.cs:88`, `:144` · `src/Services/CapacityVerifier.cs:59`, `:184` · `src/Services/BenchmarkRunner.cs:61`, `:147`
**Estado:** nuevo
**Problema:** `__fdp_wipe__`, `__fdp_verify__` y `__fdp_bench__` se borran en un `finally` que ignora los fallos. Si el proceso se cierra a la fuerza, se va la luz o el borrado falla, nadie los limpia después ni lo dice.
**Impacto:** una unidad llena de archivos de gigabytes con nombres que el usuario no reconoce.
**Evidencia:** lectura del código; no hay ninguna referencia a esas carpetas fuera de los tres servicios.
**Solución propuesta:** al seleccionar una unidad, detectar esas carpetas y ofrecer borrarlas; y avisar si la limpieza del `finally` falla.
**Criterio de aceptación:** con una carpeta `__fdp_verify__` en la unidad, la app lo dice y la borra al aceptarlo.

### AUD-039 · El borrado seguro deja sin sobrescribir los últimos 64 MB y no lo dice
**Área:** Arquitectura y resiliencia · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/Services/SecureWipe.cs:36`, `:96` · `src/Localization/Localization.cs:142`
**Estado:** nuevo
**Problema:** se sobrescribe el espacio libre menos un margen de 64 MB. La opción se llama «Borrado seguro (sobrescribir espacio libre)» y nada menciona el margen.
**Impacto:** tras un formato rápido con borrado seguro pueden quedar hasta 64 MB de datos antiguos recuperables. El margen protege de llenar una unidad en uso, pero aquí la unidad acaba de formatearse.
**Evidencia:** lectura del código.
**Solución propuesta:** reducir el margen al mínimo que el sistema de archivos necesite cuando se viene de formatear, o declararlo en la pista de la opción y en el resultado.
**Criterio de aceptación:** el margen es coherente con lo que la interfaz promete, y una prueba fija el valor.

#### QA

### AUD-040 · El respaldo de ajustes de las pruebas de UI no sobrevive a que el proceso de pruebas muera
**Área:** QA · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `tests/FormatDiskPro.UiTests/SettingsBackup.cs:10-33` · `tests/FormatDiskPro.UiTests/AppFixture.cs:27`, `:47-53`
**Estado:** nuevo
**Problema:** el respaldo de `settings.json` e `history.log` vive solo en memoria y se restaura en `Dispose`. Si la corrida se interrumpe (Ctrl+C, tiempo agotado, caída), no hay de dónde restaurar.
**Impacto:** los ajustes y el historial reales del usuario quedan como los dejaron las pruebas: otro idioma, otro tema, entradas de prueba.
**Evidencia:** lectura del código.
**Solución propuesta:** escribir el respaldo también a disco y, al empezar la corrida siguiente, restaurarlo si existe uno sin consumir.
**Criterio de aceptación:** tras matar el proceso de pruebas a mitad, la corrida siguiente restaura los archivos originales.

#### Refactorización

### AUD-041 · La misma lógica vive copiada en varios sitios
**Área:** Refactorización · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Medio
**Ubicación:** sistemas de archivos y límites: `src/Core/PartitionPlan.cs:124`, `:127`, `src/UI/MainWindow.xaml.cs:24-46`, `:586`, `src/Core/FormatLogic.cs:122-127` · codificación de scripts: `src/Core/FormatLogic.cs:64-68`, `src/Services/DiskService.cs:160-171`, `src/Services/ReinitDrive.cs:96-103` · E/S sin caché: `src/Services/BenchmarkRunner.cs:38-45`, `:155-165`, `src/Services/CapacityVerifier.cs:30-36`, `:193-200`, `src/Services/SecureWipe.cs:32-36`
**Estado:** nuevo
**Problema:** la lista de sistemas de archivos, la codificación `-EncodedCommand` y las constantes y ayudantes de E/S alineada están duplicadas.
**Impacto:** un cambio en una copia no llega a las otras: es como el texto de AUD-046 quedó diciendo algo distinto del código.
**Evidencia:** lectura del código.
**Solución propuesta:** un único catálogo de sistemas de archivos en `Core/`, un único `PowerShellCommand.Encode`, y un ayudante compartido de búfer alineado.
**Criterio de aceptación:** cada dato y cada ayudante tiene una sola definición.

### AUD-042 · `MainWindow.xaml.cs` vuelve a superar el tope que el propio proyecto se fijó, y no hay formateador ni analizadores configurados
**Área:** Refactorización · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Medio
**Ubicación:** `src/UI/MainWindow.xaml.cs` (986 líneas) frente a `CONTEXT.md:80` · bloque de `using` repetido en las seis parciales (p. ej. `src/UI/MainWindow.Operations.cs:1-17`) · raíz del repo (sin `.editorconfig`)
**Estado:** regresión del objetivo de `T2-08`, que lo dejó en 753 líneas
**Problema:** la parcial principal ha crecido un 31 % desde `T2-08` y mezcla ciclo de vida, diálogos, formateo y accesibilidad. Las seis parciales repiten los mismos 16 `using`, casi todos sin uso en cada archivo, y no hay `.editorconfig` ni nivel de análisis que lo señale.
**Impacto:** el archivo más modificado del repo (46 cambios en doce meses) es otra vez el más largo.
**Evidencia:** recuento de líneas e historial de cambios de la línea base.
**Solución propuesta:** separar los ayudantes de diálogo y el ciclo de vida de la operación en sus propias parciales; `global using` para los comunes; `.editorconfig` con `IDE0005` como advertencia.
**Criterio de aceptación:** ninguna parcial pasa de 800 líneas y la compilación señala los `using` sin uso.

### AUD-043 · Miembros y textos sin uso, comentarios desfasados y documentación XML que nadie valida
**Área:** Refactorización · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/Localization/Localization.cs:470` (`about.body`, sin referencias) · `src/Services/UpdateService.cs:20-28`, `:173` (`Version` siempre igual a `TagName`) · `src/Services/ReinitDrive.cs:14` y `src/Services/DiskService.cs:35` (resúmenes que describen lo que la clase hacía antes) · `src/UI/MainWindow.Operations.cs:24-27` y `src/UI/MainWindow.HelpAndUpdates.cs:23-26` (`<para>` sin cerrar) · `src/UI/MainWindow.xaml.cs:777-801` (dos `<summary>` en el mismo método)
**Estado:** nuevo
**Problema:** restos de refactorizaciones anteriores. La documentación XML mal formada no se detecta porque el proyecto no genera el archivo de documentación.
**Impacto:** ruido para quien lee; ninguno en ejecución.
**Evidencia:** búsqueda de referencias y lectura.
**Solución propuesta:** borrar lo que no se usa, corregir los resúmenes y activar `GenerateDocumentationFile` con CS1570 como advertencia.
**Criterio de aceptación:** la compilación con la documentación activada no da avisos de XML mal formado.

#### Redacción

### AUD-044 · Dos pistas de la interfaz mandan no usar «el botón Iniciar», que ya no existe
**Área:** Redacción · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/Localization/Localization.cs:147`, `:151` (cinco idiomas) · `.github/ISSUE_TEMPLATE/bug_report.yml:69`
**Estado:** nuevo (resto de `T12-02`, que renombró el botón a «Formatear X:»)
**Problema:** «usa Herramientas → Reinicializar unidad… (no el botón Iniciar)», y su equivalente en los otros cuatro idiomas. La plantilla de error dice «3. Pulsar Iniciar».
**Impacto:** la pista que explica cómo no equivocarse de operación nombra un botón que el usuario no puede encontrar.
**Evidencia:** lectura.
**Solución propuesta:** nombrar el botón como se ve («no el botón Formatear») en los cinco idiomas y en la plantilla.
**Criterio de aceptación:** ninguna cadena ni plantilla menciona un botón «Iniciar».

### AUD-045 · El aviso de privacidad remite a un menú que no se llama así en inglés, portugués, francés ni italiano
**Área:** Redacción · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/Localization/Localization.cs:477` frente a `:232`
**Estado:** nuevo (el texto se reescribió en `T9-18`)
**Problema:** el texto dice «Configuration →» (el menú es «Settings»), «Configuração →» («Configurações»), «Configuration →» («Paramètres») y «Configurazione →» («Impostazioni»).
**Impacto:** la instrucción para desactivar la conexión automática no coincide con lo que se ve en pantalla.
**Evidencia:** lectura.
**Solución propuesta:** usar en cada idioma el nombre real del menú; mejor aún, componer el texto con la propia clave `menu.config`.
**Criterio de aceptación:** una prueba comprueba que `about.privacy` contiene el valor de `menu.config` en cada idioma.

### AUD-046 · Un mensaje de error dice que exFAT admite etiquetas de 32 caracteres; el código valida 11
**Área:** Redacción · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/Localization/Localization.cs:439` (cinco idiomas) frente a `src/Core/FormatLogic.cs:122-127`
**Estado:** nuevo (entró con `T13-18`)
**Problema:** «FAT32 y FAT admiten 11 caracteres; NTFS, exFAT y ReFS, 32». `MaxLabelLength` devuelve 11 para exFAT.
**Impacto:** el mensaje que explica por qué se rechazó la etiqueta da una regla que el siguiente intento vuelve a incumplir.
**Evidencia:** lectura.
**Solución propuesta:** corregir el texto y generarlo a partir de `MaxLabelLength`.
**Criterio de aceptación:** una prueba compara las cifras del texto con `MaxLabelLength` para los cinco sistemas.

### AUD-047 · Las IOPS del benchmark se formatean con la cultura de Windows y no con la del idioma de la app
**Área:** Redacción · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/UI/MainWindow.Operations.cs:622-623`
**Estado:** ya conocido (`T6-12`), cierre incompleto: «los números pasan a seguir al idioma de la app» no llegó a este punto
**Problema:** `$"{…:N0} IOPS"` usa la cultura del hilo. El resto de números usa `L.Culture`.
**Impacto:** con la app en inglés sobre un Windows en español, «12.345 IOPS» junto a «1.5 MB/s».
**Evidencia:** lectura.
**Solución propuesta:** `string.Create(L.Culture, $"…")` o `ToString("N0", L.Culture)`.
**Criterio de aceptación:** una prueba fija el separador de miles de las IOPS para cada idioma.

### AUD-048 · Tratamiento, terminología y tipografía no son uniformes dentro de cada idioma
**Área:** Redacción · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Medio
**Ubicación:** tratamiento de usted en `src/Localization/Localization.cs:395`, `:396`, `:406`, `:446`, `:455`, `:457` frente al tuteo del resto · terminología y puntuación: lista completa en `trabajo/hallazgos/10-redaccion.md`
**Estado:** nuevo
**Problema:** el español mezcla «Seleccione», «Actualice» y «Asegúrese» con «Marca», «usa» y «Recuerda». Hay términos dobles («removible» y «extraíble», «disco de sistema» y «del sistema», «Cluster» y «clúster»; en portugués «Presets» y «Predefinições»; en italiano «Vedi» y «Visualizza»), puntos suspensivos desiguales en el menú Ayuda y convenciones de comillas y de signo de porcentaje sin fijar.
**Impacto:** de pulido; ninguno funcional.
**Evidencia:** lectura de las 326 claves en los cinco idiomas por el subagente de redacción, con un script propio (0 marcadores descuadrados, 0 llaves sueltas).
**Solución propuesta:** un glosario breve por idioma en `CONTRIBUTING.md` y una pasada de unificación.
**Criterio de aceptación:** el glosario existe y una búsqueda de cada término descartado no devuelve resultados.

### AUD-049 · Hay texto visible que no pasa por la localización, y en francés conviven dos sistemas de unidades
**Área:** Redacción · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática, ejecución (capturas) · **Esfuerzo:** Medio
**Ubicación:** `src/UI/MainWindow.xaml.cs:912` («ETA») · `src/Core/FormatLogic.cs:185` (B, KB, MB, GB, TB fijos) · detalle del historial tal como se escribe en el registro · evidencias: `evidencias/galeria-fr-656/main-secure-light.png`, `evidencias/galeria-es/history-dark.png`
**Estado:** nuevo
**Problema:** «ETA» y las unidades están escritas en el código. En francés, el texto fijo dice «Limite de 4 Go par fichier» y los valores calculados salen como «1020 MB» y «4 KB». El historial enseña la línea técnica en inglés («UPDATE DOWNLOADED», «BENCH F: seq w=…») bajo una categoría traducida.
**Impacto:** de coherencia; el francés es donde más se nota.
**Evidencia:** capturas de la app real.
**Solución propuesta:** pasar «ETA» y las unidades por `L.T` (o decidir que las unidades no se traducen y cambiar el texto fijo francés a «4 GB»).
**Criterio de aceptación:** en la galería en francés no conviven «Go» y «GB».

#### Documentación

### AUD-050 · La plantilla de sugerencias sigue diciendo que el CI está fuera de alcance a propósito
**Área:** Documentación · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `.github/ISSUE_TEMPLATE/feature_request.yml:8-11` · `ROADMAP.md:3678-3680`
**Estado:** nuevo (la decisión cambió el 2026-09-18 y estos dos textos no)
**Problema:** la plantilla lista «CI» entre lo descartado a propósito, y el `ROADMAP.md` dice que las plantillas «son coherentes con que no haya CI». Hay CI desde el 2026-09-18.
**Impacto:** quien proponga algo sobre el CI lee que no se quiere.
**Evidencia:** lectura.
**Solución propuesta:** quitar «CI» de la lista y corregir la nota del `ROADMAP.md`.
**Criterio de aceptación:** ninguna plantilla presenta el CI como fuera de alcance.

### AUD-051 · El README promete velocidad y tiempo restante «en toda operación larga»; solo las dan dos
**Área:** Documentación · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `README.md:80` frente a `src/UI/MainWindow.xaml.cs:901-916`
**Estado:** nuevo
**Problema:** la velocidad y el tiempo restante solo se calculan cuando la operación informa de bytes, y solo lo hacen Verificar capacidad y el borrado seguro. Formatear, chkdsk, Reinicializar y Benchmark muestran tiempo transcurrido y, como mucho, porcentaje.
**Impacto:** el README afirma más de lo que hay.
**Evidencia:** lectura.
**Solución propuesta:** ajustar la frase («tiempo y cancelación en toda operación larga; velocidad y tiempo restante en las que escriben el espacio libre»).
**Criterio de aceptación:** el README describe lo que cada operación muestra.

#### DevOps

### AUD-052 · El corte prueba una compilación de depuración y sin el listón de cero advertencias
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `release.ps1:515`, `:519` · `src/installer/build-installer.ps1:183-190` · `.github/workflows/ci.yml:74-84`
**Estado:** nuevo
**Problema:** el corte compila y prueba sin `-c Release` ni `-warnaserror`; el instalador sale después de un `dotnet publish -c Release` que no pasa por ninguna prueba. El CI sí compila en Release sin advertencias, pero se ejecuta cuando el corte ya subió la rama.
**Impacto:** el listón que declara `CONTRIBUTING.md` no forma parte de la puerta de publicación.
**Evidencia:** las dos líneas citadas.
**Solución propuesta:** `dotnet build -c Release -warnaserror` y `dotnet test -c Release --no-build` en el corte.
**Criterio de aceptación:** una advertencia introducida a propósito aborta `release.ps1 -DryRun`.

### AUD-053 · Las herramientas de los últimos pasos se validan después de mutar, y la simulación no las comprueba
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `release.ps1:689-728`, `:753-770`, `:814-844` · `src/installer/build-installer.ps1:129`, `:139`, `:177`
**Estado:** nuevo (misma familia que `T9-02`)
**Problema:** el `.csproj` se reescribe antes de saber si el instalador se puede compilar, y `gh` se busca y se autentica con la rama y el tag ya subidos. `-DryRun` no comprueba Inno Setup, `gh` ni el certificado.
**Impacto:** cortes a medias evitables, y una simulación que da por bueno un plan que el corte real no puede completar.
**Evidencia:** orden de las líneas citadas.
**Solución propuesta:** una comprobación previa compartida con `-DryRun` (Inno Setup, `gh` y su sesión, certificado si se firma) y restaurar el `.csproj` si falla el instalador.
**Criterio de aceptación:** sin `gh` en el `PATH`, `release.ps1 -DryRun` aborta nombrándolo.

### AUD-054 · La guarda de «sección del CHANGELOG vacía» no salta cuando dos encabezados de versión van seguidos
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Confirmado · **Verificación:** ejecución (semántica reproducida), estática · **Esfuerzo:** Bajo
**Ubicación:** `release.ps1:653-662`
**Estado:** nuevo (afina `T8-04` y `T4-01`, que siguen resueltas en el caso normal)
**Problema:** el cuerpo se toma con `$lines[$start..($end - 1)]`. Si tras el encabezado de la versión viene ya otro `## …`, el rango es descendente y devuelve dos líneas en vez de ninguna, así que la guarda no se dispara.
**Impacto:** el release se publicaría con dos encabezados como notas, que es también lo que enseñaría *Novedades*.
**Evidencia:** reproducido en PowerShell 5.1: `$lines[1..0]` devuelve `## [1.29.1]|# Changelog`.
**Solución propuesta:** exigir `$end -gt $start` y que el cuerpo contenga alguna línea que no sea encabezado.
**Criterio de aceptación:** con dos encabezados seguidos, el corte aborta.

### AUD-055 · El corte no dice qué pruebas unitarias se omitieron
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `release.ps1:519-521`, `:292-299`, `:324-328`
**Estado:** nuevo (extiende `T2-12`, resuelta para las de UI)
**Problema:** las unitarias se dan por buenas con el código de salida, sin `.trx`. En las de UI, si el `.trx` no se puede leer, el corte sigue con un aviso que afirma que pasaron.
**Impacto:** una prueba que pase a omitirse no deja rastro en el corte.
**Evidencia:** lectura.
**Solución propuesta:** `--logger trx` también en las unitarias, y tratar como fallo un `.trx` ausente o sin resultados.
**Criterio de aceptación:** una prueba omitida aparece nombrada en el resumen del corte.

### AUD-056 · El corte solo rechaza una de las tres variables que hacen tocar hardware
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `release.ps1:605-607` · `tests/FormatDiskPro.UiTests/TestDriveFacts.cs` · `tests/FormatDiskPro.Tests/CapacityVerifierDriveTests.cs:16-23`
**Estado:** nuevo
**Problema:** solo se mira `FORMATDISKPRO_ALLOW_DESTRUCTIVE`, y solo con `-UiTests`. No se miran `FORMATDISKPRO_ALLOW_YANK` (desmonta la USB a la fuerza) ni `FORMATDISKPRO_VERIFY_DRIVE` (activa en las unitarias una escritura sobre una unidad real).
**Impacto:** con una de esas variables viva de una sesión de depuración, el corte toca hardware sin avisar.
**Evidencia:** búsqueda en el script: una sola referencia.
**Solución propuesta:** rechazar o anunciar las tres antes de la primera ejecución de pruebas.
**Criterio de aceptación:** con cualquiera de las tres definida, `release.ps1 -DryRun` aborta o lo declara.

### AUD-057 · Los paquetes de prueba no los actualiza nadie y ya divergen entre los dos proyectos
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática, línea base · **Esfuerzo:** Bajo
**Ubicación:** `.github/dependabot.yml` · `tests/FormatDiskPro.Tests/FormatDiskPro.Tests.csproj:12-17` · `tests/FormatDiskPro.UiTests/FormatDiskPro.UiTests.csproj:12-16`
**Estado:** nuevo. No reabre la decisión de subir a mano el Windows App SDK: señala un efecto que no cubre su motivo.
**Problema:** excluir NuGet de Dependabot por un paquete deja sin vigilar todos. A 2026-10-11: `Microsoft.NET.Test.Sdk` 17.11.1 en un proyecto y 17.14.1 en el otro (última 18.10.1), `xunit` 2.9.2 y 2.9.3, `xunit.runner.visualstudio` 2.8.2 y 3.1.4 (última 4.0.1), `coverlet.collector` 6.0.2 (última 10.1.0).
**Impacto:** el recolector de cobertura que decide si un corte sale, sobre el que sigue abierta `T10-02`, lleva cuatro versiones mayores de retraso sin que nadie lo haya decidido.
**Evidencia:** `dotnet list package --outdated`.
**Solución propuesta:** añadir `nuget` a Dependabot con `ignore` para `Microsoft.WindowsAppSDK*`, y unificar versiones con `Directory.Packages.props`.
**Criterio de aceptación:** los dos proyectos de prueba referencian las mismas versiones y algo propone sus actualizaciones.

### AUD-058 · El CI no ejercita lo que solo falla al publicar
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Medio
**Ubicación:** `.github/workflows/ci.yml:66-84` · raíz del repo (sin `.editorconfig` ni `.gitattributes`)
**Estado:** nuevo. No reabre que las pruebas de UI no corran en CI.
**Problema:** los tres fallos de publicación que el proyecto documenta (el `.pri` que no se copia, la ruta que pasa de `MAX_PATH`, un script sin BOM que PowerShell 5.1 no lee) solo aparecen al publicar o al ejecutar los scripts en 5.1, y el CI no hace ninguna de las dos cosas.
**Impacto:** una regresión ahí se descubre el día del corte.
**Evidencia:** lectura del workflow.
**Solución propuesta:** un paso de `dotnet publish --self-contained` que compruebe el `.pri` y la longitud de las rutas, y otro con `shell: powershell` que analice los cuatro `.ps1` y verifique el BOM.
**Criterio de aceptación:** un PR que quite el BOM de `release.ps1` o rompa `CopyAppPriToPublish` queda en rojo.

### AUD-059 · El script de capturas falla o se degrada cuando lo lanza otro proceso
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Confirmado · **Verificación:** ejecución, estática · **Esfuerzo:** Bajo
**Ubicación:** `tools/capture-screenshots.ps1:56-58`, `:588-591`, `:406`, `:418`, `:595`
**Estado:** nuevo
**Problema:** con `powershell -File … -Only main,confirm` la lista llega como una sola cadena y el script aborta («El filtro -Only no coincide con ninguna toma»). Dentro de PowerShell, un nombre mal escrito se descarta sin aviso y la corrida termina en «Galería COMPLETA». La salida capturada desde otro proceso pierde los acentos («Galer�a»).
**Impacto:** es la herramienta de evidencias de las áreas 5 y 6, y desde `cmd`, Git Bash o una tarea programada el filtro no funciona.
**Evidencia:** observado durante esta auditoría; con `-Command "& …"` funciona.
**Solución propuesta:** separar `$Only` por comas al entrar, validar cada nombre contra la lista de tomas y fijar `[Console]::OutputEncoding` a UTF-8.
**Criterio de aceptación:** las dos formas de invocación producen las mismas tomas y un nombre inexistente aborta nombrándolo.

### AUD-060 · `new-selfsigned-cert.ps1` sigue recibiendo la contraseña como texto en la línea de comandos
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/installer/new-selfsigned-cert.ps1:34`, `:41`, `:66`
**Estado:** nuevo: es el patrón que `T3-09` corrigió en los otros dos scripts
**Problema:** `[string]$Password`, con un ejemplo que invita a escribirla en claro.
**Impacto:** bajo (certificado de pruebas); queda en el historial de PowerShell.
**Evidencia:** lectura.
**Solución propuesta:** `[SecureString]$Password`, pidiéndola con `Read-Host -AsSecureString` si falta.
**Criterio de aceptación:** ningún script del repo declara una contraseña como `[string]`.

### AUD-061 · El corte no activa el modo estricto y deja dos redirecciones de error sin proteger
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `release.ps1:95`, `:418`, `:426` · `src/installer/build-installer.ps1:52`
**Estado:** nuevo
**Problema:** ni `release.ps1` ni `build-installer.ps1` usan `Set-StrictMode` (sí lo hace el de capturas). Las líneas 418 y 426 redirigen la salida de error de `git` con la preferencia en `Stop`, que es lo que el propio script describe como trampa.
**Impacto:** una variable mal escrita vale `$null` en silencio; sin red, el corte muere con un error crudo. Falla antes de mutar nada.
**Evidencia:** lectura.
**Solución propuesta:** `Set-StrictMode -Version Latest` y un ayudante para las llamadas de solo lectura que comprueban `$LASTEXITCODE`.
**Criterio de aceptación:** sin red, `release.ps1 -DryRun` aborta con un mensaje propio.

### AUD-062 · El instalador sigue pudiendo vaciar una carpeta ajena si el destino se da por línea de comandos
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Media · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/installer/installer.iss:54`, `:82-91`
**Estado:** ya conocido (`T9-17`), cierre incompleto
**Problema:** `DisableDirPage=yes` oculta la página, pero el parámetro estándar `/DIR=` sigue fijando el destino, y `[InstallDelete] {app}\*` se ejecuta igual.
**Impacto:** un despliegue por script con `/DIR=` hacia una carpeta con contenido la vacía sin aviso.
**Evidencia:** lectura; no se probó en una máquina virtual.
**Solución propuesta:** en `[Code]`, negarse si `{app}` existe, no está vacía y no contiene `FormatDiskPro.exe`.
**Criterio de aceptación:** con `/DIR=` a una carpeta ajena no vacía, el instalador se niega.

### AUD-063 · No queda registro de lo que cada instalador empaqueta
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Medio
**Ubicación:** `release.ps1:847` · `src/installer/build-installer.ps1:183-227`
**Estado:** nuevo
**Problema:** el instalador lleva el runtime de .NET y el Windows App SDK completos (519 archivos, 229 MB publicados) y ni el release ni el repo dicen qué versiones van dentro.
**Impacto:** ante un aviso de seguridad del runtime no se puede decir qué versiones de la app están afectadas.
**Evidencia:** lectura; recuento del publish.
**Solución propuesta:** adjuntar un SBOM (CycloneDX o SPDX) como tercer asset o, como mínimo, una línea en las notas con SDK, runtime y Windows App SDK.
**Criterio de aceptación:** cada release declara las versiones que contiene.

### AUD-064 · El comando de cobertura que recomienda la guía deja archivos que hacen abortar el corte siguiente
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `.github/CONTRIBUTING.md:24` · `.gitignore` · `release.ps1:484-487`
**Estado:** nuevo
**Problema:** `dotnet test --collect:"XPlat Code Coverage"` sin directorio de resultados escribe en `tests/…/TestResults/`, que no está ignorado; la guarda de árbol sucio aborta después el corte.
**Impacto:** un aborto que no explica su causa.
**Evidencia:** `.gitignore` no contiene `TestResults/`.
**Solución propuesta:** añadir `TestResults/` y `*.trx` a `.gitignore`.
**Criterio de aceptación:** tras ejecutar el comando de la guía, `git status` no lista nada nuevo.

### AUD-065 · `build-installer.ps1 -Version` etiqueta el instalador pero no el binario, y los valores por defecto de `installer.iss` están obsoletos
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/installer/build-installer.ps1:183-190`, `:204` · `src/installer/installer.iss:20-28`
**Estado:** nuevo
**Problema:** usado suelto con una versión distinta de la del `.csproj`, produce un instalador cuyo ejecutable declara otra versión. `installer.iss` conserva como valores por defecto la versión 1.1.0 y una ruta de publish dentro del repo.
**Impacto:** nulo en el flujo de `release.ps1`; real si alguien compila a mano: la app se ofrecería a sí misma como actualización.
**Evidencia:** lectura.
**Solución propuesta:** pasar `-p:Version` al publish o abortar si difiere; quitar los valores por defecto y fallar con `#error`.
**Criterio de aceptación:** no se puede producir un instalador cuya versión difiera de la de su ejecutable.

### AUD-066 · La rama principal no está protegida y las alertas de dependencias están desactivadas
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Confirmado · **Verificación:** ejecución (API de GitHub, solo lectura) · **Esfuerzo:** Bajo
**Ubicación:** configuración del repositorio `xfiberex/FormatDiskPro`
**Estado:** nuevo
**Problema:** `master` no tiene protección de rama ni reglas (`Branch not protected`, 0 rulesets); las alertas de Dependabot y sus actualizaciones de seguridad están desactivadas; el PR #2 de Dependabot lleva abierto desde el 2026-10-01. Sí están activos el escaneo de secretos y su protección al subir, y CodeQL no tiene alertas abiertas.
**Impacto:** nada impide un `push --force` accidental sobre la rama de la que salen los releases, y ninguna alerta avisaría de una vulnerabilidad en un paquete.
**Evidencia:** respuestas de la API el 2026-10-11.
**Solución propuesta:** una regla que impida forzar y borrar `master` y exija el check de CI en los PR; activar las alertas de Dependabot; revisar el PR #2.
**Criterio de aceptación:** la API devuelve la protección y las alertas activas.

#### Legal

### AUD-067 · La app conecta con GitHub en el primer arranque tras actualizar aunque la comprobación automática esté desactivada · *requiere revisión legal*
**Área:** Legal · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/UI/MainWindow.xaml.cs:280-286` · `src/UI/MainWindow.HelpAndUpdates.cs:84-107` · `src/Localization/Localization.cs:477` · `README.md:198` · `.github/SECURITY.md:53`
**Estado:** ya conocido (`T9-18`), cierre incompleto: su criterio era que, desactivada, «no salga ninguna petición de red en el arranque»
**Problema:** `MaybeShowWhatsNewAsync` se ejecuta antes y fuera de la comprobación de la preferencia y, cuando la versión instalada no coincide con la última vista, pide las notas a la API de GitHub.
**Impacto:** quien desactivó la conexión transmite igualmente su IP una vez por actualización, que es lo que el texto de privacidad dice que no ocurre.
**Evidencia:** lectura del flujo de arranque; el único `HttpClient` de la app está en `UpdateService`.
**Solución propuesta:** con la preferencia desactivada, mostrar *Novedades* sin red o no mostrarlo solo; y precisar el texto (actualizaciones y notas de versión).
**Criterio de aceptación:** con la preferencia desactivada y una versión nueva, el arranque no emite ninguna petición.

### AUD-068 · El repo redistribuye nueve *skills* de un tercero sin su licencia ni su aviso de copyright
**Área:** Legal · **Severidad:** Bajo · **Confianza:** Media · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `.agents/skills/*/SKILL.md` · `skills-lock.json` · `THIRD-PARTY-NOTICES.txt`
**Estado:** nuevo
**Problema:** los nueve archivos proceden de `github/awesome-copilot`. Se registra su procedencia y un hash, pero no su licencia ni su titular, y nada dice que no están bajo la GPLv3 del proyecto.
**Impacto:** atribución incompleta y ambigüedad sobre qué licencia cubre esos archivos. Confianza Media: no se cotejó la licencia de origen.
**Evidencia:** lectura.
**Solución propuesta:** añadir la licencia y el copyright del origen junto a los archivos y una línea en los avisos; o dejar de versionarlos.
**Criterio de aceptación:** cada archivo de terceros del repo tiene identificada su licencia.

### AUD-069 · La licencia se declara como «GPLv3» sin precisar si es «solo» o «o posterior»
**Área:** Legal · **Severidad:** Bajo · **Confianza:** Media · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `README.md:196` · `src/Localization/Localization.cs:473` · `src/FormatDiskPro.csproj:16-18`
**Estado:** nuevo
**Problema:** ni el README, ni *Acerca de*, ni el `.csproj` dan el identificador SPDX, y las fuentes no llevan aviso de licencia.
**Impacto:** ambigüedad para quien reutilice código suelto. Es una recomendación de la propia licencia, no un incumplimiento.
**Evidencia:** lectura.
**Solución propuesta:** declarar `GPL-3.0-only` o `GPL-3.0-or-later` en los tres sitios y una cabecera SPDX de una línea por fuente.
**Criterio de aceptación:** el mismo identificador aparece en README, *Acerca de* y fuentes.

### AUD-070 · Al pedir el historial se avisa de «letras y etiquetas», pero también contiene rutas con el nombre de usuario
**Área:** Legal · **Severidad:** Bajo · **Confianza:** Confirmado · **Verificación:** ejecución (captura), estática · **Esfuerzo:** Bajo
**Ubicación:** `.github/ISSUE_TEMPLATE/bug_report.yml:76-80` · `.github/SECURITY.md:32-33` · `src/UI/MainWindow.HelpAndUpdates.cs:236` · `src/UI/MainWindow.xaml.cs:278` · `src/App.xaml.cs:47` · evidencia: `evidencias/galeria-es/history-dark.png`
**Estado:** nuevo
**Problema:** la plantilla y la política invitan a pegar líneas del historial en un issue público. El historial registra además la ruta de cada actualización descargada (`C:\Users\<usuario>\AppData\…`), la del `settings.json` apartado y trazas de excepción con rutas de compilación.
**Impacto:** el usuario puede publicar su nombre de cuenta de Windows sin que el aviso se lo haya dicho.
**Evidencia:** captura del historial de la app real.
**Solución propuesta:** ampliar el aviso, o registrar solo el nombre del archivo.
**Criterio de aceptación:** el aviso enumera lo que el historial puede contener, o el historial deja de contener rutas del perfil.

### AUD-071 · La guía de contribución no dice bajo qué licencia entran las aportaciones · *requiere revisión legal*
**Área:** Legal · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `.github/CONTRIBUTING.md` · `.github/PULL_REQUEST_TEMPLATE.md`
**Estado:** nuevo (el repositorio es público desde 2026-09)
**Problema:** ninguno de los dos archivos menciona la licencia de las contribuciones ni pide certificación de origen.
**Impacto:** por defecto lo aportado entra bajo la licencia del repo, lo que deja al autor sin margen para añadir después el permiso adicional de AUD-024 sin el acuerdo de cada colaborador.
**Evidencia:** búsqueda de «licen», «GPL» y «DCO» en ambos: sin resultados.
**Solución propuesta:** una sección «Licencia de las contribuciones» y valorar un DCO (`Signed-off-by`).
**Criterio de aceptación:** la guía y la plantilla dicen bajo qué términos se acepta una contribución.

## 4. Orden de ataque recomendado

El plan ejecutable es la Parte 3 de [`ROADMAP.md`](../../../ROADMAP.md); esto es solo el orden y su porqué.

1. **Que una operación destructiva no pueda decir algo distinto de lo que hace:** AUD-001 (cancelar), AUD-013 (identidad de la unidad) y AUD-012 (guarda que falla abierta), junto con AUD-015, que extrae esas decisiones a `Core/` y las deja bajo prueba. Es el corazón del producto y son los únicos que pueden acabar en datos perdidos.
2. **La frontera con lo que el usuario sin elevar controla:** AUD-002 primero; AUD-004, AUD-025 y AUD-026 son la misma familia y conviene resolverlas con el mismo criterio.
3. **Accesibilidad del selector:** AUD-003 es de las más baratas del informe (dos líneas y una prueba) y quita un tope de nota. AUD-014 va detrás.
4. **Arreglos rápidos de un solo sitio**, cada uno de menos de dos horas: AUD-006 y AUD-007 (chkdsk), AUD-008 (expulsar), AUD-009 (resultado del borrado), AUD-011 (texto de error), AUD-027 (arranque), AUD-005 y AUD-066 (dos interruptores en GitHub), AUD-044 a AUD-047 (textos).
5. **Antes del próximo corte:** AUD-020, AUD-021 y AUD-052 a AUD-056. Ninguno afecta al usuario de la v1.29.0; todos pueden estropear un release.
6. **Documentos vivos:** AUD-019 antes que AUD-018, porque corregir cifras en un archivo que luego se va a partir es trabajo doble. AUD-017 es independiente y rápida.
7. **Decisiones que no son de ingeniería:** AUD-024, AUD-069 y AUD-071 (licencias) necesitan una decisión del autor, con asesoramiento si es posible, antes de tocar un archivo.

## 5. Puntos fuertes

Lo que está bien hecho y conviene no perder al corregir lo anterior:

- **La lógica pura está separada y probada de verdad.** `Core/` no depende de WinUI, de procesos ni de red, y tiene un 97,7 % de cobertura con pruebas que anclan valores concretos, no idas y vueltas.
- **El plan de particiones se valida como dato antes de borrar nada**, con motivos tipados, y `ReinitDrive` lo revalida justo antes de `Clear-Disk`.
- **La verificación del instalador falla hacia el lado seguro:** sin hash, no se ejecuta; el hash se empareja por nombre, se lee con tope de 512 bytes y la firma no sirve de atajo mientras no haya publicador que fijar. Las tres tareas que lo cerraron (`T1-08`, `T2-06`, `T2-07`) siguen resueltas.
- **Todo PowerShell va por `-EncodedCommand`** y las dos posiciones interpoladas sin comillas (letra y sistema de archivos) se validan contra lista blanca. No se encontró ninguna ruta de inyección, tampoco en esta tercera pasada.
- **Las pruebas de UI conducen el binario real contra hardware real** y eligen la unidad por etiqueta y solo entre extraíbles. El ciclo destructivo completo (formatear, reinicializar, FAT32 pequeña, dos particiones) pasó y dejó los demás discos intactos, comprobado partición por partición.
- **El corte de versión documenta cada guarda con el incidente que la originó** y falla antes de mutar en casi todo lo que puede validar. Las dos guardas ejercidas en esta auditoría abortaron con mensajes que dicen qué hacer.
- **Los workflows de CI son mínimos y correctos:** permisos de solo lectura, acciones fijadas por SHA, sin secretos, y un resumen que dice lo que no se ejecutó.
- **La declaración de privacidad es verificable contra el código:** una sola clase abre conexiones, hacia un solo destino, con un `User-Agent` sin identificadores.
- **El historial se fecha en cultura invariante y neutraliza fórmulas al exportar a CSV.**
- **El `CONTEXT.md` explica el porqué de cada decisión.** Su tamaño es un problema (AUD-019); su contenido evitó en esta auditoría varias propuestas que ya estaban descartadas con motivo.

## 6. Descartados tras verificación

| Propuesto | Por quién | Por qué se descartó | Evidencia |
|---|---|---|---|
| «La app en reposo no se cierra cuando Windows se lo pide (`WM_CLOSE`)» | coordinador, 2026-10-11 17:07–17:12 | Artefacto de la sesión: esa terminal no se comprobó elevada y todo indica que no lo estaba, y UIPI bloquea los mensajes y la automatización hacia una ventana elevada. Con la terminal elevada, `WM_CLOSE` y `taskkill` sin `/F` cierran la app en 4 de 4 intentos, con y sin carga de CPU | `trabajo/scripts/cierre-wmclose.ps1` |
| «El arranque pasó de 0,5 s a 1,7–2,4 s con el Windows App SDK 2.x» | coordinador | Misma causa (la medición incluía la elevación y la carga de siete subagentes). Remedido en reposo: 512 ms | `trabajo/hallazgos/03-07-08-coordinador.md` |
| «Una excepción en el arranque deja un proceso elevado vivo y sin ventana» | subagente del área 1 | El proceso no se queda: termina con `0xC000027B`. El hecho de fondo es cierto y queda como AUD-027, con la consecuencia corregida | `evidencias/arranque-presets-null-historial.txt` |
| «La expulsión nunca llega a ejecutarse porque PowerShell termina antes que el shell» | coordinador | Refutado: sin nada abierto, los volúmenes se desmontan a los 3 s. Lo que falla es informar del resultado (AUD-008) | salida en `trabajo/progreso.md` |
| Seis avisos de gitleaks (`generic-api-key`) en el árbol de trabajo | gitleaks 8.30.1 | Todos en `obj/**/MsixContent/MSIX/*_license.xml`: salida de compilación del Windows App SDK, ignorada por git. El historial completo (153 commits) no tiene ninguno | `trabajo/gitleaks/*.json` |
| «`chkdsk` se cuelga en la primera pregunta» | coordinador, primera reproducción | La primera reproducción enviaba una marca de orden de bytes delante de la «N» (artefacto del arnés en .NET Framework). Repetida enviando exactamente lo que envía la app: la primera pregunta se contesta y el cuelgue está en la segunda. El hallazgo (AUD-006) se mantiene con la secuencia correcta | `evidencias/chkdsk-reparar-unidad-en-uso.txt` |

### Verificación independiente de los hallazgos propuestos como Altos

El subagente `verificador-hallazgos` recibió la afirmación, la ubicación y la evidencia de cada uno, sin el razonamiento del coordinador, con el encargo de refutarlos. No repitió las pruebas destructivas: leyó el código, buscó en `ROADMAP.md` y `CONTEXT.md` y midió las ACL en solo lectura.

| Hallazgo | Propuesto | Veredicto | Motivo |
|---|---|---|---|
| AUD-001 | Alto | **Confirmado (Alto)** | Fallo funcional reproducido, con estado falso en pantalla y en el historial de una operación destructiva. Sin mitigación en el código ni decisión previa. No llega a Crítico porque el borrado ya estaba confirmado por el usuario |
| AUD-002 | Alto | **Rebajado a Medio** | Real y sin mitigación en el código, pero exige una sesión ya comprometida, una actualización aceptada y ganar una carrera, y no cruza de una cuenta a otra: probabilidad baja por impacto alto, que es riesgo acotado |
| AUD-003 | Alto | **Confirmado (Alto, en el borde inferior)** | Dificulta, no impide; se mantiene porque es un fallo de nivel A en el primer paso obligatorio de todas las operaciones |

Sus tres reservas se aplicaron: se quitó de la evidencia de AUD-001 una columna de velocidad de escritura que no medía nada y se añadió que el caso largo exige desmarcar «Formato rápido»; se reescribió el impacto de AUD-003 para no afirmar que impide la tarea; y se añadió el volcado del selector desplegado, que faltaba.

## 7. Hipótesis por verificar

Sospechas sin confianza suficiente para ser hallazgo, y qué las confirmaría:

- **Texto con acentos rotos en los errores de `format.com` y `chkdsk`.** Esas herramientas escriben en la página OEM y .NET 10 decodifica la salida redirigida como UTF-8 por defecto. Lo confirma provocar un fallo de formato completo NTFS desde la app en un Windows en español y leer el diálogo.
- **IOPS de 4 KiB a la baja en unidades rápidas** por la vuelta al despachador en cada operación (AUD-032). Lo confirma comparar el benchmark de la app con el mismo motor sin contexto de sincronización sobre un SSD.
- **`TaskbarProgress` no llama a `HrInit`** antes de usar `ITaskbarList3` (`src/Services/TaskbarProgress.cs:55-58`). La documentación lo exige; no se observó ningún fallo.
- **`Clear-Disk` sobre el disco de arranque**: es posible que Windows lo rechace por su cuenta, lo que dejaría AUD-012 sin consecuencia práctica. No se puede comprobar sin riesgo.
- **Biblioteca colocada junto al instalador** en la carpeta de descarga (variante de AUD-002): depende de las protecciones de la versión de Inno Setup.
- **Navegador elevado** al abrir un enlace (AUD-004): depende de cada navegador y de si ya había uno abierto.
- **`installer.iss` sin BOM y con acentos:** si la versión instalada de Inno Setup lo lee como ANSI, la pregunta de la desinstalación saldría con los acentos rotos. Lo confirma desinstalar y leer el diálogo.
- **Carrera entre el mutex del instalador y la actualización silenciosa:** la app lanza el instalador y sale después; si el instalador comprueba el mutex antes, avisaría de «aplicación en ejecución». El flujo se probó de extremo a extremo en su día.
- **Borrador huérfano tras un `gh release create` interrumpido**, que haría fallar también `-ResumeRelease`.
- **Instalación en Windows 11 ARM64:** `ArchitecturesAllowed=x64compatible` la permite; no hay indicio de que se haya probado.
- **Procedencia y licencia del icono `FormatDiskPro.ico`:** no consta en el repo.
- **Caracteres no válidos en etiquetas FAT** (`. , ; + = [ ]`): `InvalidLabelChars` no los rechaza y el formato fallaría con un error poco claro.

## 8. Re-auditoría

**Nuevos:** 65 de los 71 hallazgos.

**Ya conocidos, con el cierre incompleto** (la tarea está marcada como hecha y una parte no lo está):

| Hallazgo | Tarea anterior | Qué quedó sin cerrar |
|---|---|---|
| AUD-047 | `T6-12` | Las IOPS siguen usando la cultura de Windows |
| AUD-062 | `T9-17` | El borrado de `{app}\*` sigue alcanzable con `/DIR=` |
| AUD-067 | `T9-18` | Sigue saliendo una petición de red en el arranque tras actualizar |
| AUD-024 | `T9-19` y «Dependencias de terceros: sin hallazgos» | La licencia declarada para el Windows App SDK no es la del paquete; el salto a la 2.x es el hecho nuevo |

**Regresiones** (se arregló y ha vuelto):

| Hallazgo | Tarea anterior | Qué volvió |
|---|---|---|
| AUD-018 | `T9-14`, `T9-15` | Cifras de estado copiadas a mano que caducan; árbol de archivos incompleto |
| AUD-042 | `T2-08` | `MainWindow.xaml.cs` vuelve a pasar del tope (986 líneas) |

**Tareas cerradas que se comprobaron y siguen resueltas:** `T0-02` (los flujos `async void` capturan sus errores; las dos pruebas de extracción de la unidad pasan), `T1-08`, `T2-06` y `T2-07` (verificación del instalador), `T2-12` y `T4-01` (corte), `T3-09` (contraseña como `SecureString` en los dos scripts del corte), `T8-04` a `T8-06`, `T9-01` a `T9-03`, `T9-07` (fecha invariante en el historial), `T9-08` (JSON ilegible apartado), `T9-09` (lista blanca en los dos constructores de comandos), `T10-01` y `T13-19` (un solo diálogo). **Ninguna cerrada en falso** fuera de los cuatro cierres incompletos de arriba.

**`T10-02`** (informe de cobertura vacío al cortar) sigue abierta y bloqueada a propósito. En esta auditoría el informe de cobertura salió completo; no se reprodujo.

**Decisiones cerradas respetadas:** no se propone dejar de correr elevada, hacer la ventana redimensionable, firmar el instalador, publicar en winget, ejecutar las pruebas de UI en CI ni convertir Reinicializar en un gestor de particiones.

## 9. Línea base y herramientas

| Qué | Resultado |
|---|---|
| Compilación (`dotnet build -c Release -warnaserror`) | 0 advertencias, 0 errores, 21,9 s |
| Unitarias (`dotnet test`, xUnit) | 690: 689 pasan, 0 fallan, 1 omitida (opt-in `FORMATDISKPRO_VERIFY_DRIVE`), 1 s |
| Cobertura (coverlet) | `Core` 97,7 % · `Services` 65,4 % · `UI` 0 % en unitarias · ensamblado entero 32,7 % de líneas |
| UI (`--filter "Category!=Slow"`, FlaUI, app real) | 43: 40 pasan, 3 omitidas (opt-in), 0 fallan, 1 m 45 s |
| UI con opt-in destructivo y de desmontaje | 3 de 3 pasan, 2 m 15 s; discos idénticos antes y después |
| Dependencias vulnerables (`dotnet list package --vulnerable --include-transitive`) | ninguna |
| Secretos (gitleaks 8.30.1 portable, suma verificada) | 0 en 153 commits |
| Análisis estático en CI (CodeQL) | 0 alertas abiertas |
| Arranque (5 arranques, reposo) | 512 ms hasta la ventana, 642 ms utilizable; 185 MB de working set |
| Instalador publicado v1.29.0 | SHA-256 igual al `.sha256` publicado; sin firmar (decisión cerrada) |
| `release.ps1 -DryRun` | las dos guardas alcanzables abortan correctamente; el árbol queda intacto |

Herramientas: .NET SDK 10.0.401, Windows PowerShell 5.1.26100, git 2.55, `gh` 2.102, Inno Setup 6 (presente, no ejecutado), gitleaks 8.30.1 (portable, borrado al terminar), Node 24 para dos scripts de recuento, CodeGraph para la lectura de código. No se instaló nada de forma global. Detalle en [`trabajo/linea-base.md`](trabajo/linea-base.md) y [`trabajo/progreso.md`](trabajo/progreso.md).

**Comandos documentados que no funcionan como dicen:** `tools/capture-screenshots.ps1 -Only a,b` desde `powershell -File` (AUD-059).

## 10. Zonas no cubiertas

| Qué no se revisó | Por qué | Qué haría falta |
|---|---|---|
| Alto contraste, escalado al 150 % y 200 %, varios monitores | No se cambió la configuración de Windows del usuario. El alto disponible al 150 % sí se simuló (`-WindowHeightDip 656`) | Una sesión dedicada o una máquina virtual |
| Narrador | No se puede escuchar desde esta sesión; se sustituyó por el volcado del árbol de UIA | Una pasada manual, o Accessibility Insights en modo de eventos (cerraría AUD-014) |
| Sustitución real del instalador (AUD-002) | Habría que ejecutar un binario propio como administrador | Una prueba de concepto en una máquina virtual |
| Cambio de dispositivo con la confirmación abierta (AUD-013) | Requería automatizar la app e intercambiar letras; se dejó en estática | La prueba unitaria que propone el propio hallazgo |
| Instalador en ejecución: `/DIR=`, desinstalación, Windows 10 anterior a la 2004, ARM64 | No se instaló ni desinstaló nada en la máquina del usuario | Una máquina virtual |
| `release.ps1 -DryRun` más allá de las guardas | Llegar a las pruebas y al plan exige una sección nueva en `CHANGELOG.md`, y la Fase 1 no modifica el proyecto | Ejecutarlo en la Fase 2 al preparar el siguiente corte |
| Benchmark sobre unidades rápidas | Solo se autorizó la USB de pruebas | Un SSD de pruebas |
| Verificación de capacidad completa (categoría `Slow`) | Tarda de 30 minutos a horas | Una corrida desatendida |
| Normativa de protección de datos, consumo y donaciones | La jurisdicción no se indicó | Indicarla, y revisión legal de AUD-024, AUD-067 y AUD-071 |
| Lectura íntegra por el coordinador de siete archivos de `src/` y de las pruebas | La revisión estática la empezaron subagentes que no terminaron (ver sección 0) | Una pasada específica; los archivos están listados en `trabajo/hallazgos/03-07-08-coordinador.md` |

**Contraste con ISO/IEC 25010:2023.** Adecuación funcional, eficiencia, capacidad de interacción, fiabilidad, seguridad, mantenibilidad y *safety* quedaron cubiertas por las áreas auditadas. **Compatibilidad** y **flexibilidad** solo en parte: no se probó la instalación en otras versiones de Windows ni en ARM64, ni la convivencia con otra instancia más allá de la lectura del código (AUD-037).

**Datos de prueba.** No se crearon cuentas. Los ajustes y el historial del usuario se respaldaron antes de cada prueba y están como estaban (`settings.json` idéntico byte a byte). La USB quedó como se encontró, y expulsada. Se borraron el publish de la auditoría, el binario portable de gitleaks y el instalador descargado para comprobar su hash. Queda la carpeta temporal de la sesión, con los guiones usados y el respaldo de los ajustes; no contiene datos personales y se puede borrar. De las evidencias se quitaron el nombre del equipo y las etiquetas de los discos que no son la USB de pruebas.
