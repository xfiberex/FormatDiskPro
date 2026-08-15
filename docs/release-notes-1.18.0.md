## FormatDiskPro v1.18.0

Versión de **calidad**: no añade funciones, endurece las que ya había. Tercera tanda de la auditoría
técnica del 2026-08-13 (22 de 40 puntos resueltos).

### Accesibilidad

- **Las operaciones se pueden seguir con un lector de pantalla.** La barra de estado es ahora una región
  activa y los **hitos** (inicio, fin, error, cancelación) se anuncian, sin mover el foco. El avance
  porcentual **no** se anuncia a propósito: durante un formateo de una hora sería ruido continuo.
- **El error de la etiqueta de volumen se lee desde el propio campo**: antes aparecía debajo, sin ninguna
  relación programática, y no había forma de saber por qué no dejaba continuar.

### Actualizaciones más seguras

- El hash **SHA-256** que se comprueba antes de ejecutar el instalador es ahora, con certeza, **el del
  instalador que se va a ejecutar**: se empareja por nombre en vez de tomar el primer `.sha256` que
  aparezca en el release.
- La descarga de ese hash tiene **tope de tamaño**: un checksum ocupa 64 caracteres, no se leen respuestas
  arbitrarias en memoria.

### Historial

- `history.log` **rota** al llegar a 2 MB (`history.1.log`): deja de crecer sin fin. El visor sigue
  mostrando las dos generaciones, así que rotar no vacía lo que ves, y *Borrar el historial* se las lleva
  las dos.

### Interno

- El script de corte de versión informa de **cuántas pruebas de UI se omitieron y por qué**: un release ya
  no puede salir "en verde" ocultando la cobertura que no llegó a ejercerse.

Instalador self-contained para Windows x64 (no requiere instalar .NET). Descarga
`FormatDiskPro-1.18.0-setup.exe` y ejecútalo (requiere privilegios de administrador).

El asset `FormatDiskPro-1.18.0-setup.exe.sha256` es el hash con el que la app verifica la descarga antes
de ejecutarla.
