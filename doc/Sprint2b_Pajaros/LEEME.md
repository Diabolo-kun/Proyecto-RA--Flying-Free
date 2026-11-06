# Sprint 2b — Sistema de pájaros

La escena `Assets/Scenes/GameRA.unity` utiliza los pájaros low poly existentes para interceptar lateralmente un avión en vuelo, sujetarlo por el ala y llevárselo hasta desaparecer.

## Qué incluye

- Apariciones aleatorias fuera de la vista del jugador, con separación inicial y límite de pájaros activos. El botón de aparición manual también respeta el límite.
- Patrullas elípticas ancladas a `ARGameSpace`, con sentidos, vueltas, velocidades y oscilaciones variables. La órbita queda delante de la pared del marcador y puede pasar por detrás del usuario.
- Detección automática de aviones próximos y de cruces entre frames. También acepta los contactos físicos que ya comunica `PaperPlane`.
- Reserva exclusiva: dos pájaros no pueden apropiarse del mismo avión. La reserva se libera si el objetivo desaparece o se desactiva el pájaro.
- Aproximación lateral y animación `Capture`. Utiliza el socket original `PaperGrip_SOCKET`, girado respecto al pico, con la compensación de agarre de cada perfil cuando está disponible.
- El avión conserva su posición, orientación y tamaño al cambiar de padre. Sus colliders dejan de participar en las interacciones y notifica exactamente una pérdida.
- Huida con el avión sujeto. Ambos desaparecen juntos fuera de pantalla, con un tiempo máximo de seguridad.
- Pausa de movimiento y animación mientras se pierde el tracking; recuperación sin barridos falsos por cambios de pose del marcador. Limpieza al terminar la partida o desactivar la bandada.
- Material del pájaro corregido: estaba enlazado a `copa_01.png`; ahora utiliza `Pajaro_Paleta.png`.

## Integración con las otras tareas

`ControlBandada` ya está conectado a la cámara y al prefab en `GameRA`. En esa escena, `Test Mode Without Vuforia` está desactivado: espera el seguimiento real de `ARGameSpace`. La escena `Bandada` conserva su modo de prueba sin marcador.

El vuelo libre sigue usando `PaperPlane.Initialize(direccionMundo, gameSpace.transform)`. Solo los aviones inicializados y aún en vuelo pueden ser objetivos del robo; las vistas previas y los aviones finalizados se ignoran.

Para el sistema de partida, el robo utiliza el evento existente `PaperPlane.OnFinished(false)` una sola vez. El responsable del contador de aviones puede consumir esa notificación. Esta entrega no redefine el límite de cinco lanzamientos ni las condiciones de victoria/derrota.

`PajaroInteraccion.Estado`, `HasCapturedPlane` y `AvionRobado` permiten consultar el estado. `HandlePlaneCollision(PaperPlane)` conserva la entrada que usa la detección existente. Un contacto con pájaro inicia el robo; ya no hace explotar el avión.

El controlador antiguo `BirdCarryController` se desactiva únicamente al configurar un pájaro de gameplay o iniciar su intercepción de `PaperPlane`; la demo antigua que usa `PaperPlaneMotor` conserva su controlador.

Se añadió un guard en `GloboTarget.PopBalloon`: un callback físico pendiente de un avión capturado o finalizado no puede explotar otro globo ni sumar puntos.

## Ajustes principales

En `ControlBandada`: mínimo/máximo activos, intervalo de aparición, radio, velocidad, oscilación, cantidad de vueltas y separación inicial.

En `PajaroInteraccion`: radio de detección (0,9 m), velocidad adicional de aproximación (3,5 m/s), tiempo máximo para interceptar (1,5 s), velocidad de huida (2,5 m/s), tiempo mínimo de huida (1,5 s) y máximo (5 s). La órbita de cada pájaro permanece fija en el espacio AR durante su patrulla, en vez de perseguir los movimientos de la cámara.

## Cómo comprobarlo

1. Abre `Unity/FlyingFree` con Unity. La versión declarada del proyecto se conserva en **6000.3.6f1**.
2. Detén Play y guarda tus escenas abiertas.
3. Ejecuta **Flying Free > Sprint 2b > Probar sistema de pájaros**. El comprobador abre una escena vacía de pruebas y entra en Play Mode.
4. Los resultados y la imagen de captura quedan en `Logs/PruebasPajaros/` dentro del proyecto. La carpeta está excluida de Git.
5. Para AR real, abre `GameRA`, detecta la marca y lanza un avión cerca de un pájaro que pase de lado. Al ser robado debe dejar de responder al joystick y salir de pantalla junto al pájaro.

## Validación realizada

- Pull inicial de `Sprint2` hasta `ca838a1`.
- Compilación e importación del proyecto completo en una copia de validación, con **Unity 6000.6.3f1 y Vuforia 10.17.4**.
- **87 comprobaciones aprobadas en Play Mode**, con los prefabs reales Cuaderno, Rayo, Brisa, AsCorazones y Estandar. Incluyen reservas, animación, agarre lateral sin salto, escalas, colliders, notificación única, puntuación, huida, desaparición, cancelación, relocalización AR y fin de partida.
- Intercepción automática probada con un avión a **40 m/s**.
- Conexiones de `GameRA` y textura verificadas antes de iniciar las pruebas.
- Compilación de desarrollo de la escena real `GameRA` para Windows: **Succeeded, 0 errores**. Los 16 avisos registrados proceden de la configuración y APIs existentes del proyecto al compilar con la versión instalada.
- Revisión visual de las poses de vuelo/captura y del robo con el material corregido.

Los informes y una captura real están junto a esta guía. El tracking perdido/recuperado y la relocalización se ejercitaron de forma controlada en Play Mode; **no constituyen una prueba con el móvil y la marca física**. Esa comprobación final de cámara y escala real queda pendiente en el dispositivo del grupo.
