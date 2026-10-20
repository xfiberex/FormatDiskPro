# Contribuir a FormatDiskPro

Gracias por el interés. Antes de escribir código, dos lecturas que ahorran tiempo:

- [`CONTEXT.md`](../CONTEXT.md) — arquitectura y **decisiones y por qué** (§4). Casi todas las decisiones
  nacieron de un fallo real; leerlas evita «arreglar» algo que está así a propósito. El registro razonado
  de cada cambio está en [`docs/contexto-historico.md`](../docs/contexto-historico.md).
- [`ROADMAP.md`](../ROADMAP.md) — qué queda por hacer y **qué está deliberadamente fuera de alcance**. Lo
  ya hecho está en [`docs/roadmap-historico.md`](../docs/roadmap-historico.md).

## Requisitos

- Windows 10 versión 2004 (compilación 19041) o posterior, o Windows 11; x64. Es el mínimo que declara
  `SupportedOSPlatformVersion` en el `.csproj`, y el instalador lo exige con `MinVersion`.
- .NET SDK **10.0.400** o un parche posterior de esa banda (10.0.4xx): lo fija [`global.json`](../global.json).
  El SDK decide qué runtime de .NET va dentro del instalador, y por eso no se deja flotar.
- [Inno Setup 6](https://jrsoftware.org/isinfo.php) solo si vas a generar el instalador.

## Compilar y probar

```powershell
dotnet build -c Release          # 0 advertencias, 0 errores: es el listón
dotnet test                      # unitarias (Core + helpers de Services)

# Cobertura de Core/ (el corte de versión la exige ≥ 90 %)
dotnet test --collect:"XPlat Code Coverage"
```

**Si cambias un paquete NuGet**, el `packages.lock.json` de su proyecto cambia con él al restaurar: súbelo en el
mismo commit. El CI y el corte restauran en modo bloqueado y fallan con `NU1004` si no corresponde. Los paquetes
solo vienen de nuget.org ([`nuget.config`](../nuget.config)).

**Las pruebas de UI conducen la app real y exigen TERMINAL ELEVADA** — la app es `requireAdministrator` y
un proceso sin elevar no puede automatizar su ventana:

```powershell
dotnet test tests\FormatDiskPro.UiTests --filter "Category!=Slow"
```

Van **fuera de la solución a propósito**, para que `dotnet test` no las arrastre. Las que necesitan una USB
de pruebas (partición extraíble etiquetada `utilidades`) **se omiten** si no está conectada: precondición
ausente no es un fallo. Las que borran datos o desmontan la unidad solo corren con su variable de entorno
(`FORMATDISKPRO_ALLOW_DESTRUCTIVE=1`, `FORMATDISKPRO_ALLOW_YANK=1`) — **no las actives sobre una unidad que
te importe**.

> **Qué hace el CI, y qué no.** Cada PR ejecuta en GitHub Actions el check *Compilación y unitarias*:
> compila en Release sin advertencias, ejecuta las unitarias y **compila** las pruebas de UI, pero **no las
> ejecuta**: un runner no tiene la USB de pruebas, y la prueba que vale aquí es la que ejerce el binario real
> contra hardware real. Su resumen dice lo que no ejecutó. Si tu PR toca la UI, sigue haciendo falta correr
> las pruebas de UI en tu máquina y decir en el PR cuáles se omitieron. La puerta de publicación es
> `release.ps1 -UiTests`, en local.
>
> Se revisan con especial cuidado los cambios en `.github/workflows/`, que se ejecutan con los permisos del
> repositorio, y en la **configuración de agentes de código**: `.mcp.json`, `.claude/`, `.agents/skills/` y
> `skills-lock.json`. Lo que digan esos archivos lo ejecuta u obedece el agente del mantenedor al abrir el
> proyecto. Un PR que los toque queda señalado por el workflow *Configuración de agentes*, que además
> comprueba que cada `SKILL.md` es el que fija `skills-lock.json`.

### Herramientas de agentes (opcionales)

Nada de esto hace falta para compilar ni para contribuir; solo lo usa quien trabaja con un agente de código.

- **`codegraph`**: índice del código que el agente consulta por MCP (`.mcp.json` lo arranca con `codegraph
  serve --mcp`). Es el paquete npm [`@colbymchenry/codegraph`](https://github.com/colbymchenry/codegraph),
  licencia MIT: `npm install -g @colbymchenry/codegraph`. Se resuelve por el `PATH`, así que es el que tengas
  instalado; el índice (`.codegraph/`) es local y no se versiona.
- **Skills** (`.agents/skills/`): nueve guías de buenas prácticas de C#/.NET copiadas de
  [`github/awesome-copilot`](https://github.com/github/awesome-copilot), bajo su licencia MIT
  ([`.agents/skills/LICENSE`](../.agents/skills/LICENSE)), no bajo la GPLv3 del proyecto. Su hash está en
  `skills-lock.json`; `tools/check-skills.ps1` lo comprueba.

## Cómo está organizado

| Capa | Regla |
|---|---|
| `Core/` | Lógica **pura y testeable**. Sin WinUI, sin `Process`, sin `HttpClient`. |
| `Services/` | Efectos colaterales: procesos, disco, red. |
| `UI/` | WinUI 3. |
| `Localization/` | **Todo** el texto de cara al usuario, en los 5 idiomas. |

Convenciones que hay tests vigilando —y que fallarán si las saltas—:

- **Nada de texto de UI fuera de `Localization`**, y las 5 traducciones completas (ES/EN/PT/FR/IT).
- **Los colores semánticos viven en `Core/SeverityPalette`** y se les mide el contraste WCAG. Añadir un
  color al inventario es ponerlo bajo test.
- **PowerShell siempre por `-EncodedCommand`**; letras de unidad validadas antes de interpolar.
- Documentación XML (`/// <summary>`) en lo público, en español, explicando **por qué** cuando no sea obvio.

## Textos de la interfaz: glosario por idioma

Un término para cada cosa y un mismo tratamiento en todo el idioma (`AUD-048`). La tabla la lee
`GlossaryTests`: cada término de la columna **Descartado** de un idioma hace fallar esa prueba si aparece en
sus textos de `Localization.cs` (sin distinguir mayúsculas; las palabras, enteras). Para cambiar
de criterio se cambia aquí, y la prueba lo sigue.

<!-- glosario:inicio -->
| Idioma | Se usa | Descartado |
|---|---|---|
| ES | Tuteo: «Selecciona», «Revisa», «Espera», «Asegúrate» | `Seleccione` · `Actualice` · `Asegúrese` · `Revise` · `Utilice` · `Espere` · `escriba` · `Vuelva` · `cambie` · `descargue` · `Bórrela` · `Sustitúyala` |
| ES | «extraíble», «disco del sistema», «clúster», «tamaño de unidad de asignación», «preset» | `removible` · `removibles` · `disco de sistema` · `cluster` |
| EN | Inglés de EE. UU., como Windows: «canceled», «favors», «license»; comillas “ ” | `cancelled` · `favours` · `licence` · `colour` · `behaviour` |
| PT | Português do Brasil, «você»: «predefinição» (no «preset»), «unidade» | `preset` · `presets` |
| FR | «nom de volume», como Windows; unidades en octetos: «Ko», «Mo», «Go», «To» | `étiquette de volume` · `KB` · `MB` · `GB` · `TB` |
| IT | «Visualizza» | `Vedi` |
<!-- glosario:fin -->

Además, sin prueba que lo vigile:

- **Signo de porcentaje:** con espacio (`{0} %`) en español, portugués, francés e italiano; pegado (`{0}%`)
  en inglés.
- **Puntos suspensivos** (`…`, un solo carácter) en todo ítem de menú que abre un diálogo.
- **Incisos:** la raya pegada al texto que encierra (`—así—`) es del español y del inglés; en portugués,
  francés e italiano se usan paréntesis.
- **Comillas:** angulares («») en español, portugués, francés e italiano; tipográficas (“ ”) en inglés.
- **Unidades:** las de `FormatBytes` salen de `unit.bytes`; no se escriben a mano en el código.

## Pull requests

1. Una idea por PR. Si toca varias cosas, sepáralas en commits (o en PRs).
2. `dotnet build -c Release` en **0/0** y `dotnet test` en verde; el CI del PR lo comprueba también. Si
   tocas UI, corre además los UI tests desde terminal elevada y di en el PR qué se omitió: el CI solo los
   compila.
3. **Añade la prueba que falla sin tu arreglo.** En este proyecto se verifica por reversión: si no has
   visto tu test fallar, no es una red, es una suposición.
4. Si cambias comportamiento o una convención, actualiza [`CONTEXT.md`](../CONTEXT.md) (§3 o §4, lo que
   toque) y añade una entrada, con fecha absoluta, arriba en
   [`docs/contexto-historico.md`](../docs/contexto-historico.md), **en el mismo commit**. Si cierras una
   tarea del `ROADMAP.md`, muévela a [`docs/roadmap-historico.md`](../docs/roadmap-historico.md).
5. Mensajes de commit: `tipo(ámbito): qué cambia`, en imperativo (`fix(update): …`, `test(ui): …`).

## Qué no se va a aceptar

Está en el roadmap, pero por si acaso: creador de USB booteable desde ISO, gestor de particiones completo,
clonado/imagen de discos, ventana redimensionable y elevación `asInvoker`. No es falta de ganas: cada uno
tiene su porqué escrito.

## Reportar problemas

Bugs y sugerencias, por los issues. Vulnerabilidades, **no**: mira [`SECURITY.md`](SECURITY.md).
