<!-- CODEGRAPH_START -->
## CodeGraph

In repositories indexed by CodeGraph (a `.codegraph/` directory exists at the repo root), reach for it BEFORE grep/find or reading files when you need to understand or locate code:

- **MCP tools** (when available): `codegraph_explore` answers most code questions in one call — the relevant symbols' verbatim source plus the call paths between them. `codegraph_node` returns one symbol's source + callers, or reads a whole file with line numbers. If the tools are listed but deferred, load them by name via tool search.
- **Shell** (always works): `codegraph explore "<symbol names or question>"` and `codegraph node <symbol-or-file>` print the same output.

If there is no `.codegraph/` directory, skip CodeGraph entirely — indexing is the user's decision.
<!-- CODEGRAPH_END -->

## Contexto del proyecto

Lee [`CONTEXT.md`](../CONTEXT.md) (raíz del repo) al iniciar una sesión: resume la arquitectura,
las decisiones y convenciones y el estado actual. Las tareas abiertas están en
[`ROADMAP.md`](../ROADMAP.md), que **solo** contiene lo pendiente.

**Mantenlos al día** tras cada cambio relevante, en el mismo commit que el cambio:

- `CONTEXT.md`: _Estado actual_ (§3) y, si cambia una decisión o convención, §4. Debe seguir por debajo
  de 500 líneas.
- [`docs/contexto-historico.md`](../docs/contexto-historico.md): una entrada nueva **arriba**, con fecha
  absoluta, con el qué y el porqué.
- Al completar una tarea del `ROADMAP.md`, **muévela** a
  [`docs/roadmap-historico.md`](../docs/roadmap-historico.md) (bloque marcado `[x]`, línea
  **Hecho (fecha)** y fila en su tabla) y actualiza las cifras de _Estado_ del `ROADMAP.md`.

Los dos archivos de `docs/` son historia: no hace falta leerlos al empezar, solo buscar en ellos cuando
haga falta el porqué de algo ya cerrado.

## Skills del proyecto (`.agents/skills/`)

El repo incluye skills de buenas prácticas C#/.NET (registro `github/awesome-copilot`).
**Consúltalas antes del trabajo correspondiente** (léelas en `.agents/skills/<nombre>/SKILL.md`,
o invócalas con la herramienta Skill si están disponibles en la sesión):

| Cuándo | Skill |
|--------|-------|
| Escribir/editar **pruebas** | `csharp-xunit` — el proyecto usa **xUnit** |
| Código **async/await** (formateo, descargas, verificación de capacidad) | `csharp-async` |
| **Documentación XML** (`/// <summary>`) | `csharp-docs` |
| Cualquier código C#/.NET nuevo o refactor | `dotnet-best-practices` |
| Revisión de **patrones de diseño** | `dotnet-design-pattern-review` |
| **Actualizar la versión** del framework (.NET 10 → futuras) | `dotnet-upgrade` |

> **Pruebas:** el framework del proyecto es **xUnit** (`csharp-xunit`). Las skills `csharp-mstest`,
> `csharp-nunit` y `csharp-tunit` también están presentes pero **no se usan aquí**; no cambies de
> framework de pruebas salvo que el usuario lo pida.
