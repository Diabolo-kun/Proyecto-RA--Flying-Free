# Flying Free - tráiler sobre una marca de Vuforia

## Abrir el proyecto

En Unity Hub, añade la carpeta `Proyecto-RA--Flying-Free-Sprint2/Proyecto-RA--Flying-Free-Sprint2/Unity/FlyingFree` del escritorio: la que contiene Assets, Packages y ProjectSettings. No selecciones solo la carpeta exterior.

El proyecto recibido indica Unity **6000.3.6f1**. Las comprobaciones se han ejecutado con **6000.6.3f1**, el editor instalado en este equipo. Se conserva el archivo original de versión del proyecto y las dependencias anteriores, salvo Vuforia: si eliges abrirlo con 6000.6.3f1, Unity puede actualizar sus paquetes integrados al importar. Hay una copia de los archivos sustituidos en `Copia_antes_del_trailer_2026-11-02`, en la carpeta exterior del escritorio.

## Probar el reconocimiento real con webcam

1. Abre `Assets/Scenes/FlyingFreeMenu.unity` y pulsa Play.
2. Pulsa el nuevo botón **TRÁILER**. Abre `TrailerAR`, con la cámara de Vuforia.
3. Imprime `TrailerEntrega/Marca_Trailer_20cm.pdf` a **tamaño real / 100 %**, sin ajustar a página. La imagen cuadrada debe medir **20 x 20 cm**. También puedes mostrar `Marca_Trailer_FlyingFree.png` en otra pantalla; para una escala física correcta, ajústala a 20 cm de ancho.
4. Enfoca toda la nueva imagen. El vídeo aparece sobre la marca, con una pantalla virtual de 30 cm de ancho. La marca antigua del logo no activa el tráiler.
5. Usa **PAUSAR / REANUDAR**, **-10 s**, **+10 s** o arrastra la barra de progreso. Los tiempos se muestran en minutos y segundos.
6. Si se pierde la marca, se oculta la pantalla y se pausa el audio y el vídeo. Al recuperar la imagen, continúa desde el mismo punto si estaba reproduciéndose. Si lo habías pausado manualmente, sigue pausado.
7. Al terminar puedes pulsar **VER OTRA VEZ**. **MENÚ** o Escape / Atrás vuelve al menú principal y detiene el reproductor.

Si no se abre la webcam, comprueba que otra aplicación no la tenga ocupada. En la configuración de Vuforia selecciona la cámara correcta. Se ha incluido el mismo ajuste de controlador de webcam que se utilizó en tus prácticas: **Tools > Vuforia > Preparar controlador de webcam**. En móvil, concede el permiso de cámara.

## Comprobar solo el vídeo y los controles sin imprimir

Abre `Assets/Scenes/TrailerPreview.unity`, pulsa Play y después **SIMULAR MARCA**. Se utiliza el vídeo real y la misma lógica de reproducción, pero el seguimiento es simulado y aparece indicado como **SIMULACIÓN**. **SIMULAR PÉRDIDA** permite comprobar la pausa automática. Esta escena no sustituye una prueba de reconocimiento de Vuforia. No se añade a la lista de escenas del juego.

## Archivos e integración

- Nuevo botón en `Assets/UI/Prefabs/FlyingFreeMenu.prefab`, enlazado a `TrailerMenuLink.OpenTrailer` mediante un UnityEvent del Button. JUGAR mantiene su conexión anterior a GameRA; el resto de ajustes del menú se conserva.
- `ResponsiveMenu` contempla cuatro botones y adapta la composición a vertical y horizontal. El HUD de vídeo aplica la safe area del dispositivo.
- `TrailerAR.unity` reutiliza la configuración de ARCamera de GameRA. La escena se añade a Build Settings / Build Profiles junto a las existentes.
- `Assets/TrailerAR/Video/Flying_Free_Video.mp4` es una copia íntegra del vídeo proporcionado, 40 segundos, 1920 x 1080 y 24 fps. No necesita Internet para reproducirse.
- La imagen está en `Assets/TrailerAR/Art/Marca_Trailer_FlyingFree.png`. `TrailerMarker` crea un **Instant Image Target** local de Vuforia llamado `FlyingFree_Trailer`; no hace falta subirla al portal ni sustituir las bases de marcas existentes.
- `TrailerPlayer` gestiona VideoPlayer, audio, pausas, búsqueda, finalización, recuperación de marca y regreso al menú. La pantalla es hija del ImageTarget y se mantiene alineada con él. Solo se reproduce con estado TRACKED; EXTENDED_TRACKED por sí solo no mantiene el vídeo activo.
- La barra envía una búsqueda al soltarla. Si se pulsan avances muy rápido, se conserva la última posición solicitada, evitando una cola larga de saltos.
- El audio del tráiler usa el volumen de sonido y silencio de `PersistentSettingsManager` / PlayerPrefs. No modifica AudioListener.volume.
- El paquete incompleto de Vuforia 11.4.4 se sustituye en manifest.json por el **10.17.4 completo de tus prácticas**. El enlace antiguo de Git LFS se conserva como archivo sin uso; no se ha borrado ninguna base de marcas.

Para volver a generar únicamente este añadido: **Flying Free > Tráiler > Preparar escenas de tráiler**. Se reconstruyen TrailerAR y TrailerPreview y se actualiza el botón del prefab, conservando sus otros componentes y conexiones. Guarda cambios manuales antes de usarlo. No es necesario ejecutar el generador para abrir o probar las escenas entregadas.

## Comprobaciones y límites

`Comprobaciones.txt` recoge las pruebas en Play Mode con el vídeo real y seguimiento simulado. `Vuforia_Comprobaciones.txt` recoge el arranque del motor/cámara de Vuforia y la carga de la nueva marca y las bases existentes. Se han cargado correctamente Base/ascorazones, Base/aspicas, Base/joker y los cuatro targets de TargetManagerBBDD.

No se ha comprobado físicamente la detección de la nueva imagen impresa ni se ha probado en un teléfono. La cámara de escritorio no permite el ajuste de enfoque automático solicitado por Vuforia y registra ese aviso, pero el motor y los observadores arrancan. El Editor de este entorno también registra una excepción de su índice de búsqueda SearchDatabase; no impidió completar las pruebas. No se afirma que el resto del Sprint 2 esté corregido.

La nueva ilustración de la marca se creó con la herramienta integrada de generación de imágenes. El prompt exacto queda en `Assets/TrailerAR/Art/Generation.json`. El vídeo es el archivo original aportado.

Referencias técnicas: https://developer.vuforia.com/library/vuforia-engine/images-and-objects/image-targets/instant-image-targets/ y https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Video.VideoPlayer-time.html
