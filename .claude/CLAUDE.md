<!-- CODEGRAPH_START -->
## CodeGraph

In repositories indexed by CodeGraph (a `.codegraph/` directory exists at the repo root), reach for it BEFORE grep/find or reading files when you need to understand or locate code:

- **MCP tools** (when available): `codegraph_explore` answers most code questions in one call — the relevant symbols' verbatim source plus the call paths between them. `codegraph_node` returns one symbol's source + callers, or reads a whole file with line numbers. If the tools are listed but deferred, load them by name via tool search.
- **Shell** (always works): `codegraph explore "<symbol names or question>"` and `codegraph node <symbol-or-file>` print the same output.

If there is no `.codegraph/` directory, skip CodeGraph entirely — indexing is the user's decision.
<!-- CODEGRAPH_END -->

## Contexto del proyecto

Lee [`CONTEXT.md`](../CONTEXT.md) (raíz del repo) al iniciar una sesión: resume la arquitectura,
las decisiones y convenciones, el estado actual y el registro de cambios. **Mantenlo actualizado**
tras cada cambio relevante (sección _Estado actual_ + nueva entrada en _Registro de cambios_,
con fecha absoluta), y commitéalo junto con el cambio para conservar el contexto entre equipos.
