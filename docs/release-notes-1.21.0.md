## FormatDiskPro v1.21.0

**Corte de mantenimiento: la aplicación se comporta exactamente igual que la 1.20.0.** No hay funciones
nuevas, ni cambios en la interfaz, ni correcciones de algo que te estuviera fallando. Si la 1.20.0 te va
bien, no notarás ninguna diferencia — y no pasa nada por actualizar más tarde.

Lo que cambia está por debajo: **cierra la auditoría técnica** iniciada el 2026-08-13 (39 de 40 puntos
resueltos, 2 descartados por decisión) y deja el proyecto en condiciones de detectar sus propios fallos
antes de que lleguen a una versión publicada.

### Los caminos de error ahora se pueden probar

Hasta ahora, comprobar qué hace la app cuando algo **falla de verdad** —una unidad que se desconecta a
mitad, `chkdsk` bloqueado por una directiva del sistema, una reinicialización que revienta después de
haber borrado el disco— exigía provocar esa avería con hardware real. Algunas, sencillamente, no se
probaban nunca: la única forma de comprobar qué pasa si *Reinicializar unidad* falla a medias era borrar
un disco de verdad y que fallara.

Los servicios internos pasan a recibir sus dependencias en lugar de construirlas, y con eso esos fallos
se reproducen en milisegundos y sin tocar ningún disco. **35 pruebas nuevas** (de 398 a 433), incluidas
las del caso que más importa: que la app **no dé por buena** una reinicialización que terminó sin dejar
la unidad utilizable.

> Nada de esto cambia lo que la app hace. Las 398 pruebas anteriores siguen pasando sin un solo cambio, y
> la suite que conduce la aplicación real da el mismo resultado que antes del refactor.

### Un registro de cambios de verdad

El repositorio ya tiene un [`CHANGELOG.md`](https://github.com/xfiberex/FormatDiskPro/blob/master/CHANGELOG.md)
con las 29 versiones publicadas. Y para que no envejezca en silencio, **el script de publicación se niega
a cortar una versión que no tenga su entrada escrita**.

### Doce capturas en el README

El README pasa de 3 a 12 capturas: ventana principal, salud S.M.A.R.T., comprobación de errores,
reinicializar unidad, confirmación destructiva e historial — **cada una en tema claro y oscuro**.

### Sobre firmar el instalador

Queda **descartado de forma explícita**, no aplazado. Este proyecto se distribuye sin firma
Authenticode, y por eso publica un `.sha256` con el que la propia aplicación **verifica el instalador
antes de ejecutarlo como administrador**. Eso no cambia: SmartScreen seguirá mostrando «editor
desconocido», y la verificación por hash sigue siendo lo que protege la actualización automática.

---

Instalador self-contained para Windows x64 (no requiere instalar .NET).

Descarga `FormatDiskPro-1.21.0-setup.exe` y ejecútalo (requiere privilegios de administrador).

El asset `FormatDiskPro-1.21.0-setup.exe.sha256` es el hash con el que la app verifica la descarga antes
de ejecutarla.
