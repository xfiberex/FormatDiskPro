## FormatDiskPro v1.19.0

Cierra el **Tier 2** de la auditoría técnica (26 de 40 puntos resueltos). Una corrección real de
diagnóstico y, por debajo, trabajo de calidad que no se ve pero sostiene lo que sí.

### Verificar capacidad real: se acabaron los falsos OK en unidades pequeñas

La prueba escribía en la unidad y la releía para detectar **capacidad falsificada**, pero la relectura
pasaba por la **caché de archivos de Windows**: en una USB más pequeña que la RAM libre, el sistema podía
devolver los datos desde memoria en vez de leerlos del medio, y una unidad falsa podía salir **correcta**.

Ahora la relectura **omite la caché del sistema** (la misma técnica que ya usaba el benchmark), así que lo
que se compara es lo que la unidad devuelve de verdad. Es el escenario para el que existe esta función.

### Por debajo

- **La cobertura de pruebas se mide y se exige**: la lógica pura está al 97 %, y un corte de versión no
  sale si baja del mínimo.
- **La ventana principal, reorganizada** en archivos por asunto (de 2 100 a 753 líneas el mayor), sin
  cambiar comportamiento: la suite completa sobre la app real da el mismo resultado que antes.
- **El proyecto ya publica** cómo reportar una vulnerabilidad de forma privada (`SECURITY.md`) y cómo
  contribuir (`CONTRIBUTING.md`), con plantillas de issue.

### Nada cambia en cómo se usa

Ninguna opción, atajo ni comportamiento de la interfaz cambia respecto a la 1.18.0.

---

Instalador self-contained para Windows x64 (no requiere instalar .NET). Descarga
`FormatDiskPro-1.19.0-setup.exe` y ejecútalo (requiere privilegios de administrador).

El asset `FormatDiskPro-1.19.0-setup.exe.sha256` es el hash con el que la app verifica la descarga antes
de ejecutarla.
