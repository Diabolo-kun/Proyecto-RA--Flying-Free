# Sprint 2b — Puntuación y gestión de partida

## Reglas configuradas

- **Aviones ilimitados.** El HUD muestra ∞ y cuenta los lanzamientos realizados.
- **Objetivo: explotar 5 globos en 30 segundos.** Ambos valores son configurables en `GameManagerRA`.
- Victoria inmediata al completar el objetivo. Derrota al agotarse el tiempo sin completarlo.
- Un avión libre en vuelo a la vez. Tras acertar, fallar o sufrir un robo, se puede lanzar otro. Un segundo toque permite retomar el joystick del avión actual, sin sustituirlo ni registrar otro lanzamiento.
- El reloj espera a que se reconozca la marca. Si se pierde el seguimiento AR, se pausan el reloj y el avión libre; no se aceptan lanzamientos ni impactos durante esa pausa.

El límite de cinco municiones del planteamiento inicial se ha sustituido por suministro infinito, según la indicación posterior de Joel. En `GameRA`, la dificultad depende del tiempo y del objetivo. El valor de aviones iniciales solo se usa si se desactiva expresamente el modo ilimitado en el Inspector.

## Qué incluye la entrega

`GameManagerRA` coordina los lanzamientos, la puntuación y un único resultado final. Conserva contadores separados de aciertos, fallos y robos. El último estado se congela al terminar y no acepta puntos tardíos ni resultados duplicados.

El globo, sus puntos y el final del avión se resuelven como una transacción: alcanzar el objetivo no puede contabilizar a la vez un fallo. Los callbacks repetidos de un mismo globo o avión tampoco duplican puntos ni estadísticas.

Un avión que pasa de largo la marca, sale del área de vuelo o supera su tiempo máximo se considera perdido y libera el siguiente intento. Los valores iniciales son 1 m más allá de la marca, 4 m de margen lateral/vertical y 6 s de vuelo máximo. Todos son configurables. El tiempo máximo de vuelo también se pausa cuando se pierde el tracking.

La destrucción o desactivación externa de un avión se resuelve una sola vez. Los robos reales del sistema de pájaros cuentan como robos, no como choques, y permiten volver a lanzar mientras el pájaro se lleva el anterior.

`ScoreManager` rechaza cantidades nulas/negativas y puntos después del final. Los gestores duplicados no destruyen el GameObject que pueda contener otros sistemas. Las suscripciones se limpian al cambiar de escena.

`GameRA` incluye un HUD con objetivo, reloj, aviones ∞, lanzamientos y estado del seguimiento; paneles de victoria/derrota con estadísticas y botones para repetir o volver al menú. Se adapta a vertical/horizontal y al área segura del móvil. El generador de globos de esta escena tiene desactivada la simulación permanente de marca.

## Integración con las otras tareas

- Para lanzar desde los gestos, utilizar `TouchFlightController.TryLaunch(posicionPantalla)`. El controlador registra el avión con el gestor únicamente después de crearlo e inicializarlo correctamente.
- Un controlador de lanzamiento alternativo debe llamar a `GameManagerRA.TryRegisterLaunch(plane)` después de `PaperPlane.Initialize(...)` y respetar `PuedeLanzar`.
- Los globos deben utilizar `GloboTarget.PopBalloon(plane)`, que solicita la resolución a `GameManagerRA.TryPopBalloon`. No sumar puntos por segunda vez desde otro detector de colisiones.
- `PaperPlane.OnFinished(true/false)` se conserva para las integraciones existentes. El gestor distingue un robo consultando `IsCaptured` durante la notificación.
- `AvionesLanzados`, `AvionesAcertados`, `AvionesFallados`, `AvionesRobados`, `AvionesEnVuelo`, `TiempoRestante`, `PartidaEnCurso`, `PuedeLanzar` y `EsperandoSeguimiento` exponen el estado de la ronda.
- `AvionesIlimitados` identifica el modo infinito. En ese modo `AvionesRestantes` devuelve `int.MaxValue`; las interfaces deben mostrar ∞ utilizando la propiedad del modo, no imprimir ese número.
- `OnAvionesActualizados`, `OnPartidaIniciada` y `OnPartidaFinalizada` permiten conectar otras interfaces. El evento final comunica victoria/derrota y puntuación una sola vez.
- El botón Repetir carga `GameRA` y el botón Menú carga `FlyingFreeMenu`. `IniciarPartida()` permite reiniciar los contadores y objetos de la ronda actual; también restablece los globos generados y retira los aviones que estuvieran siendo transportados.

Esta rama parte de la entrega de pájaros de la [solicitud #3](https://github.com/Diabolo-kun/Proyecto-RA--Flying-Free/pull/3), aún pendiente de incorporación al preparar esta entrega. Incluye esa base para poder verificar los robos reales junto a la lógica de partida.

## Cómo abrir y probar

1. Abre `Unity/FlyingFree`. Se mantiene la versión declarada del equipo, **6000.3.6f1**.
2. Abre `Assets/Scenes/GameRA.unity` para jugar con la marca.
3. Para pruebas automáticas sin cámara, detén Play, guarda tus escenas y ejecuta **Flying Free > Sprint 2b > Probar puntuación y partida**.
4. Los resultados e imágenes se escriben en `Logs/PruebasPartida/`, excluido de Git. El ensayo utiliza el gestor, HUD, botones y cinco prefabs reales; al final carga realmente las escenas de juego y menú. El seguimiento se controla explícitamente como simulación.
5. Las pruebas de pájaros siguen disponibles en **Flying Free > Sprint 2b > Probar sistema de pájaros**.

## Validación realizada

- Pull de `Sprint2` hasta `ca838a1`; base de pájaros `54f2b22`.
- **56 comprobaciones de partida aprobadas en Play Mode.** Incluyen 25 lanzamientos fallidos consecutivos sin consumir munición; puntuación única con los cinco modelos; victoria, derrota, relojes, callbacks tardíos, pérdida de aviones, robos reales, reinicio y navegación real.
- **87 comprobaciones de pájaros aprobadas nuevamente** con la lógica de partida nueva.
- HUD y resultados revisados mediante imágenes reales en orientación vertical y horizontal.
- Compilación completa de desarrollo para Windows con menú, selección, `GameRA` y tráiler: **Succeeded, 0 errores**, en **Unity 6000.6.3f1 con Vuforia 10.17.4**. Los 39 avisos de compilación incluyen APIs compatibles con la versión 6000.3 del equipo que la versión instalada 6000.6 marca como obsoletas, además de avisos de paquetes/shaders. No se ha cambiado la versión declarada del proyecto.

Los informes y capturas están en esta carpeta. Falta comprobar cámara, seguimiento y escala con **un móvil y la marca física**; las pruebas controladas en el editor no sustituyen esa validación en el dispositivo.
