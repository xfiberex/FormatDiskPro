## Qué cambia y por qué

<!-- Si arregla un issue: "Cierra #N". Si viene del roadmap, di la tarea (p. ej. T3-01). -->

## Cómo se ha comprobado

<!--
Este proyecto verifica por REVERSIÓN: una prueba que nunca has visto fallar no es una red, es una
suposición. Di qué prueba falla sin tu arreglo.

El CI del PR («Compilación y unitarias») compila y ejecuta las unitarias, pero las pruebas de UI solo las
COMPILA: si tu cambio toca la UI, ejecútalas en tu máquina.
-->

- [ ] `dotnet build -c Release` → **0 advertencias / 0 errores**
- [ ] `dotnet test` en verde
- [ ] UI tests (si toca la UI), desde **terminal elevada**: indica cuántos se omitieron y por qué
- [ ] Probado sobre hardware real: <!-- tipo de unidad, o "no aplica" -->

## Documentación

- [ ] `CONTEXT.md` actualizado (Estado actual + entrada en el Registro de cambios, con fecha absoluta) si
      cambia comportamiento, una convención o una decisión
- [ ] `ROADMAP.md` actualizado si cierra una tarea

## Alcance

- [ ] Si toca `.github/workflows/`, explica por qué (se ejecutan con los permisos del repositorio)
- [ ] No entra en lo que está deliberadamente fuera de alcance (ver `ROADMAP.md`)
- [ ] Todo el texto nuevo de cara al usuario está en `Localization`, con sus 5 traducciones
