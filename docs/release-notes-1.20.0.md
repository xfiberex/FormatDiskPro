## FormatDiskPro v1.20.0

Cierra el **Tier 3** de la auditoría técnica (35 de 40 puntos resueltos; los cinco restantes son mejoras
de proceso, no defectos). Pulido: cosas pequeñas que, cuando fallaban, fallaban en silencio.

### La exportación del historial ya no falla sin decirlo

Al exportar el historial a CSV, cualquier error —carpeta protegida, archivo abierto en Excel, disco
lleno— se descartaba sin más: elegías el destino, no pasaba nada visible y te quedabas **creyendo que
tenías tu archivo**. Ahora el motivo real aparece dentro del propio diálogo y queda anotado en el
historial.

### La salud de la unidad, cuando no se puede leer

Si la consulta S.M.A.R.T. falla, la ficha lo dice ("no disponible") en vez de dejar el dato a medias, y
el fallo queda registrado.

### El borrado seguro usa aleatoriedad criptográfica

La pasada de datos aleatorios ahora se genera con el generador criptográfico del sistema. Para destruir
datos el resultado es equivalente, pero «borrado seguro» no debe apoyarse en un generador que no lo es.

### Preferencias editadas a mano

Un `settings.json` con valores imposibles (0 pasadas de borrado, un tamaño FAT32 no admitido) entraba tal
cual y la interfaz lo tapaba eligiendo otra cosa. Ahora se corrigen al cargarlas.

### Accesibilidad y estabilidad

- Los **iconos decorativos** salen del árbol de accesibilidad: el lector de pantalla ya no los anuncia
  entre los controles reales.
- Un **marcador mal escrito en una traducción** ya no puede tumbar una pantalla: se muestra el texto sin
  formatear.
- La preparación de una unidad **detecta sus etapas sin releer el texto acumulado**, así que las
  operaciones largas no se van ralentizando.

### Nada cambia en cómo se usa

Ninguna opción ni atajo de la interfaz cambia respecto a la 1.19.0.

---

Instalador self-contained para Windows x64 (no requiere instalar .NET). Descarga
`FormatDiskPro-1.20.0-setup.exe` y ejecútalo (requiere privilegios de administrador).

El asset `FormatDiskPro-1.20.0-setup.exe.sha256` es el hash con el que la app verifica la descarga antes
de ejecutarla.
