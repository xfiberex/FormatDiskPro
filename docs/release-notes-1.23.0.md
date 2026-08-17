## FormatDiskPro v1.23.0

Esta versión no trae funciones nuevas. Trae una revisión completa de la interfaz, y lo primero que encontró
fueron **tres sitios donde la aplicación afirmaba algo que no era cierto**. Cuando la herramienta borra
discos, eso importa más que cualquier función que pudiera añadirse.

### Lo que la interfaz decía y no era verdad

**El diálogo que confirma *Reinicializar unidad* se titulaba «Confirmar formato».** Las dos operaciones
irreversibles comparten diálogo, y compartían también el título. El cuerpo sí explicaba que se borra el
disco físico entero, pero quien leía solo el título estaba confirmando algo distinto —y bastante menos
grave— de lo que iba a ocurrir. Ahora cada operación pone el suyo.

**El campo de confirmación mostraba la letra que hay que teclear.** Iba como texto de marcador, así que el
campo se leía como si ya estuviera relleno y regalaba la respuesta justo donde la aplicación pone su única
fricción deliberada. Al quitarlo apareció algo peor: Windows usaba ese marcador como **nombre accesible**
del campo, de modo que un lector de pantalla anunciaba la letra en voz alta. Ahora el campo aparece vacío y
tiene nombre propio, en los cinco idiomas.

**«Velocidad de rotación: SSD»**, en *Salud del disco*, con «Tipo de medio: SSD» en la fila de encima. Una
velocidad cuyo valor era un tipo de medio. En un disco de estado sólido la fila ya no aparece —no es un dato
que falte, es una pregunta que no aplica— y en uno mecánico sigue mostrando sus RPM. Si no se puede saber si
gira, la fila se queda como «No disponible»: esconderla sería dar por hecho que es SSD.

### Los números ahora hablan tu idioma

Con la interfaz en español sobre un Windows en inglés salía `223.6 GB` y `32,161 h (≈ 3.7 años)`:
separadores ingleses pegados a palabras españolas. La aplicación deja cambiar de idioma sin tocar Windows,
así que si cambia el texto, debe cambiar también cómo se escriben las cifras.

Lo que se **guarda** —el historial, el CSV exportado— sigue en un formato fijo e independiente del idioma:
un fichero exportado con la aplicación en francés se lee igual con la aplicación en inglés.

### Datos que estaban ahí pero en crudo

- **Las horas de encendido se leen.** Antes `32161 h`; ahora `32.161 h (≈ 3,7 años)`, con separador de
  millares y la equivalencia en días, meses o años según lo que dé una cifra útil.
- **El historial muestra tamaños, no bytes**: `small-fat32=2 GB` en vez de `small-fat32=2147483648`. Como
  solo cambia lo que se ve, las entradas ya registradas también se leen mejor, y el buscador encuentra
  igual si escribes «2 GB» que si escribes el número exacto.
- **La pantalla de *Novedades* enseñaba los asteriscos del Markdown** y partía los párrafos a mitad de
  frase. Es la primera pantalla que se ve tras actualizar — esta misma.
- **La *Licencia* y los *Avisos de terceros* no cabían.** Vienen preformateados a 80 columnas y el diálogo
  los ajustaba a unas 60: cada línea larga se partía en dos y hasta las líneas separadoras salían cortadas.
  Ahora conservan su maquetación original, sin alterar ni una letra del texto legal.
- **Los resúmenes de *Reinicializar unidad* se partían donde no toca**, y en un sitio distinto en cada
  idioma. Es el texto que hay que leer antes de borrar un disco entero.

### La barra de ocupación

La tarjeta *Unidad* pinta ahora el espacio usado **y** el libre, cada uno con su color, con el dato en
claro encima (`Usado 780,9 GB / 930,5 GB`). Antes el hueco era una línea de 1 px: en una unidad recién
formateada no se veía nada, y el espacio usado no aparecía en cifras por ningún lado. El relleno sigue
avisando de lo llena que está la unidad —ámbar al 80 %, rojo al 90 %—, y el contraste entre usado y libre
está medido contra el criterio de WCAG para objetos gráficos, no elegido a ojo.

### Y el resto del pulido

Opciones de *Comprobar errores* que explican qué hace cada una y qué cuesta; todos los diálogos con el mismo
ancho (abrir dos seguidos hacía saltar la ventana); las opciones desactivadas atenuadas una sola vez y no
dos; encabezados de campo puntuados igual entre sí.

### Por debajo

**64 pruebas nuevas** (521 → 585), y la lógica pura de `Core/` al 98,1 %. Tres de los arreglos se
verificaron reintroduciendo el fallo para comprobar que la prueba lo cazaba — porque una prueba en verde no
demuestra nada si nunca se la ha visto fallar. De hecho este corte encontró **cuatro pruebas que llevaban
tiempo en verde** comprobando algo que el usuario nunca veía.

---

Instalador self-contained para Windows x64 (no requiere instalar .NET).

Descarga `FormatDiskPro-1.23.0-setup.exe` y ejecútalo (requiere privilegios de administrador).

El asset `FormatDiskPro-1.23.0-setup.exe.sha256` es el hash con el que la app verifica la descarga antes
de ejecutarla.
