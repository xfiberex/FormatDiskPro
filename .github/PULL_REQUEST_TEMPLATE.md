## Qué cambia y por qué

<!-- Si arregla un issue: "Cierra #N". Si viene del roadmap, di la tarea (p. ej. T3-01). -->

## Cómo se ha comprobado

<!--
Este proyecto verifica por REVERSIÓN: una prueba que nunca has visto fallar no es una red, es una
suposición. Di qué prueba falla sin tu arreglo.
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

- [ ] No añade GitHub Actions ni workflows (el testing de este proyecto es local, por decisión)
- [ ] No entra en lo que está deliberadamente fuera de alcance (ver `ROADMAP.md`)
- [ ] Todo el texto nuevo de cara al usuario está en `Localization`, con sus 5 traducciones
