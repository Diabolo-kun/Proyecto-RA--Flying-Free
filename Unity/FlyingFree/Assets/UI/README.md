# Flying Free — Menú principal para Unity · versión 2

Interfaz real y editable de uGUI, con escena, prefab y scripts. Esta revisión usa el **logo original aportado por el usuario**, fondos de parque ilustrados a partir de su referencia y botones independientes con textura, marco, relieve e iconos. Los textos y controles son elementos reales de Unity; no están incrustados en una imagen de menú. No requiere cámara de RA, shaders personalizados ni servicios externos en ejecución.

## Importar en vuestro proyecto

1. Haz una copia de seguridad del proyecto. Abre **Assets > Import Package > Custom Package…** y selecciona `FlyingFree-Menu-v2.unitypackage`. Importa la carpeta `Assets/FlyingFree` completa. Si ya importaste la primera versión, acepta actualizar los archivos de esa carpeta: se conserva la identidad del prefab y de la escena.
2. Comprueba que el proyecto tiene **Unity UI / com.unity.ugui** instalado desde Package Manager. El proyecto autónomo incluye la dependencia. Se ha usado Unity **6000.6.3f1**, con uGUI **2.0.0**. Otras versiones no están verificadas.
3. Abre `Assets/FlyingFree/UI/Scenes/FlyingFreeMenu.unity` y pulsa Play. Hay exactamente tres botones en la página principal: **JUGAR, CONFIGURACIÓN y SALIR**.
4. Como alternativa, arrastra `Assets/FlyingFree/UI/Prefabs/FlyingFreeMenu.prefab` a vuestra escena. El prefab ya incluye Canvas, CanvasScaler y GraphicRaycaster. Evita colocarlo dentro de otro Canvas: está diseñado como Canvas raíz.
5. Conserva un único EventSystem activo. Si ya existe uno, el menú lo reutiliza; si no existe, crea uno durante Play. Es compatible con el Input Manager clásico y con Input System instalado y activado. Para Input System, el EventSystem existente debe usar InputSystemUIInputModule con acciones de UI configuradas.

## Abrir el proyecto autónomo

Descomprime `FlyingFree-Proyecto-Unity-v2.zip`, añade su carpeta desde **Unity Hub > Add project from disk** y ábrela con Unity 6000.6.3f1. Abre la escena indicada arriba. `Library`, `Temp` y las cachés no se distribuyen: Unity las reconstruye. El proyecto contiene los ajustes de autorrotación y la escena en Build Profiles. La importación del `.unitypackage` no modifica los ajustes globales de vuestro proyecto.

## Conectar la lógica del juego

Selecciona la raíz **Flying Free • Menú**. El componente **Flying Free Menu** expone estos eventos en el Inspector:

| Evento | Uso |
| --- | --- |
| On Play Requested | Arrastra el objeto controlador del juego y elige su método público para iniciar RA o cargar su escena. |
| On Exit Requested | Acción opcional para guardar estado, volver a una pantalla de vuestro juego o tramitar la salida. |
| On Settings Opened / Closed | Responder a la apertura o cierre del panel. |
| On Sound Volume Changed (float) | Volumen efectivo de efectos, entre 0 y 1. |
| On Music Volume Changed (float) | Volumen efectivo de música, entre 0 y 1. |
| On Mute Changed (bool) | Estado global del control «Silenciar todo». |

Escoge la opción **Dynamic float / Dynamic bool** al enlazar un parámetro desde el Inspector. Los eventos también son públicos y aceptan `AddListener` / `RemoveListener`. Las propiedades `SoundVolume`, `MusicVolume`, `Muted` e `IsSettingsOpen` permiten consultar el estado. `PublishAudio()` vuelve a emitir los valores actuales, por ejemplo al crear un sistema de sonido después del menú.

**JUGAR emite el evento y permanece en el menú hasta que vuestro código responda.** No se presupone ningún nombre de escena ni se inicia una sesión AR. Desactiva la raíz del menú o descárgalo cuando entre la escena de juego para retirar también sus elementos de UI y su EventSystem, si lo creó el propio menú.

**SALIR:** por defecto emite el evento y pide cerrar la aplicación en Android y escritorio. En el Editor escribe una confirmación en Console, sin detener Play. En iOS y WebGL solamente emite el evento: no intenta forzar el cierre de esas plataformas. Desmarca **Quit Application On Exit** si vuestro compañero quiere gestionar toda la salida desde el evento, especialmente si carga otra escena o hace un guardado asíncrono. No se inicia ningún guardado del juego desde este paquete.

Los Button.onClick se enlazan al controlador en Awake. No añadas además la misma acción de abrir/cerrar configuración a sus listas OnClick, porque duplicarías llamadas. Conecta la lógica externa en los eventos del componente FlyingFreeMenu.

## Configuración de sonido y música

El panel incluye dos sliders continuos con porcentajes, un toggle **Silenciar todo** y **VOLVER AL MENÚ**. Mientras está abierto, el menú principal se desactiva y el fondo de configuración intercepta los toques. Escape / botón Atrás de Android cierra el panel cuando la plataforma lo entrega como Escape. No sale del juego desde el menú principal por pulsar Atrás.

Los valores se guardan mediante PlayerPrefs con las claves `FlyingFree.UI.Sound`, `FlyingFree.UI.Music` y `FlyingFree.UI.Muted`; se guardan al volver, salir y pausar la aplicación. Por defecto: sonido 80 %, música 60 %, silencio desactivado. Silenciar emite volumen efectivo 0; al desactivar silencio recupera los niveles elegidos. Los sliders conservan sus valores aunque el silencio esté activo.

El paquete **no incluye pistas ni efectos de audio**. Para oír la respuesta de los controles, enlaza vuestros sistemas de audio a los eventos o añade **MenuAudioAdapter** a un objeto, asigna `Menu` y los AudioSources de efectos/música correspondientes. Cada fuente recibirá el volumen efectivo. El adaptador es opcional y no modifica AudioListener.volume ni un AudioMixer global. Si vuestro juego usa un mezclador con decibelios, realizad la conversión en vuestro propio receptor.

## Orientaciones, safe area y personalización

- En vuestro proyecto móvil, abre **Project Settings > Player > Resolution and Presentation**: Default Orientation **Auto Rotation**, permite Portrait y Landscape Left/Right. Portrait Upside Down es opcional. Cambia también las opciones equivalentes de iOS si procede.
- `ResponsiveMenu` usa la safe area del dispositivo y actualiza anchors al cambiar tamaño, orientación o recortes. El logo se sitúa centrado arriba y los tres botones debajo. En horizontal se reducen el logo y la separación vertical para conservar el encuadre de referencia; en vertical el logo ocupa más altura. Se usa CanvasScaler con Scale With Screen Size / Expand.
- El fondo cubre toda la pantalla. Marca, botones y configuración permanecen dentro de la zona segura. El panel de configuración se escala para caber también en pantallas bajas. Se ha diseñado para móviles y tablets; ventanas extremadamente pequeñas o formatos ajenos a móvil pueden reducir el texto y deben revisarse.
- El campo **Simulated Safe Area** se deja en cero para producción. Sirve para simular un recorte en píxeles al revisar en el Editor. En Device Simulator se usa `UnityEngine.Device.Screen.safeArea`.
- Edita textos, colores y tamaños dentro del prefab. `ResponsiveMenu` controla los rectángulos exteriores Marca, Acciones y Panel configuración: para cambiar la composición de ambas orientaciones, modifica sus valores en ese script. Los elementos interiores son uGUI estándar editables.
- `MenuBackground` elige el fondo horizontal o vertical y ajusta su recorte sin deformarlo. Puedes sustituir sus dos texturas desde el Inspector.
- Cada botón conserva un área táctil fija y un hijo **Arte y contenido** con sprite, texto e icono. `MenuButtonFeedback` anima ese hijo al pasar, enfocar o pulsar el botón; usa tiempo no escalado. `MenuIconGraphic` dibuja los iconos de forma independiente.
- El avión forma parte del logo original. No se ha añadido otro avión separado ni se ha recreado el logotipo con una fuente.
- Para usar el prefab sobre la cámara AR, puedes desactivar su hijo **Parque low-poly**; la configuración y los eventos siguen siendo independientes. El menú por defecto es opaco y de pantalla completa.

## Organización

```
Assets/FlyingFree/UI/
  Art/V2/FlyingFreeLogo.png     Logo original con su transparencia
  Art/V2/ParkLandscape.png      Fondo horizontal sin texto ni botones
  Art/V2/ParkPortrait.png       Fondo vertical sin texto ni botones
  Art/V2/ButtonBlue.png         Botón azul vacío con relieve y transparencia
  Art/V2/ButtonCream.png        Botón crema vacío con relieve y transparencia
  Art/V2/Button*.asset          Recorte y bordes de importación de los sprites
  Art/V2/Generation.json        Prompts exactos y procedencia de los assets
  Art/RoundedPanel.png          Bordes del panel de configuración y sliders
  Fonts/Kalam-Bold.ttf          Tipografía manuscrita de botones y ajustes
  Fonts/OFL-Kalam.txt           Licencia abierta de la tipografía
  Prefabs/FlyingFreeMenu.prefab Canvas y jerarquía completa de UI
  Scenes/FlyingFreeMenu.unity   Escena de muestra con instancia del prefab
  Runtime/                     Controlador, eventos, responsive, arte e input
  Editor/                      Generador y comprobación reproducible
  README.md                    Esta guía
  LICENSE.txt                  Licencia del código y arte original
```

La tipografía es **Kalam Bold**, de Indian Type Foundry, distribuida con su licencia SIL Open Font License. No es necesario importar TextMeshPro. El logo se copia sin alterar sus píxeles; conserva su avión, papel rayado, letras y transparencia. Los fondos y los dos botones vacíos se generaron con la herramienta integrada `image_gen`, siguiendo la referencia visual aportada. Los prompts exactos están en `Art/V2/Generation.json`. Los antiguos scripts de geometría de la primera revisión pueden permanecer en Runtime por compatibilidad, pero la escena v2 no los utiliza.

## Regenerar y verificar

La escena y el prefab **ya se entregan generados**. **Flying Free > Crear escena y prefab de muestra** sirve para reconstruirlos desde código si se necesita; sobrescribe esos dos assets y sustituye la escena abierta. Úsalo en una copia o en el proyecto autónomo, después de guardar cualquier trabajo. El generador también establece escena de arranque y autorrotación en ese proyecto. No lo ejecutes para una importación normal.

`FlyingFreeQA.Run` es una comprobación automatizada de Editor que entra en Play Mode, acciona botones mediante PointerClick, comprueba eventos, volumen, silencio y persistencia, y renderiza seis tamaños/composiciones mediante una cámara de Unity. Se ejecuta por línea de comandos con `-batchmode -projectPath RUTA -executeMethod FlyingFree.UI.Editor.FlyingFreeQA.Run -logFile RUTA_LOG` (sin `-quit`, porque finaliza por sí misma). Hazlo en una copia: modifica y después elimina las tres preferencias de muestra y cierra el Editor al terminar. Las capturas y el informe se escriben en `../deliverables/Previews-v2` respecto a la raíz del proyecto.

Revisa `VALIDACION-v2.md`, junto al ZIP entregado, para ver exactamente qué comprobaciones se realizaron. Las capturas de escritorio no sustituyen una prueba táctil ni una compilación en un teléfono físico con notch, cambios de orientación y cámara AR.

Fuentes técnicas consultadas: [Unity Screen.safeArea](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/screen/safearea) y [Unity Input System UI Module](https://github.com/Unity-Technologies/InputSystem/blob/develop/Packages/com.unity.inputsystem/Documentation~/introduction-ui-input-module.md).
