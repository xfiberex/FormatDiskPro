## FormatDiskPro v1.22.0

Si alguna vez has usado *Reinicializar unidad → crear solo una partición FAT32 pequeña* para actualizar el
BIOS de una placa base, sabrás cómo acababa: con un pendrive de 256 GB del que solo podías usar 32, y el
resto **sin asignar** hasta que abrieras *Crear y formatear particiones de disco duro* de Windows. Justo la
herramienta que esta aplicación existe para no tener que abrir.

Esta versión cierra eso.

### Ahora puedes aprovechar el resto del disco

Al marcar la partición FAT32 pequeña aparece una opción nueva: **qué hacer con el resto del disco**.

- **Dejarlo sin asignar** — lo de siempre. Sigue siendo el valor por defecto: si no tocas nada, la
  aplicación se comporta exactamente igual que antes.
- **Crear una segunda partición** que ocupe todo el sobrante, con su sistema de archivos (exFAT o NTFS) y
  su etiqueta.

Todo en la misma operación y bajo la misma confirmación destructiva de siempre — escribiendo la letra de la
unidad. No hay pantallas nuevas ni pasos añadidos.

La **partición FAT32 se crea siempre primera**, y no es un detalle: Windows 10 (1703) y posteriores muestran
las dos, pero equipos más antiguos y muchos aparatos —televisores, radios de coche, el propio BIOS de una
placa base— solo leen la primera partición de una unidad extraíble. Y esa es justamente la que interesa que
vean. La aplicación te lo dice ahí mismo, no solo en la documentación.

### La opción estaba escondida donde más falta hacía

Un fallo que llevaba ahí desde la v1.14.0: la sección **solo aparecía en unidades de 32 GB o más**.

Tenía su lógica cuando se escribió —la función nació como rodeo al límite de Windows, que no crea volúmenes
FAT32 de más de 32 GB, y en unidades menores FAT32 ya está disponible—, pero se quedaba corta: lo que hace
por debajo es *crear una partición más pequeña que el disco y dejar el resto libre*, y eso sirve igual en un
pendrive de 16 GB. Ahora aparece en **cualquier unidad extraíble** donde quepa.

Al arreglarlo salieron dos problemas más, ninguno visible hasta entonces:

- **El selector ofrecía tamaños que no caben.** Siempre 1/2/4/8/16/32 GB, sin mirar el disco. En un pendrive
  de «16 GB» (unos 14,9 GB reales) elegir 16 habría fallado **con el disco ya borrado**. Ahora solo se
  ofrecen los que caben de verdad.
- **El tope se medía sobre la partición, no sobre el disco.** Usar la función una vez dejaba el máximo
  clavado en el tamaño de lo que acababas de crear: un trinquete que solo bajaba.

### Si algo falla a mitad, ahora te lo cuenta

Con dos particiones existe un estado intermedio real —la primera creada y formateada, la segunda no— y el
mensaje de error no sabía distinguirlo de «no se hizo nada». Ahora dice cuántas particiones se crearon,
cuáles quedaron utilizables, y deja claro que **el disco ya estaba borrado** cuando falló. Un «no se pudo
reinicializar» a secas deja creer que no pasó nada, que es lo contrario de la verdad.

**No se revierte nada automáticamente**, y es deliberado: el disco ya está borrado, así que «deshacer» solo
podría significar borrarlo otra vez. Se te informa y decides tú.

### Por debajo

El plan de particiones pasó a ser un dato explícito y validable **antes de tocar el disco**: que la suma
quepa, que ninguna partición sea de cero, que cada volumen FAT32 respete el límite de Windows, que las
etiquetas valgan para su sistema de archivos, que el número de particiones sea legal para MBR. Trece motivos
de rechazo, todos comprobados sin hardware. **88 pruebas nuevas** (433 → 521).

---

Instalador self-contained para Windows x64 (no requiere instalar .NET).

Descarga `FormatDiskPro-1.22.0-setup.exe` y ejecútalo (requiere privilegios de administrador).

El asset `FormatDiskPro-1.22.0-setup.exe.sha256` es el hash con el que la app verifica la descarga antes
de ejecutarla.
