# Aviones y pájaro - Joel

Entrega para integrar los modelos y animaciones del sprint 2 en Unity.

## Archivo que hay que importar

`Aviones_y_Pajaro.unitypackage` (aproximadamente 3,4 MB).

En vuestro proyecto de Unity: **Assets > Import Package > Custom Package**,
seleccionad el archivo y pulsad **Import**. Si ya habíais importado una versión
anterior, conservad seleccionados los archivos actualizados al volver a importar.

## Contenido

- Tres aviones con texturas propias: Cuaderno, Rayo y Brisa.
- Perfiles de velocidad y control diferentes para cada avión.
- Pájaro low poly con esqueleto, textura y animaciones de vuelo y captura lateral.
- Prefabs de los tres aviones, el pájaro y el selector del marcador.
- Controladores para el vuelo, el agarre y el transporte del avión.
- Marcador actualizado con los tres aviones, sin el pájaro, y PDF A4 imprimible.
- Selector con un único marcador y tres botones en la pantalla del móvil.
- Escena de demostración e instrucciones de integración.

## Dónde encontrarlo después de importar

- Prefabs: `Assets/FlyingFreeSprint2/Prefabs`.
- Demo: `Assets/FlyingFreeSprint2/Scenes/Demo_Sprint2.unity`.
- Instrucciones completas: `Assets/FlyingFreeSprint2/LEEME_SPRINT2.md`.
- Marcador y PDF: `Assets/FlyingFreeSprint2/Markers`.

El prefab `Pajaro_FlyingFree` incluye el Animator y el controlador. La captura
agarra el borde lateral del ala con el pico y transporta el avión del jugador.

La demo permite revisar los elementos con un marcador simulado. Para el juego
hay que conectarlos a vuestra escena RA, configurar Vuforia y probar el marcador
con la cámara del móvil. Imprimid el PDF en A4 al 100 %, sin ajustar a página:
la imagen de referencia mide 20 x 20 cm.

El paquete contiene los FBX y todos los recursos de Unity necesarios para esta
parte. Los archivos editables de Maya se conservan en la entrega local.
