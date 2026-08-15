# Política de seguridad

FormatDiskPro **formatea, borra y reinicializa unidades**, corre **siempre con permisos de administrador**
y se **auto-actualiza** desde GitHub Releases. Cualquiera de esas tres cosas es un buen motivo para
tomarse en serio un fallo de seguridad.

## Versiones con soporte

| Versión | Soporte |
|---|---|
| Última publicada | ✅ Sí |
| Anteriores | ❌ No — actualiza antes de reportar |

Solo hay una línea de desarrollo. Un arreglo de seguridad sale en la siguiente versión publicada.

## Cómo reportar una vulnerabilidad

**No abras un issue público** con los detalles.

Usa el reporte privado de GitHub: pestaña **Security → Report a vulnerability**
([enlace directo](https://github.com/xfiberex/FormatDiskPro/security/advisories/new)). Queda visible solo
para el mantenedor hasta que haya arreglo.

Si esa vía no te funciona, abre un issue **sin detalles técnicos** (algo como «he encontrado un problema
de seguridad, ¿por dónde te lo cuento?») y se te indicará un canal privado.

Ayuda mucho incluir:

- Versión de FormatDiskPro (*Ayuda → Acerca de*) y de Windows.
- Qué esperabas que pasara y qué pasó.
- Pasos para reproducirlo, aunque sean aproximados.
- Si aplica, el `history.log` (`%AppData%\FormatDiskPro\history.log`) — **revísalo antes**: registra
  letras y etiquetas de unidad.

## Qué esperar

Es un proyecto mantenido por una sola persona en su tiempo libre: no hay acuerdos de nivel de servicio,
pero sí compromiso de leer todo lo que llegue por esta vía, responder en cuanto sea posible y darte
crédito en el aviso público si quieres.

## Lo que ya está decidido (para ahorrarte el reporte)

Estas no son vulnerabilidades, son decisiones documentadas en [`CONTEXT.md`](../CONTEXT.md) §4:

- **La app pide administrador al arrancar** (`requireAdministrator`). Casi todo lo que hace lo necesita;
  el manifiesto lo declara en vez de escalar por sorpresa.
- **El instalador no está firmado**, así que SmartScreen dice «editor desconocido». Por eso la
  auto-actualización **verifica el SHA-256** publicado como asset del release **antes** de ejecutar nada:
  sin hash, el instalador descargado se borra y no se ejecuta.
- **Alcance de esa verificación:** el `.exe` y su hash salen del mismo release, así que detecta corrupción
  y manipulación **en tránsito**, no un compromiso de la cuenta de GitHub. Es el compromiso habitual de un
  proyecto sin certificado.
- **Sin telemetría.** La única conexión de red es a la API de GitHub para buscar actualizaciones.

Lo que **sí** interesa: cualquier forma de que un tercero consiga ejecución con los permisos de la app,
saltarse la verificación del instalador, hacer que se formatee algo que el usuario no pidió, o que la
guarda del disco de sistema no proteja.
