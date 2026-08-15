# Contribuir a FormatDiskPro

Gracias por el interés. Antes de escribir código, dos lecturas que ahorran tiempo:

- [`CONTEXT.md`](../CONTEXT.md) — arquitectura, **decisiones y por qué** (§4), y el registro de cambios.
  Casi todas las decisiones nacieron de un fallo real; leerlas evita «arreglar» algo que está así a
  propósito.
- [`ROADMAP.md`](../ROADMAP.md) — qué está hecho, qué queda y **qué está deliberadamente fuera de
  alcance**.

## Requisitos

- Windows 10 1809+ / Windows 11, x64.
- .NET SDK **10**.
- [Inno Setup 6](https://jrsoftware.org/isinfo.php) solo si vas a generar el instalador.

## Compilar y probar

```powershell
dotnet build -c Release          # 0 advertencias, 0 errores: es el listón
dotnet test                      # unitarias (Core + helpers de Services)

# Cobertura de Core/ (el corte de versión la exige ≥ 90 %)
dotnet test --collect:"XPlat Code Coverage"
```

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

> **No hay CI.** El testing de este proyecto es local, por decisión firme: la prueba que vale aquí es la
> que ejerce el binario real contra hardware real, y eso no cabe en un runner. No envíes PRs que añadan
> GitHub Actions o workflows. Si tu cambio necesita una comprobación automática, el sitio es `release.ps1`.

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

## Pull requests

1. Una idea por PR. Si toca varias cosas, sepáralas en commits (o en PRs).
2. `dotnet build -c Release` en **0/0** y `dotnet test` en verde. Si tocas UI, corre también los UI tests
   desde terminal elevada y di en el PR qué se omitió.
3. **Añade la prueba que falla sin tu arreglo.** En este proyecto se verifica por reversión: si no has
   visto tu test fallar, no es una red, es una suposición.
4. Si cambias comportamiento o una convención, actualiza [`CONTEXT.md`](../CONTEXT.md) (Estado actual +
   una entrada en el Registro de cambios, con fecha absoluta) **en el mismo commit**.
5. Mensajes de commit: `tipo(ámbito): qué cambia`, en imperativo (`fix(update): …`, `test(ui): …`).

## Qué no se va a aceptar

Está en el roadmap, pero por si acaso: creador de USB booteable desde ISO, gestor de particiones completo,
clonado/imagen de discos, ventana redimensionable, elevación `asInvoker` y CI. No es falta de ganas: cada
uno tiene su porqué escrito.

## Reportar problemas

Bugs y sugerencias, por los issues. Vulnerabilidades, **no**: mira [`SECURITY.md`](SECURITY.md).
