# Modelos y animaciones del vídeo - Flying Free

Paquete independiente del de aviones y pájaro del sprint de realidad aumentada.
Recursos convertidos desde la última escena completa del vídeo (Cinemática V6).

## Importar en Unity

1. En vuestro proyecto, abrid **Assets > Import Package > Custom Package**.
2. Seleccionad `Modelos_y_Animaciones_Video.unitypackage` e importad su contenido.
3. Abrid `Assets/FlyingFreeVideo/Scenes/Video_Parque_Demo.unity` y pulsad **Play**.

Todos los recursos de este paquete están en `Assets/FlyingFreeVideo`, separados
de `Assets/FlyingFreeSprint2` para poder importar los dos paquetes en el mismo proyecto.
El paquete se prepara y comprueba con Unity 6000.0.38f1.

## Qué incluye

- El escenario del parque y la ciudad, con sus modelos, texturas y materiales.
- Prefabs de decoración: árboles, mobiliario de picnic, farolas, edificios y otros elementos del escenario.
- El niño animado y un conjunto con la mesa, la hoja y el avión para revisar la acción completa.
- Animaciones separadas de sacar la hoja, doblar el papel, levantarse y lanzar el avión.
- El papel con sus pliegues animados.
- El avión protagonista, el vuelo suave en bucle y la trayectoria del vídeo.
- El avión secundario con el choque, la deformación y la caída.
- La explosión del globo y la explosión de estilo cartoon del choque.
- Los paseantes, palomas, pájaros y ardillas de la versión V6, con sus animaciones.
- Una escena de demostración con el montaje, las cámaras, el logo y la mezcla de audio del vídeo.

## Prefabs y animaciones

Los conjuntos principales están en `Assets/FlyingFreeVideo/Prefabs`.
La decoración está en su subcarpeta `Decoracion` y los personajes y animales
individuales en `Vida`. Los clips están en `Animations`, y los controladores
de Animator correspondientes en `Controllers`.

`Globo_Explosion` empieza intacto. Para reproducir el estallido desde vuestra
lógica de juego, activad el estado `Globo_Explosion_Explotar_Globo` de su Animator.
La escena de demostración sincroniza por separado todos los eventos del vídeo.

Para usar un elemento en el juego, arrastrad su prefab a vuestra escena. Los
conjuntos animados incluyen un Animator. Podéis elegir las acciones de sus
controladores o utilizar los clips en vuestra lógica de juego. Las trayectorias
de la cinemática se conservan como animaciones del vídeo; adaptad el desplazamiento
a las reglas de vuestro juego cuando reutilicéis esos elementos.

El niño conserva las animaciones de sus articulaciones. Su superficie y el
plegado del papel se han convertido a blend shapes para reproducir las
deformaciones de Maya en Unity. Conservad activada la importación de blend shapes
si modificáis los ajustes de los FBX. Cada conjunto conserva sus texturas y su
apariencia low poly; el niño mantiene el sombreado suave del vídeo.

Estos clips reproducen las acciones ya realizadas para el vídeo. Para crear
nuevas poses del niño cambiando el rig, utilizad el archivo original de Maya;
la superficie convertida conserva las deformaciones de la secuencia entregada.

## Demostración y exploración

La demo dura 40 segundos y reproduce la secuencia a 24 fotogramas por segundo.
Tiene botones para reproducir, pausar, reiniciar y explorar el parque, además
de una barra para saltar a un momento concreto. **F1** oculta los controles.

En el modo de exploración, mantened el botón derecho del ratón pulsado para
mirar alrededor. Usad **WASD** para moveros, **Q/E** para bajar/subir y **Mayús**
para acelerar. También podéis recorrer la escena desde la ventana Scene de Unity.

Para revisar una pose sin entrar en Play, seleccionad `REPRODUCTOR_DEL_VIDEO`,
ajustad `Time Seconds` y pulsad **Ver fotograma en el editor**.

La escena de demostración sirve como referencia del montaje del vídeo. Para
la aplicación de realidad aumentada podéis utilizar únicamente los prefabs que
necesitéis. Las colisiones, las reglas del juego y la vinculación al marcador
se configuran en vuestro proyecto de juego.

## Materiales y escala

Los FBX se convierten de centímetros de Maya a metros en Unity: una unidad de
Unity equivale a un metro. Los materiales usan Standard en Built-in y disponen
de adaptación a Lit en URP al importar el paquete o cambiar de pipeline.

Los archivos originales de Maya y las versiones anteriores del vídeo se
conservan en la carpeta local del sprint 1.
