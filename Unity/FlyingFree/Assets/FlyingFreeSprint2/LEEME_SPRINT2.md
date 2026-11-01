# Flying Free - Sprint 2

Paquete de modelos y animaciones para el juego de realidad aumentada.

## Pájaro

- Modelo original low poly: 1.188 triángulos, 732 vértices de modelado y 18 huesos.
- Una malla con skin y un solo material; atlas de color de 512 x 512.
- `Modelos_FBX/Pajaro_Modelo.fbx`: modelo con esqueleto, sin animación.
- `Modelos_FBX/Pajaro_Vuelo.fbx`: vuelo en bucle de 2 segundos a 30 fps.
- `Modelos_FBX/Pajaro_Captura.fbx`: animación de 4 segundos: abre el pico,
  coge el avión por el borde lateral del ala y continúa aleteando mientras lo lleva.
  La aproximación es perpendicular al avión; no lo agarra desde la cola.
- La captura ocurre en el fotograma 49, a los 1,6 segundos de comenzar el clip.
- `PaperGrip_SOCKET` es el hueso expuesto al que se sujeta el avión.
- No se incluye el avión dentro del FBX del pájaro: el juego utiliza el avión
  real del jugador. El prefab y el controlador incluidos hacen la unión.
- La escena Maya de captura incluye el avión de referencia y un recorrido de
  demostración separado del rig. En el FBX, la trayectoria la controla el juego.

## Tres aviones

| Avión | Aspecto | Velocidad de lanzamiento | Giro máximo | Respuesta |
|---|---|---|---|---|
| Cuaderno | Papel rayado azul, forma clásica | 1,70-3,30 m/s | 110 grados/s | Equilibrada |
| Rayo | Franjas cálidas, alas estrechas y nariz larga | 2,40-4,60 m/s | 76 grados/s | Más lenta: anticipar giros |
| Brisa | Papel cuadriculado verde, alas anchas | 1,15-2,45 m/s | 148 grados/s | Más rápida y precisa |

Los FBX contienen la forma y la textura. Las diferencias de velocidad y control
se aplican mediante los prefabs y los perfiles de Unity; no quedan "grabadas"
en la geometría. Los valores son un punto de partida editable en `Profiles`.

Todos miran hacia +Z, con +Y hacia arriba. Los FBX se han exportado desde Maya
en centímetros y Unity los importa a metros conservando `Scale Factor = 1`.
El avión Cuaderno mide aproximadamente 30 cm de largo. Se pueden escalar los
elementos de la escena de juego de forma conjunta si necesitáis otra escala RA.

## Usarlo en Unity

1. Importa `FlyingFree_Sprint2.unitypackage` con Assets > Import Package > Custom Package.
2. Abre `Assets/FlyingFreeSprint2/Scenes/Demo_Sprint2.unity` y pulsa Play.
3. Elige Cuaderno, Rayo o Brisa en las tarjetas superiores.
4. Ajusta la fuerza, pulsa LANZAR y arrastra el joystick de la esquina inferior derecha.
5. Pulsa PAJARO: CAPTURAR para ver cómo se aproxima al avión y se lo lleva.

La demo usa un marcador simulado para poder revisar la selección y las animaciones
sin cámara. No sustituye la escena RA completa de vuestro juego.

El módulo integrado Animation debe estar habilitado. Si partís de un proyecto
con módulos mínimos y aparece un error de `Animator`, activad Animation en
Window > Package Manager > Built-in. También puede añadirse esta entrada a las
dependencias existentes de `Packages/manifest.json`, conservando el resto:
`"com.unity.modules.animation": "1.0.0"`.

Los prefabs se encuentran en `Assets/FlyingFreeSprint2/Prefabs`.
Las texturas y materiales son compatibles con Built-in y URP; el editor adapta
los materiales al importar. Si necesitáis regenerar la configuración:
Flying Free > Sprint 2 > Preparar prefabs y demo.

## Un marcador y tres botones: integración con Vuforia

El marcador actualizado muestra únicamente Cuaderno, Rayo y Brisa, sin el pájaro.
Usad la imagen y el PDF incluidos en este paquete. Si ya habíais importado la
versión anterior, volved a importar el paquete sustituyendo los archivos existentes
y reemplazad la impresión anterior. Si gestionáis vuestro propio ImageTarget,
actualizad también su imagen de referencia con este PNG.

1. En vuestro proyecto con Vuforia Engine y la ARCamera ya configurada, importa el paquete.
2. Usa Flying Free > Sprint 2 > Activar adaptador Vuforia.
3. Espera a que Unity termine de compilar.
4. Arrastra `Selector_Marcador.prefab` a la raíz de vuestra escena RA.
5. Conserva `Printed Width Meters = 0.20` si imprimís el marcador del PDF al tamaño real.
6. Apunta al marcador: aparecerán la vista previa y los tres botones. La elección queda
   disponible en `PlaneMarkerSelector.Selected` y en el evento `SelectionChanged`.

El adaptador crea un Instant Image Target desde la imagen incluida, por lo que
no necesita que generéis una base de datos adicional para este marcador.
Utiliza la configuración/licencia de Vuforia de vuestro proyecto; este paquete
no incluye claves de licencia ni modifica el proyecto Tarea_RA existente.

Si ya tenéis un ImageTarget configurado o usáis otro sistema de seguimiento,
podéis conectar sus eventos a `VuforiaSelectionMarker.TargetFound()` y `TargetLost()`
sin activar el adaptador automático. Usad una de las dos vías de seguimiento.

## Conectar el vuelo y la captura al juego

- Instancia el prefab del perfil seleccionado: `selector.Selected.prefab`.
- `PaperPlaneMotor.Launch(fuerza01)` convierte una fuerza de 0 a 1 en la velocidad del perfil.
- `PaperPlaneMotor.SetSteering(Vector2)` recibe el joystick, con cada eje entre -1 y 1.
- El evento `PaperPlaneMotor.Hit` permite conectar los impactos con vuestros globos.
- Instancia `Pajaro_FlyingFree.prefab` y llama a `BirdCarryController.BeginCapture(avion)`.
  El controlador se aproxima al avión, lo acopla al pico y se lo lleva.
- La compensación del agarre cambia con el ancho de cada avión, evitando que
  Rayo o Brisa se sujeten como si tuvieran las dimensiones de Cuaderno.
- El vuelo de los pájaros por la pantalla y las reglas de aparición de globos
  corresponden a vuestro controlador de juego; no se modifica vuestro nivel actual.

## Abrir los modelos en Maya

1. File > Set Project: selecciona esta carpeta Sprint2_FlyingFree_Lateral.
2. Abre el archivo deseado dentro de `Maya`.
3. Pulsa 6 para texturas; en las escenas con iluminación de preview, pulsa también 7.
4. Selecciona `Pajaro_FlyingFree` y pulsa F para encuadrar el personaje.
5. Las animaciones están preparadas a 30 fps. Usa reproducción Real-time.

`Pajaro_Modelo_Maya.ma` contiene el modelo y rig; `Pajaro_Vuelo_Maya.ma` el bucle;
`Pajaro_Captura_Maya.ma` la demostración de captura; `Tres_Aviones_Maya.ma` los tres modelos.

## Marcador y vistas previas

- `Marcadores/Selector_FlyingFree_A4.pdf`: imprimir en A4 al 100 %, sin ajustar.
  El cuadrado debe medir 20 x 20 cm; incluye una regla de comprobación de 10 cm.
- `Marcadores/Selector_FlyingFree.png`: imagen exacta usada para el reconocimiento.
- `Previews/Presentacion_Sprint2.mp4`: vuelo, captura y comparación de aviones.
- `Previews/Pajaro_Vuelo.mp4` y `Pajaro_Captura.mp4`: animaciones por separado.
- `Previews/Comparativa_Aviones.png`: diferencias visuales y de comportamiento.

## Verificación

Se ha comprobado el cierre del bucle, el rig al reimportar los FBX en Maya,
las intersecciones durante la captura y el agarre de los tres aviones.
El paquete se ha importado en Unity 6000.0.38f1 y se ha probado en Play Mode:
velocidades y giros distintos, las tres selecciones, acoplamiento y transporte.
El adaptador también se ha compilado contra Vuforia Engine 11.4.4.

La detección del marcador con cámara y su estabilidad deben probarse en vuestro
móvil con la impresión real, la ARCamera y la licencia de vuestro proyecto.

Referencia de la API utilizada:
https://developer.vuforia.com/references/unity/classVuforia_1_1ObserverFactory.html
