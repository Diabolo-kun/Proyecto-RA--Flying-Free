# Proyecto-RA--Flying-Free

## Tráiler en realidad aumentada (Sprint 2)

El menú incluye **TRÁILER**, que abre una escena de Vuforia para reproducir el vídeo sobre una nueva marca. Incluye pausa/reanudación, saltos de 10 segundos, barra de progreso, repetición y regreso al menú. Al perder la marca se pausa y conserva el punto de reproducción.

- Proyecto Unity: `Unity/FlyingFree`.
- Escena de inicio: `Assets/Scenes/FlyingFreeMenu.unity`.
- Tráiler con reconocimiento real: `Assets/Scenes/TrailerAR.unity`.
- Prueba de controles sin cámara: `Assets/Scenes/TrailerPreview.unity`, identificada como simulación.
- [Guía de apertura y prueba](Unity/FlyingFree/TrailerEntrega/LEEME_TRAILER.md).
- [Marca imprimible de 20 x 20 cm](Unity/FlyingFree/TrailerEntrega/Marca_Trailer_20cm.pdf).

Se utiliza **Vuforia 10.17.4**, la versión de las prácticas. El SDK completo y el vídeo se almacenan con **Git LFS**. Instala Git LFS antes de clonar:

```sh
git lfs install
git clone --branch Sprint2 https://github.com/Diabolo-kun/Proyecto-RA--Flying-Free.git
cd Proyecto-RA--Flying-Free
git lfs pull
```

Abre la subcarpeta `Unity/FlyingFree` desde Unity Hub. Un ZIP descargado desde GitHub puede contener enlaces LFS en lugar de los archivos grandes: usa el clon con Git LFS para disponer del SDK y del vídeo completos.

La rama conserva la versión de Unity del equipo (**6000.3.6f1**). Las pruebas locales del añadido se han ejecutado con **6000.6.3f1**, Vuforia 10.17.4 y el vídeo real; los informes están en `Unity/FlyingFree/TrailerEntrega`. Falta la prueba física con la marca impresa y en móvil.
