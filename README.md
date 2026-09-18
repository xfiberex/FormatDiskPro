# FormatDiskPro

![Release](https://img.shields.io/github/v/release/xfiberex/FormatDiskPro?label=versión&color=blue)
![.NET](https://img.shields.io/badge/.NET-10-512BD4)
![Plataforma](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6)
![Licencia](https://img.shields.io/github/license/xfiberex/FormatDiskPro?label=licencia&color=green)
[![Compilación y unitarias](https://github.com/xfiberex/FormatDiskPro/actions/workflows/ci.yml/badge.svg)](https://github.com/xfiberex/FormatDiskPro/actions/workflows/ci.yml)
[![CodeQL](https://github.com/xfiberex/FormatDiskPro/actions/workflows/codeql.yml/badge.svg)](https://github.com/xfiberex/FormatDiskPro/actions/workflows/codeql.yml)

Formateo y diagnóstico de unidades para Windows. Parte del diálogo nativo *Formatear unidad* y añade
**5 sistemas de archivos**, **S.M.A.R.T.**, detección de **USB falsificadas**, **chkdsk**, **benchmark**,
**borrado seguro** y reinicialización de memorias rotas, con el **disco de sistema siempre protegido**.
WinUI 3, en 5 idiomas, gratis y sin telemetría.

> ⚠️ Formatear y borrar es **irreversible**. Comprueba la unidad antes de confirmar.

## Capturas

| Claro | Oscuro |
|:---:|:---:|
| ![Ventana principal en tema claro](docs/screenshots/main-light.png) | ![Ventana principal en tema oscuro](docs/screenshots/main-dark.png) |

<details>
<summary><b>Más pantallas</b>: S.M.A.R.T., chkdsk, reinicializar, confirmación e historial</summary>

| Salud del disco (S.M.A.R.T.) | |
|:---:|:---:|
| ![Salud S.M.A.R.T. en tema claro](docs/screenshots/health-light.png) | ![Salud S.M.A.R.T. en tema oscuro](docs/screenshots/health-dark.png) |

Cada métrica se colorea por rango **y** lleva su estado en texto. Lo que la unidad no expone sale como
*No disponible*, no como un cero engañoso.

| Comprobar errores (chkdsk) | Reinicializar unidad |
|:---:|:---:|
| ![Diálogo de chkdsk en tema claro](docs/screenshots/checkdisk-light.png) | ![Diálogo de reinicializar en tema oscuro](docs/screenshots/reinit-dark.png) |
| ![Diálogo de chkdsk en tema oscuro](docs/screenshots/checkdisk-dark.png) | ![Diálogo de reinicializar en tema claro](docs/screenshots/reinit-light.png) |

| Confirmar formato | Historial de operaciones |
|:---:|:---:|
| ![Diálogo de confirmación en tema claro](docs/screenshots/confirm-light.png) | ![Historial en tema claro](docs/screenshots/history-light.png) |
| ![Diálogo de confirmación en tema oscuro](docs/screenshots/confirm-dark.png) | ![Historial en tema oscuro](docs/screenshots/history-dark.png) |

*Reinicializar* y *chkdsk* se tomaron sobre una USB (reinicializar solo existe en extraíbles) y el resto
sobre un SSD interno (una USB no expone los contadores S.M.A.R.T.). El acento rojo es el de Windows en esa
máquina: la app usa el tuyo. Ninguna captura está editada: las genera
[`tools/capture-screenshots.ps1`](tools/capture-screenshots.ps1) conduciendo la app real.

</details>

## Qué hace

**Formateo**
- NTFS, exFAT, ReFS, FAT32 y FAT, con **sugerencia** según la unidad y una descripción de cada uno.
- Rápido o completo, con **progreso real en %** en el completo (NTFS/FAT/FAT32). Compresión NTFS opcional.
- **Borrado seguro** del espacio libre con 1, 3 o 7 pasadas (1 por defecto: NIST 800-88), con %,
  velocidad y tiempo restante.
- **Presets** integrados (USB universal, consola/TV, datos Windows…) y propios, que se guardan, renombran,
  reordenan y borran.

**Seguridad**
- El **disco de sistema** sale como `[Protegido]` y se bloquea dos veces: al listar y al iniciar.
- Para confirmar hay que **escribir la letra de la unidad**, y el botón nombra el destino (*Formatear H:*).
- Detecta la **protección de escritura** antes de formatear y ofrece quitarla.
- **Reinicializar unidad** (solo extraíbles) rehace una USB con particiones raras o RAW. Opcionalmente crea
  una FAT32 pequeña primero (1–32 GB, p. ej. para actualizar una BIOS) y deja el resto sin asignar o en
  una segunda partición exFAT/NTFS.

**Diagnóstico**
- **S.M.A.R.T.**: salud, temperatura, horas, desgaste de SSD, RPM y errores, coloreados por rango.
- **Capacidad real**: escribe y relee el espacio libre, sin caché, para destapar memorias falsificadas.
- **chkdsk**: *Solo comprobar* (lectura, también en el disco de sistema) o *Comprobar y reparar* (`/f`).
- **Benchmark** secuencial y 4 KiB aleatorio (MB/s e IOPS), sin caché y sin tocar tus datos.

**Día a día**
- Interfaz en **español, inglés, portugués, francés e italiano**, conmutable en caliente; las cifras siguen
  al idioma elegido.
- Tema automático, claro u oscuro, con el **color de acento de Windows**. Todo color de texto pasa una
  prueba automática de **contraste WCAG AA**.
- La lista de unidades se refresca sola al conectar o desconectar; expulsión segura de extraíbles.
- Tiempo, velocidad, ETA y **cancelación** en toda operación larga, y aviso (sonido + barra de tareas) al
  terminar si estás en otra ventana.
- **Historial** con búsqueda, filtros y exportación a CSV.
- **Actualizaciones** desde GitHub Releases con sus notas, y verificación del instalador antes de ejecutarlo.

Lo que viene, en la [hoja de ruta](ROADMAP.md); lo que trajo cada versión, en el [changelog](CHANGELOG.md).

## Instalación

Descarga `FormatDiskPro-x.y.z-setup.exe` de **[Releases](https://github.com/xfiberex/FormatDiskPro/releases)**
y ejecútalo. Incluye .NET: no hay que instalar nada más.

| Requisito | |
|---|---|
| Sistema | Windows 10 1809+ u 11, x64 |
| Permisos | Administrador (lo pide el UAC al abrir) |

La app busca versiones nuevas al arrancar (desactivable en *Configuración*) y en
*Ayuda → Buscar actualizaciones…*, y se actualiza sola.

> **Cómo se verifica una actualización.** El instalador corre como administrador, así que antes de
> lanzarlo la app exige una de dos cosas: una **firma Authenticode** de confianza o, como se publica
> **sin firmar**, que su **SHA-256** coincida con el `.sha256` publicado en el mismo release. Si falla,
> lo borra sin ejecutarlo. Esto detecta una descarga corrupta o manipulada en tránsito, **no** una cuenta de
> GitHub comprometida: el `.exe` y su hash salen del mismo sitio. Por eso SmartScreen avisa de «editor
> desconocido».

## Uso

1. Abre la app (pedirá permisos de administrador).
2. Elige la unidad. La de Windows aparece como `[Protegido]` y no se puede formatear.
3. Elige sistema de archivos, tamaño de clúster y etiqueta, o aplica un **preset**.
4. Pulsa **Formatear _X_:**, escribe la letra de la unidad y confirma.

*Salud*, *Benchmark* e *Historial*, que no escriben nada, están a un clic bajo el menú. Lo que borra está
en *Herramientas*, siempre con confirmación.

| Menú | Opciones |
|---|---|
| **Herramientas** | Verificar capacidad real · Salud del disco (S.M.A.R.T.) · Comprobar errores (chkdsk) · Benchmark rápido · Quitar protección de escritura · Reinicializar unidad · Expulsar unidad · Ver historial |
| **Configuración** | Idioma · Tema · Presets (y *Gestionar presets…*) · Avisar al terminar · Buscar actualizaciones al iniciar |
| **Ayuda** | Buscar actualizaciones · Novedades · Licencia · Avisos de terceros · Acerca de |

Lo que la unidad seleccionada no admite aparece **apagado y con el motivo en el propio menú**, p. ej.
«Expulsar unidad *(solo extraíbles)*».

| Sistema | Para qué | Archivo máx. |
|---|---|---|
| NTFS | Discos internos de Windows | sin límite práctico |
| exFAT | USB de más de 32 GB | sin límite práctico |
| ReFS | Almacenamiento crítico | sin límite práctico |
| FAT32 | USB ≤ 32 GB, consolas, TV | 4 GB |
| FAT | Unidades de menos de 2 GB | 2 GB |

Preferencias e historial se guardan en `%AppData%\FormatDiskPro\`.

## Desarrollo

Requiere el .NET SDK 10 (e [Inno Setup 6](https://jrsoftware.org/isinfo.php) para el instalador).
[CONTRIBUTING.md](.github/CONTRIBUTING.md) explica qué se espera de un PR, y [CONTEXT.md](CONTEXT.md) la
arquitectura y el **porqué** de cada decisión.

```powershell
dotnet build -c Release                                      # el listón: 0 advertencias
dotnet test                                                  # unitarias (xUnit)
dotnet test tests\FormatDiskPro.UiTests --filter "Category!=Slow"   # UI (FlaUI), terminal ELEVADA
```

Las **pruebas de UI** conducen la app real y van **fuera de la solución**. Las que necesitan la USB de
pruebas se omiten si no está conectada. Las que borran datos (`FORMATDISKPRO_ALLOW_DESTRUCTIVE=1`) o la
desmontan a mitad de operación (`FORMATDISKPRO_ALLOW_YANK=1`) solo corren si se piden expresamente.

> `dotnet build` genera un ejecutable que necesita el runtime de escritorio de .NET 10. Para probar lo que
> se distribuye, usa el publish *self-contained* de `build-installer.ps1`.

### Integración continua

| Workflow | Cuándo | Qué hace |
|---|---|---|
| [**Compilación y unitarias**](.github/workflows/ci.yml) | push a `master`, cada PR, manual | Compila en Release sin advertencias, ejecuta las unitarias y **compila** las de UI sin ejecutarlas. El resumen dice qué no ejecutó y qué se omitió. |
| [**CodeQL**](.github/workflows/codeql.yml) | igual, más cada lunes | Análisis de seguridad del código de la app. |
| [**Dependabot**](.github/dependabot.yml) | mensual | Actualiza las acciones, fijadas por SHA. NuGet no: el Windows App SDK se sube a mano. |

Los cambios que solo tocan `*.md` o `docs/` no disparan los workflows. Un runner no tiene la USB de
pruebas ni puede elevar la app, así que **la puerta de publicación sigue siendo local**:
`release.ps1 -UiTests`.

### Instalador, publicación y capturas

| Tarea | Comando |
|---|---|
| Instalador + `.sha256` (en `installer\Output\`) | `src\FormatDiskPro\installer\build-installer.ps1` |
| Firmar (opcional) | `… -CertThumbprint A1B2…` o `… -CertFile cert.pfx -CertPassword (Read-Host -AsSecureString)` |
| Certificado autofirmado de prueba | `src\FormatDiskPro\installer\new-selfsigned-cert.ps1 [-Trust]` |
| Cortar una versión | `.\release.ps1 -Version X.Y.Z -UiTests` (`-DryRun` para ver el plan) |
| Regenerar capturas (terminal elevada) | `.\tools\capture-screenshots.ps1 -Gallery -Exe <publish>\FormatDiskPro.exe -Drive <USB>` |

`release.ps1` valida, prueba, compila el instalador, etiqueta, sube y crea el GitHub Release con el
`.exe` y su `.sha256`. Aborta si falta el `.sha256` o si `CHANGELOG.md` no tiene la sección de la versión.
Las capturas se toman del publish *self-contained* y sobre la USB de pruebas: en un disco fijo,
*Reinicializar* sale con su aviso de «solo extraíbles». La galería va a `docs/screenshots/gallery/`
(ignorada por git), y de ahí se copian a mano las del README. Todos los scripts documentan sus parámetros:
`Get-Help <script> -Detailed`.

### Arquitectura

| Capa | Contenido |
|---|---|
| `src/FormatDiskPro/Core/` | Lógica pura y testeable: comandos, parseos, umbrales, paleta de colores medida |
| `src/FormatDiskPro/Services/` | Efectos: PowerShell (siempre por `-EncodedCommand`), disco, red, ajustes, historial |
| `src/FormatDiskPro/UI/` | WinUI 3 (Windows App SDK 1.8, unpackaged, Mica) |
| `src/FormatDiskPro/Localization/` | Todo el texto de la interfaz, en los 5 idiomas |
| `tests/` | Unitarias (`FormatDiskPro.Tests`) y de UI (`FormatDiskPro.UiTests`) |

## Licencia, privacidad y contacto

- **[GPLv3](LICENSE)**: úsalo, modifícalo y redistribúyelo, con los derivados bajo la misma licencia. Sin
  garantía. Atribuciones en [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt), también en *Ayuda*.
- **Privacidad**: sin telemetría ni datos personales. La única conexión es a GitHub Releases, por HTTPS.
- **Errores y sugerencias**: en los [issues](https://github.com/xfiberex/FormatDiskPro/issues).
  **Vulnerabilidades**: en privado, según [SECURITY.md](.github/SECURITY.md).
- **Apoyar el proyecto**: donación voluntaria (PayPal) desde *Ayuda → Acerca de…*. Ninguna función es de
  pago.
