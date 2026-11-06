# Sprint 2b — Tarea 4: colisiones e interacciones

El sistema se integra directamente en la escena existente `Assets/Scenes/GameRA.unity`. No requiere volver a generar la escena ni añadir componentes manualmente. Se mantiene **Unity 6000.3.6f1**, **Vuforia 10.17.4** y las reglas de partida ya integradas por el equipo: aviones ilimitados y objetivo de cinco globos en treinta segundos.

## Qué se ha completado

| Requisito | Implementación |
|---|---|
| Colisiones entre aviones y globos | Triggers y barrido de los colliders reales del avión entre posiciones de vuelo. Detecta también un globo atravesado entre dos frames. |
| Explosión de los globos | Ejecuta el clip del prefab original, detiene su movimiento y deja terminar los 5,625 segundos de animación antes de eliminarlo. |
| Colisiones entre pájaros y aviones | El contacto inicia el robo existente. Un cruce rápido también se detecta aunque el avión ya haya salido del radio al terminar ese frame. |
| Evitar colisiones repetidas | Reserva del globo antes de avisar a otros sistemas, consumo único del avión, desactivación de todos sus colliders y reserva exclusiva del pájaro. |

Los contactos físicos y los barridos usan el mismo resolutor. Los contactos tardíos de un avión robado, terminado o utilizado para otro globo se ignoran. También se ignoran aviones de vista previa y componentes desactivados. Un globo ya explotado no consume otro avión.

Si un paso de movimiento atraviesa varios globos, se resuelve el primero del recorrido. Se respetan las capas de colisión y `Physics.IgnoreCollision`; los triggers decorativos no cuentan como obstáculos. Los buffers se reutilizan y crecen si hay muchos contactos, en vez de omitir resultados al llenarse.

La pérdida de seguimiento bloquea las interacciones de globo, pájaro y obstáculo. Al recuperar la marca, el barrido utiliza la pose actual de la referencia AR: mover o relocalizar la marca no se interpreta como vuelo a través de la habitación.

## Corrección: ascenso paralelo a la marca

Los globos **suben por el mismo plano de la imagen**, hacia la parte superior de la vista. La coordenada normal al target permanece fija a `0,03 × GameplayScale` metros de su superficie; la subida y la oscilación no los desplazan hacia la cámara. La dirección continúa siendo paralela a la imagen. El encuadre compacto que se describe a continuación ajusta ahora la escala de todo el conjunto de forma coherente.

La corrección anterior fijaba la subida a `Vector3.up` mundial. Eso no garantizaba que fuese paralela a la marca: el eje vertical global y el eje que recorre la imagen son referencias distintas. Ahora el generador toma la normal del target y proyecta el arriba de la cámara sobre su plano. El nudo de cada globo y el marco compartido con avión/pájaros utilizan esa misma referencia.

El área nominal de 2 × 1,5 unidades se conserva para los sistemas del equipo, pero ahora se convierte a un tamaño compacto calculado con la proyección real de la cámara. El centro nominal se sitúa a 0,3 unidades hacia arriba y se desplaza dentro del encuadre cuando es necesario. Se conservan el seguimiento, la reserva única de impactos, la explosión completa, los robos y los treinta segundos de partida de las tareas 3 y 5.

Las pruebas de orientación se han sustituido por ensayos con **poses coherentes de cámara y target**, incluida una referencia centrada en la marca. Se mide la subida hacia arriba en pantalla y la profundidad constante, también a distancias de cámara de 0,35, 0,7 y 1,5 m. Las capturas son renders del editor con seguimiento simulado, no imágenes de una prueba física con la webcam.

La marca que utiliza `GameRA` sigue siendo **`target_logo`**, el logo de Flying Free configurado a **10 × 10 cm**. [Abrir la imagen](../../Unity/FlyingFree/Assets/Editor/Vuforia/ImageTargetTextures/TargetManagerBBDD/target_logo_scaled.jpg).

## Encuadre compacto alrededor de la marca

El área anterior se extendía hasta 2 × 1,5 metros alrededor de un logo de diez centímetros. Ahora `Fit Layout To Camera` está activado en `GameRA`: al generar los globos calcula un tamaño que entra en la cámara AR, reservando espacio para el HUD y el joystick. El máximo es el 15 % del área nominal (30 × 22,5 cm); cerca de la cámara se adapta a un tamaño menor. La formación queda alrededor y por encima del logo, manteniendo el plano paralelo.

El cálculo se realiza una vez al generar la ronda; no agranda ni encoge los objetos mientras se mueve el dispositivo. Repetir la partida vuelve a ajustar el conjunto. Se toma la proyección calibrada de `ARCamera`, tanto en vertical como en horizontal, sin alterar la cámara de Vuforia ni su zoom.

La escala se comparte con `ARGameSpace`: tamaño, movimiento y colliders de globos, aviones y pájaros mantienen la misma proporción. Se adaptan los radios, márgenes de aparición y velocidad de aproximación/huida del pájaro. El avión sale delante del recorte cercano de la cámara y apunta al punto pulsado sobre el plano de los globos, evitando el paralaje del anterior punto de mira lejano. Se conserva el controlador existente; no se implementa un nuevo sistema de gestos.

Las nuevas comprobaciones usan una cámara de 41,5 grados, a 30, 50 y 80 cm de la marca, en formato 1080 × 2400 y 1920 × 1080. Verifican las esquinas de los bounds de los cuerpos e hilos, los seis globos completos en pantalla, impactos desde `TouchFlightController`, robos y aparición automática de pájaros a la escala compacta.

Capturas del editor, con seguimiento simulado y cámara a **50 cm**: [vertical](Encuadre_Compacto_Vertical.png) y [horizontal](Encuadre_Compacto_Horizontal.png). No son capturas de una prueba física con la webcam.

## Cómo abrir y comprobar

1. En Unity Hub abre la carpeta **`Unity/FlyingFree` de tu clon**, con **6000.3.6f1**. Espera a que termine de compilar.
2. Para la comprobación automática, detén Play y selecciona **Flying Free > Sprint 2b > Probar colisiones e interacciones**.
3. El comprobador solicita guardar las escenas modificadas, abre una escena temporal y ejecuta los ensayos con los prefabs reales. La vista indica que es una simulación. Al terminar vuelve a las escenas guardadas y abre el informe `Logs/PruebasColisiones/Colisiones_PlayMode.txt`.
4. El resultado esperado es **175 líneas PASS** y **RESULTADO: TODAS LAS PRUEBAS PASAN**. Las capturas se guardan en esa misma carpeta. El informe de esta entrega está [aquí](Colisiones_PlayMode.txt).
5. Para jugar con Vuforia, abre **`Assets/Scenes/GameRA.unity`**, pulsa Play y enfoca la **marca del logo (`target_logo`)**. Lanza un avión contra un globo: debe desaparecer el avión, estallar un solo globo y sumar una sola vez. Si toca un pájaro, debe activar el robo sin sumar puntos.
6. Comprueba también que perder la marca pausa la interacción y que, al recuperarla, el juego continúa sin explosiones falsas.

La ejecución automática usa PhysX, los cinco prefabs de avión, el globo animado, el pájaro y el gestor de partida reales. El seguimiento perdido/recuperado se controla como **simulación**, sin presentar ese ensayo como una prueba física con Vuforia.

## Conexión con las tareas del equipo

- **Lanzamiento:** `TouchFlightController` ya convierte los prefabs seleccionados a `PaperPlane` al lanzarlos. El nuevo detector se prepara automáticamente en `PaperPlane.Awake`; no debe añadirse como componente. Las demos antiguas con `PaperPlaneMotor` conservan su funcionamiento.
- **Globos:** conservar `GloboTarget` en la raíz y los colliders dentro de su jerarquía. `PopBalloon(plane)` sigue siendo la entrada compatible. `TryPopBalloon(plane)` devuelve si se aceptó el impacto, e `IsPopped` permite consultar su estado. El evento del Inspector **On Popped** se ejecuta una vez para conectar sonido u otros efectos; no debe volver a sumar puntos.
- **Pájaros:** se mantienen `HandlePlaneCollision`, `IntentarInterceptar`, el socket lateral y el sistema de reservas de la tarea 3. El avión llevado deja de participar en colisiones.
- **Puntuación:** la única modificación de `GameManagerRA` consiste en consumir el avión antes de emitir la actualización de puntos. Así, si un listener provoca otra colisión durante la notificación, no puede contabilizar otro globo con ese mismo avión. No se redefinen objetivos, tiempo ni munición.
- **Objetivos clásicos:** `PlaneTarget.NotifyHit` consume una sola vez el objetivo y finaliza el avión antes de emitir su evento, impidiendo llamadas repetidas desde el propio evento.
- **Seguimiento:** el generador de globos evita reactivar renderers de globos ya explotados al recuperar la marca.

Los cinco aviones actuales utilizan `BoxCollider`; también se verifica el detector con un `SphereCollider` hijo. Para un futuro collider de geometría personalizada se utiliza su caja envolvente como respaldo conservador; habría que calibrarlo con el modelo nuevo.

## Archivos de la tarea

- `Assets/Scripts/PlaneCollisionDetector.cs`: consultas de solapamiento y barrido, filtros y orden de contactos.
- `Assets/Scripts/PaperPlane.cs`: resolutor común, bloqueo por estado y retirada de colliders.
- `Assets/Scripts/GloboTarget.cs`: impacto único, explosión completa y evento de interacción.
- `Assets/Scripts/PajaroInteraccion.cs`: validación del contacto y compatibilidad con cruces rápidos/pausa.
- `Assets/Scripts/PlaneTarget.cs`: consumo único del objetivo clásico.
- `Assets/Scripts/GameManagerRA.cs`: orden de la transacción de impacto.
- `Assets/Scripts/TargetPlaneSpawner.cs`: recuperación de tracking sin reactivar un globo explotado.
- `Assets/Editor/Collisions/CollisionSprint2BChecks.cs` y `Assets/Tests/Collisions/CollisionPlayModeProbe.cs`: comprobador reproducible desde Unity, excluido del ejecutable.

La orientación usa el generador, `BalloonAnimator` y la referencia `ARGameSpace`. El encuadre compacto añade una escala compartida en `ARGameSpace`, activa el perfil en `GameRA` y adapta las conversiones de lanzamiento, movimiento, colisiones y captura de los sistemas existentes. Se conservan los modelos, materiales, prefabs, versión de Unity y dependencias.

## Validación

- Base revisada: rama `Sprint2`, commit `fef14b1`.
- **175 comprobaciones de colisiones, orientación y encuadre aprobadas en Play Mode**, con Unity **6000.3.6f1**: cinco modelos a 120 m/s, contactos compuestos, escalas, dos globos en un paso, dos aviones en un globo, capas, exclusiones, obstáculos finos, más de 32 contactos, objetivos clásicos, callbacks reentrantes, explosión completa, robo real, pausa y relocalización simulada, poses coherentes de cámara y target, subida paralela a la marca, profundidad constante, seis globos e hilos visibles a 30/50/80 cm en vertical y horizontal, escala estable, lanzamiento e impacto reales sobre el globo señalado, robos compactos y aparición de pájaros a escala.
- **87 comprobaciones existentes de pájaros aprobadas** con estos cambios. [Informe](Pajaros_PlayMode.txt).
- **56 comprobaciones existentes de puntuación y partida aprobadas**, incluida navegación real a juego y menú. [Informe](Partida_PlayMode.txt).
- Compilación completa de desarrollo para Windows con las escenas habilitadas del juego: **Succeeded, 0 errores y 4 avisos**, en **Unity 6000.3.6f1**. [Informe](Compilacion.txt). No se ha compilado ni probado un APK en el móvil.
- Capturas desde la cámara de referencia del target: [ascenso paralelo](Globos_Paralelos_Camara_Unity.png) y [vista vertical de móvil](Globos_Paralelos_Portrait_Unity.png). La segunda cámara está a 4,2 m para encuadrar el área completa; no implica una reducción de los globos.
- Capturas reales del editor: [explosión del globo](Explosion_Globo_Unity.png) y [contacto con pájaro](Colision_Pajaro_Unity.png).

**Pendiente del equipo de integración AR:** comprobar cámara, seguimiento y escala en el móvil con la marca física. La validación de escritorio no sustituye esa prueba de dispositivo de la tarea 6.

Referencias técnicas de Unity 6.3: [BoxCastNonAlloc](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.BoxCastNonAlloc.html), [OverlapBoxNonAlloc](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.OverlapBoxNonAlloc.html), [SyncTransforms](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.SyncTransforms.html).
