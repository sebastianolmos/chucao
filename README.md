# Chucao: Práctica de conceptos

Aplicación de escritorio para Windows que permite practicar la explicación oral de conceptos mediante selección aleatoria, sin repetirlos hasta completar una ronda.


## Estado

Proyecto en etapa inicial. Las siguientes funciones se implementarán progresivamente.

La ventana inicial tiene tamaño fijo y permite cambiar entre inglés y español con el botón ES/EN de la esquina superior derecha. En la esquina izquierda, un botón con engranaje abre las configuraciones. La barra de la ventana muestra «Chucao». Dentro de la aplicación se muestra «Chucao» en grande y debajo el subtítulo traducido «Práctica de conceptos» / «Concept Practice». El primer inicio utiliza inglés y los cambios se guardan en `%LocalAppData%\Chucao\settings.csv` (columnas `Key,Value`). Ese archivo también guarda `TimerMinutes`. Si una preferencia es ilegible, se avisa y se conserva el archivo sin sobrescribirlo automáticamente.

Las traducciones están centralizadas en `src/Chucao/Resources/Translations.csv`, con columnas `Key,es,en`. El archivo se integra como recurso en el ejecutable; CsvHelper lee las traducciones y lee/escribe las preferencias.

La vista principal muestra cuadros de listas en dos columnas dentro de un contenedor con desplazamiento y un botón «+» al final. Las tarjetas tienen una altura mínima de 60 unidades de WPF. Debajo del contenedor se muestra el botón «Temporizador». Se pueden crear listas con nombres de hasta 30 caracteres (no vacíos), abrir su detalle y eliminarlas con confirmación. «Volver» permite regresar sin eliminarlas. Los formularios permiten cancelar con el botón correspondiente o con Escape.

Las listas se descubren al iniciar desde `%LocalAppData%\Chucao\Lists`, sin listas de ejemplo. Crear una lista escribe su CSV; confirmar su eliminación borra ese archivo. La interfaz se actualiza después de que la operación termina correctamente. Los nombres no cambian al alternar el idioma.

Cada CSV utiliza exclusivamente las columnas `Concept,SeenOrder`. Una lista nueva contiene solo la cabecera. `SeenOrder` admite 0 para conceptos no vistos y enteros positivos sin repetir para el orden de aparición. Los CSV no reconocidos, ilegibles o inválidos se omiten con un aviso y permanecen sin cambios; las listas válidas se cargan igualmente.

El detalle muestra los conceptos no vistos (`SeenOrder = 0`) en un contenedor con desplazamiento. Cada fila tiene dos botones alineados a la derecha: «Visto» y «Eliminar». Marcar como visto asigna el siguiente número después del mayor `SeenOrder` y oculta la fila sin borrarla del CSV. Eliminar borra permanentemente solo esa fila, que no reaparece al reiniciar. Debajo del nombre de la lista se muestra el concepto con el mayor orden positivo como último visto, o un aviso si todavía no hay ninguno.

«Agregar concepto» abre un formulario con un límite de 30 caracteres y guarda el texto con `SeenOrder = 0`. Se rechazan textos vacíos o compuestos solo por espacios y conceptos exactamente iguales dentro de la misma lista, incluyendo los vistos, distinguiendo mayúsculas de minúsculas. Los textos ingresados se conservan tal como se escriben. Los conceptos existentes no se borran ni se deduplican automáticamente.

Debajo del contenedor aparecen en una fila «Agregar concepto» a la izquierda, «Agregar conjunto» en el centro y «Reiniciar lista» a la derecha. En la fila inferior, «Seleccionar concepto aleatorio» ocupa aproximadamente dos tercios a la izquierda y «Eliminar lista» un tercio a la derecha. Reiniciar guarda `SeenOrder = 0` para todos los conceptos de la lista actual sin modificar sus textos ni su orden, vuelve a mostrarlos y borra la indicación del último visto. Cuando todos están vistos, el contenedor muestra un mensaje para reiniciar explícitamente; una lista sin conceptos muestra un mensaje distinto. Los cambios se escriben primero en un archivo temporal y reemplazan el CSV solo al completar la escritura. Si falla el guardado, la interfaz mantiene sus datos anteriores y muestra un aviso traducido.

La selección aleatoria lee el CSV actual y elige con igual probabilidad entre las filas con `SeenOrder = 0`. Guarda el elegido con el siguiente orden positivo, actualiza el último visto y lo oculta de la lista antes de abrir un cuadro con el texto destacado. «Volver» o Escape cierran ese cuadro sin deshacer el estado guardado. «Abrir temporizador» lleva el concepto a la vista del temporizador. El sorteo queda desactivado cuando no hay conceptos disponibles; no comienza otra ronda automáticamente. Si falla el guardado, no se muestra ningún elegido ni se altera el progreso en pantalla.

Las configuraciones permiten seleccionar minutos enteros de 1 a 59, con 1 minuto por defecto. «Volver» guarda la duración; Escape descarta la edición. Los archivos de preferencias antiguos que solo incluyen el idioma siguen siendo válidos y usan 1 minuto hasta que se guarda otra duración. Guardar idioma o duración conserva las demás claves. Una duración inválida al iniciar produce un aviso y usa 1 minuto sin modificar el archivo.

El temporizador se abre desde la vista principal (sin concepto) o desde un concepto elegido (mostrando su texto). Tiene un título, un Canvas con el recorrido del ave, una zona de concepto y números grandes en formato `mm:ss`. No comienza automáticamente: «Empezar» inicia la cuenta y luego el botón alterna entre «Pausar» y «Continuar». Usa tiempo monotónico transcurrido para evitar acumular retraso entre actualizaciones. Al llegar a `00:00` se detiene, reproduce una vez el sonido de finalización sin aviso visual y permite «Empezar de nuevo». El botón reserva desde el inicio el espacio de su etiqueta de reinicio para que el Canvas no se reduzca al terminar. Cambiar el idioma no reinicia la cuenta. «Volver» la detiene y restaura la vista anterior: la principal o el cuadro del concepto elegido, sin otro sorteo. Abrirlo otra vez prepara la duración completa configurada; no se persiste una cuenta en curso entre ejecuciones.

«Agregar conjunto» abre una caja multilínea para pegar conceptos. Cada línea debe comenzar con el guion ASCII `-` (se permiten espacios antes). Se retira solo ese primer guion y los espacios exteriores del concepto; se conservan mayúsculas, acentos y guiones interiores. Se admiten saltos de línea de Windows y otros sistemas. Se omiten líneas sin guion, conceptos vacíos, textos de más de 30 caracteres y duplicados exactos respecto de la lista completa (incluidos los vistos) y del mismo bloque. Los válidos se agregan en el orden pegado con estado 0 mediante un único guardado, sin modificar el progreso anterior.

Al confirmar un conjunto, el formulario se cierra y se muestra solo la cantidad añadida junto a «Volver», durante 10 segundos y sin ventana de resultado ni sonido. Si no se añadió ninguno se muestra 0. Agregar un concepto individual muestra «Se añadió un concepto». El aviso cambia de idioma durante la ejecución; una nueva adición reemplaza el conteo y reinicia los 10 segundos. Al salir de la lista se limpia. Ante un error de guardado no se anuncia éxito ni se agrega parcialmente el conjunto, y se conserva el texto pegado para reintentar. Los errores siguen usando los avisos traducidos existentes. Confirmar, cancelar y Escape funcionan como en los demás formularios.

Los nombres visibles admiten caracteres como `/`, `:` o `?`: el archivo usa `list-` seguido del nombre codificado como bytes UTF-8 en hexadecimal y la extensión `.csv`. Esa codificación reversible permite recuperar el nombre sin columnas adicionales ni un catálogo. No se debe renombrar manualmente el archivo. Se recortan los espacios exteriores y se rechazan nombres repetidos sin distinguir mayúsculas de minúsculas; nunca se sobrescribe una lista existente. Para respaldar las listas, copiar la carpeta completa.

## Organización del código

En el temporizador, «Volver» está en la esquina superior izquierda, frente al selector de idioma. El botón de iniciar/pausar/continuar queda centrado abajo con `Width="120"`, en `Views/TimerView.xaml` (`StartButton`). La fila flexible reservada para la animación absorbe el espacio liberado y desplaza hacia abajo el concepto y las cifras.

La distancia entre «Último concepto visto» y el texto del concepto se ajusta en `Views/ListDetailView.xaml`, en el `TextBlock` enlazado a `LastSeenConcept`: `Margin="0,12,0,0"`. El segundo valor es la separación superior, actualmente 12 unidades de WPF.

- `src/Chucao/Views`: ventana contenedora y vistas separadas de listas, detalle, creación y confirmación de eliminación. Sus archivos `.xaml.cs` solo coordinan presentación, foco y desplazamiento, además de conectar el selector de idioma con los servicios existentes.
- `src/Chucao/ViewModels`: estado de las listas, navegación y formularios en `MainViewModel`; `TimerViewModel` mantiene el estado de la cuenta atrás usando `TimeProvider`; `RelayCommand` conecta los botones con sus acciones mediante `ICommand` de WPF.
- `src/Chucao/Services`: localización, lectura/escritura de preferencias, persistencia de listas en `ListFileService` y elección entre conceptos disponibles en `ConceptSelectionService`.
- `src/Chucao/Resources`: traducciones y estilos tipográficos compartidos en `Typography.xaml`.
- `src/Chucao/Models`: `ConceptEntry` representa el texto y el estado de un concepto, sin dependencias de WPF.

La aplicación arranca desde `Views/MainWindow.xaml`. Los formularios de conceptos están en `Views/CreateConceptView.xaml` y `Views/AddConceptSetView.xaml`. `MainViewModel` procesa las líneas y expone el conteo añadido; `MainWindow.xaml.cs` oculta el aviso con un `DispatcherTimer` de WPF, exclusivo de esta notificación visual. `ListFileService` comprueba los duplicados sobre el CSV actual y guarda el conjunto completo. `Assets/Fonts`, en la raíz del repositorio, contiene las fuentes proporcionadas para la aplicación.

## Imágenes e icono del ave

`Assets/Images/stand.png` se muestra centrado encima de «Chucao» en la vista principal. La imagen ocupa 96 × 96 unidades de WPF, mantiene su proporción y reduce el espacio restante del contenedor de listas, que sigue siendo desplazable. Sus dimensiones y separación están en `Views/ListsView.xaml`, en el elemento `HomeBirdImage` (`Width`, `Height`, `Margin` y `Stretch`).

El icono del ejecutable usa `Assets/Images/icono.ico`, convertido desde `icono.png` con transparencia y tamaños 16, 24, 32, 48, 64, 128 y 256. El estilo compartido `AppWindowStyle` de `Resources/Typography.xaml` aplica el mismo icono a la barra de título de las ventanas y sus botones de la barra de tareas. Los PNG definitivos y el ICO generado se incluyen en Git; el sonido temporal sigue excluido.

Para reemplazar las imágenes:

1. Reemplazar `stand.png`, `fly1.png`, `fly2.png` o `icono.png` en `Assets/Images`. Después de cambiar `icono.png`, regenerar `icono.ico` con el conversor.
2. Si se cambian sus nombres o ubicaciones, ajustar `ApplicationImageFile`, `ApplicationIconFile`, `BirdFly1ImageFile` y `BirdFly2ImageFile` al principio de `src/Chucao/Chucao.csproj`. `ApplicationIcon` configura el ejecutable. Los alias internos permanecen estables, por lo que no hace falta cambiar las vistas ni `App.xaml.cs`.
3. Compilar de nuevo para actualizar los recursos y el icono del ejecutable.

`App.xaml.cs` carga y congela los cinco recursos gráficos antes de abrir la ventana: `ApplicationImage` (stand), `ApplicationWindowIcon` (icono), `BirdFly1Image`, `BirdFly2Image` y `StumpImage` (tronco). Las imágenes definitivas son recursos requeridos del proyecto y se distribuyen dentro de la aplicación.

El conversor reutilizable está en `tools/Convert-PngToIcon.ps1`. Desde la raíz del repositorio, ejecutarlo con `-InputPath .\Assets\Images\icono.png -OutputPath .\Assets\Images\icono.ico`; añadir `-Force` solo para reemplazar un ICO existente. Para ejecutarlo con una política limitada al proceso puede usarse `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\Convert-PngToIcon.ps1` seguido de esos parámetros. El conversor conserva el PNG original y centra imágenes no cuadradas sin deformarlas.

Para cambiar dimensiones de botones:

| Elemento | Lugar | Propiedades |
|---|---|---|
| Tarjetas de listas | `Views/ListsView.xaml`, botón del `DataTemplate` | `MinHeight="50"`, `Padding="8"`, margen `ListCardMargin` de `Palette.xaml`; el ancho lo reparte `UniformGrid Columns="2"`. |
| Agregar lista (+) | `Views/ListsView.xaml`, `AddListButton` | `MinHeight="40"` y `Margin`; se puede añadir `Padding`, `Height` o `Width`. El tamaño del símbolo se configura en `AddListFontSize` de `Typography.xaml`. |
| Temporizador principal | `Views/ListsView.xaml`, `TimerButton` | `Padding="12,10"` y `Margin="0,16,0,0"`; se puede fijar `Height` o `MinHeight`. |
| Otros botones | Su archivo de `Views` y los estilos de `Controls.xaml` | `Width`, `Height`, `MinWidth`, `MinHeight`, `Padding` y `Margin`. Los valores locales de las vistas tienen prioridad sobre el estilo. |

`MinHeight` es una altura mínima y permite crecer con el contenido; `Height` fija la altura. `Padding` es el espacio interior y `Margin` el exterior. Las medidas son unidades de WPF.

## Tipografía

Comfortaa © 2011 The Comfortaa Project Authors. Fuente utilizada bajo la **SIL Open Font License, versión 1.1**. El texto completo de la licencia está en [Assets/Fonts/OFL.txt](Assets/Fonts/OFL.txt).

La interfaz usa Comfortaa. Los archivos Light, Bold y Regular de `Assets/Fonts` están enlazados como recursos WPF en `src/Chucao/Chucao.csproj`, sin moverlos ni modificarlos; los pesos utilizados por cada rol se configuran en `RegularFontWeight` y `EmphasisFontWeight` de `Resources/Typography.xaml`. La licencia `Assets/Fonts/OFL.txt` se copia junto a la distribución.

Para cambiar los archivos de fuente, editar sus elementos `Resource Include` de `Chucao.csproj` y sus rutas `Link` si corresponde. Para cambiar la familia interna o los pesos, editar `AppFontFamily`, `RegularFontWeight` y `EmphasisFontWeight` al comienzo de `src/Chucao/Resources/Typography.xaml`. `#Comfortaa` es el nombre interno de la familia, no el nombre del archivo. `App.xaml` carga ese diccionario para toda la aplicación.

Los tamaños se configuran en las claves `...FontSize` de ese mismo diccionario:

| Grupo | Clave | Tamaño |
|---|---|---|
| Texto general y etiquetas | `BodyFontSize` | 12 |
| Botones | `ButtonFontSize` | 12 |
| Tarjetas de listas | `ListCardFontSize` | 16 |
| Campos de texto | `InputFontSize` | 16 |
| Selector y opciones de minutos | `MinutesFontSize` | 18 |
| Nombre Chucao y subtítulo | `AppTitleFontSize`, `SubtitleFontSize` | 36, 18 |
| Nombre de lista y título de página | `ListTitleFontSize`, `PageTitleFontSize` | 26, 28 |
| Títulos de formularios y confirmación | `DialogTitleFontSize`, `SmallDialogTitleFontSize` | 22, 20 |
| Título de configuraciones y rótulo del concepto elegido | `SettingsTitleFontSize`, `DialogCaptionFontSize` | 24, 20 |
| Conceptos y último visto | `ConceptFontSize`, `LastSeenFontSize` | 16, 16 |
| Concepto elegido y concepto del temporizador | `SelectedConceptFontSize`, `TimerConceptFontSize` | 32, 28 |
| Números del temporizador | `TimerDigitsFontSize` | 64 |
| Botón + e icono del engranaje | `AddListFontSize`, `IconFontSize` | 24, 18 |
| Ayudas y avisos temporales | `HelpFontSize`, `NoticeFontSize` | 12, 12 |

Los colores de texto se centralizan en `BodyTextBrush`, `TitleTextBrush`, `SecondaryTextBrush`, `ButtonTextBrush`, `InputTextBrush` y `NoticeTextBrush`, que toman `TextColor` de `Palette.xaml` (#5C2D00). Cada estilo de grupo puede ampliar o reemplazar propiedades como `Foreground`, `FontWeight`, `FontStyle` o `FontStretch`; las vistas asignan su estilo mediante `Style`. La geometría, los fondos y las plantillas de controles están separados en `Controls.xaml` y `Palette.xaml`.

Los avisos de error usan `Views/NoticeWindow.xaml` para que su contenido y botón también adopten Comfortaa. El engranaje sigue usando Segoe MDL2 Assets, una fuente de símbolos. La barra de título nativa y los menús de Windows conservan su tipografía de sistema; personalizarlos requiere trabajar el marco por separado.

Se comprobaron los nombres internos y la resolución de las caras Light/Bold con WPF sobre los archivos originales, además del XML y las referencias de estilos. No se ejecutó build ni revisión visual de la aplicación: después de compilar hay que comprobar texto en ambos idiomas, diálogos, desplegables, botones deshabilitados, teclado, cifras y posibles recortes dentro de la ventana fija.

## Estilo visual y ajustes

Los recursos se cargan en este orden desde `App.xaml`: `Palette.xaml` (colores y medidas), `Typography.xaml` (fuentes y tamaños) y `Controls.xaml` (plantillas y variantes de controles). No se modificaron las reglas de práctica, el CSV ni las preferencias.

El fondo de las vistas y cuadros es #FFEDC2, el texto y los contornos son #5C2D00 y el oscurecimiento de los formularios es #805C2D00: conserva el mismo alfa 80 que el fondo oscuro anterior. El marco y la barra de título nativos usan #FFBF4F, con texto #5C2D00, mediante `Views/NativeWindowColors.cs`. Mantienen sus controles y comportamiento estándar. Estos atributos se aplican desde Windows 11 build 22000; en versiones anteriores se conserva el marco del sistema. Véase la [documentación de atributos DWM](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute).

Los botones comparten contorno de una unidad, esquinas ligeramente redondeadas, indicador de foco de teclado y sombra marrón únicamente hacia la derecha y abajo. La sombra es una silueta desplazada, no un desenfoque alrededor del botón. Cada acción utiliza una variante semántica (verde, roja, morada, azul, rosa, naranja, celeste o amarilla). El botón de idioma elige sus colores y hover según `Localization.CurrentLanguage`. Los deshabilitados conservan la paleta y reducen su opacidad.

| Qué ajustar | Archivo y recurso |
|---|---|
| Color de todo el texto y de los bordes | `Resources/Palette.xaml`: `TextColor`. Los pinceles tipográficos apuntan a ese color. |
| Marco y barra de título | `WindowFrameColor`, `WindowCaptionColor`. La integración nativa está en `Views/NativeWindowColors.cs`. |
| Fondo de vistas y cuadros | `ViewBackgroundColor`. |
| Color y transparencia del oscurecimiento | `OverlayColor`; formato #AARRGGBB. El primer par controla la opacidad. |
| Color de cada botón y su hover | Pares `GreenColor`/`GreenHoverColor`, `RedColor`/`RedHoverColor`, `PurpleColor`/`PurpleHoverColor`, `BlueColor`/`BlueHoverColor`, `PinkColor`/`PinkHoverColor`, `OrangeColor`/`OrangeHoverColor`, `BackColor`/`BackHoverColor`, `SettingsColor`/`SettingsHoverColor` y `ListCardColor`/`ListCardHoverColor`. |
| Botón EN (estando en español) | `LanguageToEnglishColor` y `LanguageToEnglishHoverColor`. |
| Botón ES (estando en inglés) | `LanguageToSpanishColor` y `LanguageToSpanishHoverColor`. |
| Redondeado de todos los botones | `ButtonCornerRadius`, inicialmente 6. |
| Grosor del contorno | `ControlBorderThickness`, inicialmente 1. |
| Tamaño/desplazamiento de la sombra | `ButtonShadowOffsetX` y `ButtonShadowOffsetY`, inicialmente 3 y 3. Aumentarlos exige dejar más espacio entre controles. No hay radio de desenfoque porque la sombra es sólida. |
| Espacio interior general | `DefaultButtonPadding`, inicialmente 12,8; los `Padding` locales de las vistas prevalecen. |
| Separación de tarjetas | `ListCardMargin`, inicialmente 6 por lado. |
| Separación de acciones en dos/tres columnas | `LeftActionMargin`, `MiddleActionMargin` y `RightActionMargin`. Los lados internos de 6 + 6 dejan 12 entre botones. |
| Separación entre filas de acciones | `ActionRowMargin`, inicialmente 0,0,0,16. |
| Separación entre Visto y Eliminar | `InlineActionMargin`, con 10 a la derecha. |
| Separación de botones de formularios | `DialogActionMargin` y `FinalDialogActionMargin`, con espacio extra debajo para la sombra. |
| Aspecto de controles deshabilitados | `DisabledControlOpacity`, inicialmente 0.5. |
| Deslizador y fondo de desplazamiento | `ScrollThumbColor` (#FF931F), `ScrollTrackColor` (#FFBF4F) y `ScrollBarThickness` (14). Su plantilla está en `Controls.xaml`. |
| Campos de texto | `InputColor` (blanco), `InputCornerRadius` y la plantilla `TextBox` de `Controls.xaml`. |
| Selector y opciones de minutos | Las plantillas `ComboBox` y `ComboBoxItem` de `Controls.xaml`; usan `SettingsColor`, `SettingsHoverColor`, `ViewBackgroundColor` y `TextColor`. |
| Canvas de animación | En `Resources/Palette.xaml`: `AnimationColor` (cielo, #83DDF7), `AnimationGroundColor` (terreno, #86D964), `AnimationPathColor` (camino, #AAA184), `AnimationCanvasWidth` (380), `AnimationCanvasHeight` (260) y `AnimationCornerRadius` (12). El `Canvas` rectangular de `Views/TimerView.xaml` recorta su fondo y contenido con esquinas redondeadas. El recorte se ajusta al tamaño en `AnimationCanvas_SizeChanged`, en `Views/TimerView.xaml.cs`. El `Viewbox` lo reduce proporcionalmente si falta espacio: las medidas son unidades lógicas de WPF, no una garantía del tamaño visible. |
| Tamaño y ubicación de un botón concreto | `Width`, `Height`, `MinHeight`, `Padding` y `Margin` del botón en su archivo de `Views`. |

Los márgenes siguen el orden izquierda, arriba, derecha, abajo. Los valores de las vistas tienen prioridad sobre los setters de los estilos. Los colores se comparten mediante `DynamicResource`; las variantes de botones heredan de `AppButtonStyle`. Para crear otra variante, basarla en ese estilo y definir `Background` y su trigger `IsMouseOver`.

Se cargaron los tres diccionarios con WPF y se instanciaron las plantillas de botones, ScrollBar en ambas orientaciones y ComboBox sin abrir ventanas. Se comprobaron los colores normales de las variantes y el color de selección de minutos. No se ejecutó la build, las pruebas de la aplicación ni una revisión visual. Después de compilar hay que revisar hover en ambos idiomas, foco/teclado, desplazamiento con rueda y arrastre, selección de minutos, sombras sin recortes, los cuadros y el marco activo/inactivo en Windows 11.

## Recorrido del ave

El fondo se dibuja con figuras de WPF detrás del ave: cielo celeste, terreno verde y un camino trapezoidal que se estrecha hacia el horizonte. En `Views/TimerView.xaml`, el `Viewbox` interior usa un diseño de referencia de 366 × 256 y `Stretch="Fill"` para adaptarlo al ancho y alto del Canvas. El terreno comienza en `Canvas.Top="55"`; el polígono del camino se ajusta en `Points="168,55 208,55 289,256 63,256"`. Los colores están en la paleta. No requiere una imagen de fondo adicional.

Los cinco troncos usan `Assets/Images/tronco.png`, enlazado como `Assets/Images/Stump.png` en `Chucao.csproj` y cargado como `StumpImage` en `App.xaml.cs`. Se dibujan sobre el paisaje y detrás del ave. El arreglo `stumpPlacements` en `Views/TimerView.xaml.cs` contiene `(X, Y, Width, Flip)` en el orden de los puntos: X es el centro horizontal relativo (0–1), Y es el borde superior relativo (0–1), Width es la fracción del ancho del Canvas que ocupa la imagen y Flip refleja horizontalmente el tronco. La altura conserva la proporción original del PNG. Los tamaños aumentan hacia el primer plano, los troncos de la derecha están reflejados y los cercanos a los bordes se recortan con el Canvas, como en la referencia. Este arreglo es independiente de `animationWaypoints`, para poder ajustar los troncos sin cambiar el vuelo. Se recalculan al cambiar las dimensiones del Canvas.

El arreglo `animationWaypoints` en `Views/TimerView.xaml.cs` define los puntos en orden. Cada `new(x, y)` usa valores de 0 a 1, desde la esquina superior izquierda: X aumenta hacia la derecha e Y hacia abajo. Se pueden editar, añadir o quitar entradas. Las coordenadas se conservan para el vuelo, pero sus puntos de guía ya no se dibujan.

En `TimerView.xaml.cs`, `FinalFlightSeconds` (2) es la duración del último vuelo y `EarlierFlightIncrementSeconds` (2) es el incremento por cada vuelo anterior. Con cinco puntos, los vuelos duran 8, 6, 4 y 2 segundos. El tiempo restante se reparte por igual entre los primeros cuatro puntos, sin espera al final. Si la suma de vuelos supera el temporizador, cada tramo recibe exactamente `duración / cantidad de tramos`, sin esperas y con giro instantáneo en las llegadas. Este caso está comentado en `Views/BirdFlightTimeline.cs`.

`BirdInitialSize` (24) y `BirdFinalSize` (60), en `Resources/Palette.xaml`, definen la caja cuadrada de la imagen, conservando su proporción. El ave se posiciona por su centro y crece linealmente según el tiempo acumulado en vuelo; durante las esperas conserva el tamaño. Empieza mirando a la derecha, manteniendo la orientación original de las imágenes, y se refleja horizontalmente en cada llegada, incluida la última. La pose inicial usa `ScaleX = 1`. Sin puntos se oculta; con un único punto permanece en él con su tamaño inicial.

Durante las esperas, antes de iniciar, al pausar y al terminar se muestra `stand.png`. Durante cada vuelo se alternan `fly1.png` y `fly2.png`. La constante `FlightFrameSeconds = 0.15`, en `Views/TimerView.xaml.cs`, establece los segundos por imagen: un valor menor acelera el aleteo y uno mayor lo ralentiza; debe ser mayor que 0. Un ciclo de ambas imágenes tarda el doble. El cambio de imagen usa el tiempo transcurrido del temporizador y conserva el tamaño y orientación del ave. Revisar en ambos idiomas el icono, la portada, la alternancia en vuelo, las pausas, los giros y la llegada final; no se ejecutó la build ni se abrió la aplicación en esta tarea.

La animación usa `Timer.Duration` y `Timer.Elapsed`, derivados del mismo reloj monotónico que la cuenta atrás. La vista actualiza ambos al dibujar cada cuadro: pausar congela posición y tamaño; continuar retoma el mismo recorrido; reiniciar vuelve al primer punto. La llegada final coincide con `00:00`. El cálculo visual separado en `BirdFlightTimeline` distingue `Stopped` y `Flying`, sin introducir otra cuenta de tiempo. `tests/Chucao.Checks/BirdFlightChecks.cs` cubre tiempos, esperas, crecimiento, giros y el caso de vuelos que exceden la duración. Se añadieron comprobaciones del reloj compartido en `TimerChecks.cs`. No se ejecutaron build, pruebas ni revisión visual en esta tarea, siguiendo la preferencia del usuario; revisar el recorrido en ambas entradas al temporizador, pausa/continuación, reinicio, cambio de idioma y llegada final.

El sonido temporal está en `Assets/Sounds/bird.wav` y se excluye de Git. `TimerCompletionSoundFile`, en `src/Chucao/Chucao.csproj`, define el archivo fuente; al compilar o publicar se copia como `Assets/Sounds/TimerFinished.wav` junto a la aplicación. Para reemplazarlo, cambiar esa ruta y ajustar la regla de `.gitignore` cuando corresponda. En `Views/TimerView.xaml.cs`, `CompletionSoundVolume = 0.6` controla el volumen entre 0 (silencio) y 1 (máximo), además del volumen de Windows. Se reproduce una sola vez por cuenta terminada; reiniciar habilita una nueva reproducción y salir de la vista detiene el audio. Si falta el archivo temporal o falla su reproducción, el temporizador sigue funcionando. Un `DispatcherTimer` mantiene la comprobación del final incluso cuando la ventana minimizada deja de dibujarse. No se ejecutaron build ni reproducción de audio en esta tarea; revisar finalización, pausa, reinicio, ventana minimizada y salida durante el sonido.

## Funciones previstas

- Ampliar la administración de listas, incluyendo la edición de nombres.
- Editar conceptos existentes.
- Importar listas desde archivos TXT, con un concepto por línea.
- Personalizar la apariencia de la aplicación.
- Extender las traducciones en español e inglés a las nuevas funcionalidades.

## Tecnologías

- C# y .NET 10.
- WPF para la interfaz gráfica.
- CSV para persistencia, utilizando CsvHelper.

## Desarrollo

Requisitos:

- Windows.
- SDK de .NET 10.
- VS Code con C# Dev Kit o una versión compatible de Visual Studio con desarrollo de escritorio de .NET.

El proyecto está en `src/Chucao`. La ventana inicial tiene un tamaño fijo de 480 × 640 unidades de WPF y permite minimizarse.

Ejecutar desde la raíz del repositorio:

```powershell
dotnet build .\src\Chucao\Chucao.csproj
dotnet run --project .\src\Chucao\Chucao.csproj
```

## Comprobación

Las comprobaciones de traducciones, persistencia de preferencias y flujo de listas se ejecutan con:

```powershell
dotnet run --project .\tests\Chucao.Checks\Chucao.Checks.csproj
```

Usan archivos temporales y no modifican las preferencias ni las listas del usuario. Incluyen recuperación entre sesiones, creación y eliminación, nombres especiales y duplicados, CSV con comas/comillas/saltos de línea, archivos inválidos y fallos de acceso. La revisión visual incluye alternar ambos idiomas, usar Tab y Enter/Espacio en el botón y cerrar/abrir para comprobar la preferencia. Todavía no hay una práctica implementada para comprobar un cambio de idioma durante ella.

Para revisar manualmente las listas en ambos idiomas: comprobar las dos columnas con nombres largos y crear suficientes para activar el desplazamiento; escribir y pegar nombres de más de 30 caracteres; comprobar que no se acepten nombres vacíos ni repetidos; cancelar y confirmar la creación y eliminación; volver desde el detalle; cambiar el idioma sin perder las listas ni la vista actual; y cerrar/abrir para comprobar la persistencia. La barra de la ventana sigue mostrando «Chucao» y el título interior se oculta en el detalle.

Para los conceptos: agregar, cancelar y rechazar duplicados exactos; comprobar el límite al escribir y pegar; marcar varios como vistos y comprobar la desaparición de sus filas y el último visto; cerrar/abrir para verificar el progreso; completar una lista y comprobar el aviso; reiniciarla y verificar que reaparecen; eliminar un concepto y comprobar que no reaparece al reiniciar. Repetir en ambos idiomas y con teclado. Las comprobaciones de persistencia están en `tests/Chucao.Checks/ConceptChecks.cs` y `SeenConceptChecks.cs`; incluyen progreso, eliminación, reinicio, listas vacías/completas y fallos de escritura. En esta tarea no se ejecutaron ni compilación ni pruebas por petición del usuario.

Para conjuntos: pegar líneas con y sin `-`, duplicados (incluidos vistos), líneas vacías y textos largos; comprobar orden, estado 0 y conteo añadido. Verificar también un bloque sin conceptos válidos, cancelar, reintentar tras un fallo de guardado y volver a abrir la aplicación. Comprobar que el aviso junto a «Volver» desaparece tras 10 segundos, se renueva con otra adición (también individual), cambia de idioma y se limpia al salir. Las pruebas de `tests/Chucao.Checks/ConceptSetChecks.cs` y `ConceptChecks.cs` comprueban los conteos y su limpieza; la duración real y la apariencia requieren revisión de interfaz. En esta tarea no se ejecutaron pruebas ni build.

Para el sorteo: comprobar el reparto 2/3–1/3 de los botones, seleccionar hasta agotar los disponibles y verificar que no se repiten, que se actualiza el último visto y que el botón queda desactivado al completar la lista. Volver/Escape y cerrar/abrir deben conservar el estado. Reiniciar debe permitir otra ronda. Revisar también el cuadro y sus textos en ambos idiomas, el acceso con teclado y el acceso al temporizador. `tests/Chucao.Checks/RandomConceptChecks.cs` añade comprobaciones deterministas de candidatos disponibles, rondas, persistencia y fallos de guardado.

Para el temporizador: guardar 1 y 59 minutos y reabrir la aplicación; cancelar configuraciones con Escape; abrirlo desde la principal y desde un concepto; iniciar, pausar, continuar, terminar y empezar de nuevo. Volver debe detener la cuenta y recuperar su origen sin alterar el concepto ni su estado visto. Verificar traducciones durante la cuenta, teclado, la altura del contenedor de listas y el área vacía de animación. `tests/Chucao.Checks/TimerChecks.cs` usa un reloj simulado, sin esperas reales, para comprobar la cuenta atrás y añade casos de preferencias antiguas, rangos y fallos de guardado. En esta tarea no se ejecutaron ni build ni pruebas, siguiendo la preferencia del usuario.

## Distribución

Se prevé publicar una versión autocontenida para Windows que incluya el runtime de .NET y pueda abrirse mediante doble clic.

Los archivos de datos se conservarán fuera del ejecutable para mantener las listas y el progreso al actualizar la aplicación.

---

## Summary

**Chucao: Concept Practice** is a Windows desktop application for practicing oral explanations of randomly selected concepts, without repetition until a round is complete.

The project is at an early stage. Planned features include:

- Extend list management, including renaming.
- Import TXT files with one concept per line.
- Add concept editing and progress saving using the existing CSV format.
- Customize the interface.
- Extend Spanish and English translations to new features.
- Add an animation to the timer's reserved area.

The initial window already supports switching between English and Spanish with the top-right ES/EN button. It starts in English without a saved preference and remembers changes in `%LocalAppData%\Chucao\settings.csv`. Translations are embedded from `src/Chucao/Resources/Translations.csv`.

The main view displays scrollable list cards in two columns and a final + button. Users can create lists with nonblank names up to 30 characters, open a detail view, return, and delete with confirmation. Lists persist as individual CSVs in `%LocalAppData%\Chucao\Lists`, with only the `Concept,SeenOrder` columns. Internal filenames encode display names as UTF-8 hexadecimal, allowing names containing Windows-reserved characters without an additional catalog. Duplicate list names are rejected case-insensitively. Invalid files are reported and left unchanged. The detail view now displays concepts and allows adding nonblank texts up to 30 characters with state zero. Exact duplicate concepts within a list are rejected case-sensitively. Reset list saves zero for every concept without changing text or row order. Add set opens a multiline form accepting ASCII dash-prefixed lines. It skips blanks, texts longer than 30 characters and exact duplicates (including seen concepts and repeats within the pasted block). Accepted concepts are saved together in input order with state zero. A silent translated notice beside Back shows only the added count for ten seconds, for both single concepts and sets. New additions restart its lifetime; leaving the list clears it. Confirmed sets close the form even when zero are added; failed saves preserve the draft without partial additions and retain the existing error warnings. Changes replace the CSV only after a complete temporary file has been written.

Random selection now chooses only unseen concepts, saves the selected one as the latest seen, and shows it prominently in a modal panel with Back and an Open timer button. Back and Escape preserve the saved selection. The draw button takes the left two-thirds of the bottom row; Delete list takes the right third. Empty or exhausted lists cannot draw until concepts become available again. No automatic round restart occurs. Only unseen concepts are displayed. Each row has right-aligned Seen and Delete buttons. Marking assigns the next order, hides the row and updates the last seen concept below the list title. Deleting permanently removes the selected row. A completed list displays a reset message; an empty list displays a different message. Reset restores seen concepts and clears the last seen text. All progress survives reopening the application.

The application uses C#, .NET 10, WPF, and CsvHelper for CSV storage.

Development requires Windows and the .NET 10 SDK. From the repository root:

```powershell
dotnet build .\src\Chucao\Chucao.csproj
dotnet run --project .\src\Chucao\Chucao.csproj
```

A self-contained Windows release is planned. User data will remain outside the executable.

The timer is accessible from the home screen or a selected concept. Settings (top-left gear) persist a whole-minute duration from 1 to 59, defaulting to one, alongside the language preference. The timer shows mm:ss and supports Start, Pause, Resume and Start again after reaching zero. Back stops it and restores its origin; reopening prepares the full duration. Language changes preserve the countdown. Its view reserves blank space for a future animation. No build or tests were run for this change; TimerChecks includes a simulated clock and persistence checks.

Typography is centralized in src/Chucao/Resources/Typography.xaml. Comfortaa-Light.ttf is the regular role and Comfortaa-Bold.ttf is the emphasis role; Chucao.csproj links these two files from the repository's Assets/Fonts directory as WPF resources and distributes OFL.txt. Shared size keys and text brushes cover all UI groups. Custom WPF notices use the same font. The gear retains its icon font, and native Windows captions retain system typography. No build or visual app review was performed for this change.

The final artwork uses icono.png (converted to icono.ico) for the executable and window icon, stand.png for the home screen and stationary bird, and fly1.png/fly2.png for flight. Chucao.csproj selects their source paths; stable internal resource aliases keep views independent of filenames. The home image appears at 96 by 96 WPF units, configured in ListsView.xaml. FlightFrameSeconds in TimerView.xaml.cs controls seconds per wing image (0.15 by default). The final artwork and generated ICO are versioned. Convert-PngToIcon.ps1 converts PNG to ICO without modifying the source. No application build or visual app review was performed.

Visual styling now uses Palette.xaml for colors and geometry, Typography.xaml for fonts and Controls.xaml for shared control templates. Buttons have rounded corners, brown outlines and solid shadows offset right/down; named variants provide the requested normal and hover colors. Scrollbars, text fields, minute options and modal backgrounds use the shared palette. NativeWindowColors applies the requested frame, caption and caption-text colors on supported Windows 11 versions without replacing native window controls. Only headless WPF dictionary/template checks were run; no application build or visual review was performed.
