# Progreso · auditoría 2026-10-11

Punto de control. Si la sesión se corta, se retoma desde aquí.

## Alcance acordado (Fase 0, confirmado por el usuario el 2026-10-11)

- **Commit auditado:** `72f29022bac5fc602f425de1e231045ce9994593` · rama `master` · árbol limpio · v1.29.0 (2026-09-22).
- **Tipo:** re-auditoría. Pasadas previas: 2026-08-13 (12 áreas), 2026-08-26 (transversal, Tier 9), 2026-09-18 (UI/UX medida, Tier 13). Ninguna dejó `INFORME.md`: no hay notas anteriores.
- **Alcance:** repo completo · **profundidad exhaustiva**, sin muestreo (21.130 líneas de código).
- **Áreas incluidas (12):** 1, 2, 3, 5, 6, 7, 8, 9, 10, 11, 12, 13.
- **Áreas excluidas:** 4 SEO (escritorio, sin superficie web) · 14 IA (el producto no integra modelos; la configuración de agentes del repo se revisa en 2 y 12).
- **Zonas excluidas:** `.agents/skills/` (terceros; solo procedencia), `LICENSE`, `bin/`, `obj/`, `.codegraph/`.
- **Contexto:** utilidad gratuita, público general, distribución solo por GitHub Releases, Windows 10 1809+ x64, informe en español. **Jurisdicción no indicada** → área 13 solo núcleo común (zona no cubierta).
- **IDs:** se conserva `T{tier}-{n}` y los tiers temáticos; lo nuevo entra como `AUD-001…` en sección propia por severidad.
- **Subagentes:** `auditor-area` en paralelo para la estática; `verificador-hallazgos` para Crítico/Alto.
- **Carpeta:** versionar `INFORME.md` y `evidencias/`; ignorar `trabajo/` (cambio de `.gitignore` propuesto, no aplicado en Fase 1). Sin commits en Fase 1.

## Verificación en ejecución (acordada)

- Terminal **elevada** (comprobado 10:52). Sesión interactiva.
- Build + unitarias con cobertura: sí.
- App real: suite de UI (`--filter "Category!=Slow"`) + galería en ambos temas sobre publish self-contained en `%TEMP%`; arranque y memoria.
- **Destructivo AUTORIZADO expresamente por el usuario, solo sobre la USB de pruebas vacía:**
  disco físico **6** (`VendorCo ProductCode`, USB, 29,3 GB, MBR) = **G:** (FAT32 1 GB, `UTILIDADES`) + **I:** (exFAT 28,3 GB). Ambas vacías (comprobado).
  **Ningún otro disco.** Discos fijos con datos: C (sistema), D, E, F, H, R → jamás destino. Reidentificar el disco 6 antes de cada operación destructiva (las letras pueden cambiar tras reinicializar).
- `release.ps1 -DryRun`: sí, tras leerlo. Nunca sin `-DryRun`.
- GitHub solo lectura con `gh`: sí.
- Herramientas: `dotnet list package --vulnerable`; gitleaks portable en scratchpad (se borra al terminar); alertas CodeQL vía `gh`; sin Semgrep.

## Decisiones cerradas que NO se reabren

Siempre elevada · ventana fija 500×900 · instalador sin firmar (`#13`/`T4-03`) · pruebas de UI locales (`T2-10`) · *Reinicializar* no es gestor de particiones (`T5-04`) · fuera de alcance: USB booteable, gestor de particiones, clonado.

## Estado de las áreas

| Área | Estática | Ejecución | Archivo |
|---|---|---|---|
| 1 Código | pendiente | — | `hallazgos/01-codigo.md` |
| 2 Seguridad | pendiente | pendiente | `hallazgos/02-seguridad.md` |
| 3 Rendimiento | pendiente | pendiente | `hallazgos/03-rendimiento.md` |
| 5 Accesibilidad | pendiente | pendiente | `hallazgos/05-accesibilidad.md` |
| 6 UI/UX | pendiente | pendiente | `hallazgos/06-ui-ux.md` |
| 7 Arquitectura | pendiente | pendiente | `hallazgos/07-arquitectura.md` |
| 8 QA | pendiente | pendiente | `hallazgos/08-qa-testing.md` |
| 9 Refactorización | pendiente | — | `hallazgos/09-refactorizacion.md` |
| 10 Redacción | pendiente | — | `hallazgos/10-redaccion.md` |
| 11 Documentación | pendiente | — | `hallazgos/11-documentacion.md` |
| 12 DevOps | pendiente | pendiente | `hallazgos/12-devops.md` |
| 13 Legal | pendiente | — | `hallazgos/13-legal.md` |

## Ejecución ya hecha (paso 1.3) — NO repetir salvo motivo

Todo sobre el publish self-contained de la v1.29.0 en `%TEMP%\FormatDiskPro-audit-publish\` (variable `FORMATDISKPRO_EXE`).

| Qué | Resultado | Evidencia |
|---|---|---|
| Build Release `-warnaserror` | 0 advertencias / 0 errores (21,9 s) | `linea-base.md` |
| Unitarias + cobertura | 690: 689 pasan, 0 fallan, 1 omitida · líneas 32,67 %, ramas 34,05 % (ensamblado entero) | `evidencias/unitarias.trx` |
| SCA `dotnet list package --vulnerable --include-transitive` | sin vulnerables (app, Tests, UiTests) | `linea-base.md` |
| Suite de UI `--filter "Category!=Slow"` | 43: 40 pasan, 3 omitidas (opt-in), 0 fallan · 1 m 45 s | `evidencias/ui-suite.trx` |
| Destructivas (`ALLOW_DESTRUCTIVE=1`, `ALLOW_YANK=1`) | 3/3 pasan · 2 m 15 s · particiones de TODOS los discos idénticas antes/después | `evidencias/ui-destructivas.trx`, `discos-antes.txt`, `discos-despues.txt` |
| Galería ES, claro + oscuro | 28/28 tomas | `evidencias/galeria-es/` |
| Galería FR/PT/IT/EN, claro (7 tomas c/u) y FR a 656 DIP | completas | `evidencias/galeria-{fr,pt,it,en,fr-656}/` |
| Arranque en reposo (5 arranques) | ventana 586/513/502/515/516 ms (media templada 512) · utilizable 642 ms · WS 185 MB · privada 121 MB · publish 228,7 MB / 519 archivos | `trabajo/scripts/medir-arranque.ps1` |
| GitHub solo lectura (`gh`) | ver «Observado en GitHub» abajo | — |
| Instalador publicado v1.29.0 | SHA-256 coincide con su `.sha256` · `NotSigned` (decisión cerrada) | — |
| Ajustes del usuario (`%AppData%\FormatDiskPro`) | respaldo propio en la carpeta temporal de la sesión; `settings.json` idéntico tras las pruebas | — |

**Observado en GitHub (2026-10-11):** repo público, 0 estrellas, 63 descargas de instaladores en total · `master` **sin protección de rama ni rulesets** · **reporte privado de vulnerabilidades desactivado** (y `SECURITY.md` lo da como canal principal) · alertas de Dependabot desactivadas, *security updates* desactivadas · secret scanning + push protection activados · 0 alertas de CodeQL abiertas · últimas ejecuciones de CI y CodeQL en verde · PR #2 de Dependabot (acciones) abierto desde el 2026-10-01.

**Segunda tanda de ejecución (2026-10-11, 22:10–22:40, terminal elevada comprobada):**

| Qué | Resultado | Evidencia |
|---|---|---|
| Árbol de UIA de la ventana principal | 49 elementos; único interactivo sin nombre: `DrivePicker`; sus 8 ítems se llaman «FormatDiskPro.UI.DriveViewModel» → **P-05-01 confirmado** | `evidencias/uia-arbol-principal.txt` |
| Orden de tabulación (40 pulsaciones) | ciclo coherente: selector → actualizar → presets → FS → clúster → etiqueta → opciones → Cerrar → Formatear → menú → Salud/Benchmark/Historial | `evidencias/orden-tabulacion.txt` |
| `settings.json` con `"UserPresets": null` | la app **muere al arrancar** (código 0xC000027B), sin ventana ni mensaje; `CRASH: NullReferenceException … FillPresets` en el historial. Con `Language` o `Theme` a `null` arranca bien | `evidencias/arranque-presets-null-historial.txt` |
| Format-Volume que falla | stderr = 1.682 caracteres de **CLIXML** (`#< CLIXML <Objs …`), que es lo que la app pone en el diálogo de error | `evidencias/format-volume-fallido-stderr.txt` |
| `chkdsk G: /f` con un archivo abierto, una sola «N», entrada sin cerrar | **no termina** (30 s); sale al cerrar la entrada (código 3). Hay dos preguntas: desmontar y programar para el reinicio → **P-01-08 confirmado** | `evidencias/chkdsk-reparar-unidad-en-uso.txt` |
| Cancelar un formato completo exFAT (`Format-Volume -Full`) a los 4 s | el formato **siguió y terminó** a los 50 s con el árbol de PowerShell ya muerto → **C-07-B confirmado** | `evidencias/cancelar-format-volume.txt` |
| Cancelar un formato completo NTFS (`format.com`) a los 5 s | se detiene; la unidad queda **sin sistema de archivos** (`IsReady=False`) | salida en esta bitácora |
| Expulsar con un archivo abierto | PowerShell sale con 0 en 0,5 s y la unidad sigue montada → la app diría «expulsada» → **P-01-06 confirmado** | salida en esta bitácora |
| Expulsar sin nada abierto | funciona (volúmenes desmontados a los 3 s). **La USB quedó expulsada: hay que volver a enchufarla** | — |
| Cierre por `WM_CLOSE` y `taskkill` sin `/F` | cierra bien (4 de 4, con y sin carga de CPU) | `trabajo/scripts/cierre-wmclose.ps1` |
| gitleaks 8.30.1 portable (suma verificada) | historial: 153 commits, **0 secretos**. Árbol: 6 avisos, todos en `obj/**/MsixContent/*_license.xml` (salida de compilación, no versionada): falsos positivos | `trabajo/gitleaks/*.json` |
| `release.ps1 -Version 1.29.1 -DryRun` | aborta: falta la sección del CHANGELOG (guarda correcta) | — |
| `release.ps1 -DryRun` (1.29.0) | aborta: el tag ya existe (guarda correcta). Árbol intacto | — |

**Artefacto descartado (no es un hallazgo):** entre las 17:07 y las 17:12 la app «no se cerraba» con `WM_CLOSE` y el arranque medía 1,7–2,4 s. Esa sesión no se comprobó elevada y todo apunta a que no lo estaba: UIPI bloquea mensajes y UIA hacia una ventana elevada, y el arranque incluía el UAC. Con la terminal elevada no se reproduce. **Lección: comprobar la elevación al empezar cada sesión.**

**No se hizo y se declara:** intercambio de letras con el diálogo de confirmación abierto (C-07-A queda en estática, confianza Alta) · alto contraste y escalado del sistema (no se cambió la configuración de Windows del usuario) · Narrador (se sustituye por el volcado de UIA) · la fase de pruebas y plan de `release.ps1 -DryRun` (exige editar `CHANGELOG.md`, vedado en Fase 1) · medición del benchmark con y sin contexto de UI.

**Estado de la USB al terminar:** como se encontró (disco 6: G: FAT32 `UTILIDADES` 1 GB vacía; I: exFAT 28,3 GB vacía), **expulsada** por la última prueba.

## Subagentes (paso 1.2)

Lanzados 10:55; cortados por límite de sesión sin escribir nada. Reanudados 17:06 (escribieron parciales 17:08–17:11) y otra vez 17:20 tras cerrarse la sesión. Se reanudan con SendMessage al mismo ID (conservan contexto).

| Áreas | ID del agente | Archivos | Estado a las 17:20 |
|---|---|---|---|
| 1 + 9 | `a65de6765030a3b9b` | `01-codigo.md`, `09-refactorizacion.md` | parcial (19 + 8 candidatos) |
| 2 | `a2de14175e120eb15` | `02-seguridad.md` | parcial (4; P-02-01 candidato Alto) |
| 3 + 7 | `a416c114f45ddbc35` | `03-rendimiento.md`, `07-arquitectura.md` | sin escribir |
| 5 + 6 | `a68efad634c36c814` | `05-accesibilidad.md`, `06-ui-ux.md` | parcial (2 + 2; P-05-01 candidato Alto) |
| 8 | `adfa6b13a15f85af4` | `08-qa-testing.md` | sin escribir |
| 10 + 11 | `acd73712bf02e4683` | `10-redaccion.md`, `11-documentacion.md` | parcial (10 + 8) |
| 12 + 13 | `a0acd25419460dc59` | `12-devops.md`, `13-legal.md` | sin escribir |

## FASE 1 TERMINADA (2026-10-11, ~23:30)

- **Cambio de táctica a las 22:07:** tras el tercer corte por límite de sesión, la revisión estática la terminó el coordinador en la sesión principal (áreas 3, 7 y 8 suyas; el resto, verificando cada candidato de los subagentes). Ver `hallazgos/00-coordinador.md` y `hallazgos/03-07-08-coordinador.md`.
- **Consolidación:** 71 hallazgos, `AUD-001` a `AUD-071`: **0 críticos · 2 altos · 22 medios · 47 bajos**. 65 nuevos, 4 ya conocidos con cierre incompleto (`T6-12`, `T9-17`, `T9-18`, `T9-19`), 2 regresiones (`T9-14`/`T9-15`, `T2-08`).
- **Verificación independiente** (`verificador-hallazgos`, un solo agente): AUD-001 confirmado (Alto), AUD-002 **rebajado a Medio**, AUD-003 confirmado (Alto). Sus reservas, aplicadas.
- **Entregables:** `INFORME.md` · `ROADMAP.md` (Parte 3 al final, fila nueva en la tabla de partes, párrafo nuevo en Estado) · `CONTEXT.md` (fila en la cabecera y en §3, §8 *Falsos positivos verificados*, entrada del registro). `CHANGELOG.md` sin tocar: no hay cambios hechos que registrar.
- **Autoverificación 1.6:** IDs del informe = IDs del ROADMAP (71 = 71); totales por tier cuadran (2/22/47); sin patrones de secretos; sin fechas relativas; enlaces y evidencias citadas existen; `git status` solo muestra `docs/auditoria/`, `ROADMAP.md` y `CONTEXT.md`; código, pruebas, scripts, README y CHANGELOG sin tocar.
- **Limpieza:** borrados el publish de `%TEMP%`, gitleaks portable y el instalador descargado. Ajustes e historial del usuario idénticos al respaldo. USB como se encontró, expulsada.
- **Sin hacer en Fase 1 (propuestas para el usuario):** `.gitignore` para `docs/auditoria/*/trabajo/`; commits; decidir qué tareas pasan a la Fase 2.

**Para regenerar la Parte 3 del ROADMAP desde el informe:** `node trabajo/scripts/roadmap-gen.js INFORME.md salida.md` (misma fuente para título, ubicación, qué hacer y criterio).

## Bitácora

- 2026-10-11 10:40 — Fase 0: línea base de inventario, lectura de CONTEXT §1–7 y ROADMAP (estado, decisiones, áreas sin hallazgos).
- 2026-10-11 10:52 — Alcance confirmado. Carpeta creada.
- 2026-10-11 10:53 — Línea base (build, unitarias, cobertura, SCA). 7 auditores lanzados.
- 2026-10-11 10:57–11:10 — Suite de UI, destructivas y galerías.
- 2026-10-11 ~11:xx — Los 7 auditores se cortan por límite de sesión (reinicio a las 15:40) sin escribir nada.
- 2026-10-11 17:06 — Reanudados. GitHub en solo lectura; verificación del instalador publicado; primeras pruebas de cierre.
- 2026-10-11 17:12 — Se cierra la sesión; los auditores quedan parados con parciales en disco.
- 2026-10-11 17:14 — Sesión nueva: entorno recomprobado (elevada, USB en disco 6, sin procesos colgados). Arranque remedido en reposo. Auditores reanudados (17:20).
