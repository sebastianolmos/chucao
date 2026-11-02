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

El temporizador se abre desde la vista principal (sin concepto) o desde un concepto elegido (mostrando su texto). Tiene un título, espacio vacío reservado para una futura animación, una zona de concepto y números grandes en formato `mm:ss`. No comienza automáticamente: «Empezar» inicia la cuenta y luego el botón alterna entre «Pausar» y «Continuar». Usa tiempo monotónico transcurrido para evitar acumular retraso entre actualizaciones. Al llegar a `00:00` se detiene, muestra «Tiempo terminado» sin sonido y permite «Empezar de nuevo». Cambiar el idioma no reinicia la cuenta. «Volver» la detiene y restaura la vista anterior: la principal o el cuadro del concepto elegido, sin otro sorteo. Abrirlo otra vez prepara la duración completa configurada; no se persiste una cuenta en curso entre ejecuciones.

«Agregar conjunto» abre una caja multilínea para pegar conceptos. Cada línea debe comenzar con el guion ASCII `-` (se permiten espacios antes). Se retira solo ese primer guion y los espacios exteriores del concepto; se conservan mayúsculas, acentos y guiones interiores. Se admiten saltos de línea de Windows y otros sistemas. Se omiten líneas sin guion, conceptos vacíos, textos de más de 30 caracteres y duplicados exactos respecto de la lista completa (incluidos los vistos) y del mismo bloque. Los válidos se agregan en el orden pegado con estado 0 mediante un único guardado, sin modificar el progreso anterior.

Al confirmar un conjunto, el formulario se cierra y se muestra solo la cantidad añadida junto a «Volver», durante 10 segundos y sin ventana de resultado ni sonido. Si no se añadió ninguno se muestra 0. Agregar un concepto individual muestra «Se añadió un concepto». El aviso cambia de idioma durante la ejecución; una nueva adición reemplaza el conteo y reinicia los 10 segundos. Al salir de la lista se limpia. Ante un error de guardado no se anuncia éxito ni se agrega parcialmente el conjunto, y se conserva el texto pegado para reintentar. Los errores siguen usando los avisos traducidos existentes. Confirmar, cancelar y Escape funcionan como en los demás formularios.

Los nombres visibles admiten caracteres como `/`, `:` o `?`: el archivo usa `list-` seguido del nombre codificado como bytes UTF-8 en hexadecimal y la extensión `.csv`. Esa codificación reversible permite recuperar el nombre sin columnas adicionales ni un catálogo. No se debe renombrar manualmente el archivo. Se recortan los espacios exteriores y se rechazan nombres repetidos sin distinguir mayúsculas de minúsculas; nunca se sobrescribe una lista existente. Para respaldar las listas, copiar la carpeta completa.

## Organización del código

- `src/Chucao/Views`: ventana contenedora y vistas separadas de listas, detalle, creación y confirmación de eliminación. Sus archivos `.xaml.cs` solo coordinan presentación, foco y desplazamiento, además de conectar el selector de idioma con los servicios existentes.
- `src/Chucao/ViewModels`: estado de las listas, navegación y formularios en `MainViewModel`; `TimerViewModel` mantiene el estado de la cuenta atrás usando `TimeProvider`; `RelayCommand` conecta los botones con sus acciones mediante `ICommand` de WPF.
- `src/Chucao/Services`: localización, lectura/escritura de preferencias, persistencia de listas en `ListFileService` y elección entre conceptos disponibles en `ConceptSelectionService`.
- `src/Chucao/Resources`: traducciones.
- `src/Chucao/Models`: `ConceptEntry` representa el texto y el estado de un concepto, sin dependencias de WPF.

La aplicación arranca desde `Views/MainWindow.xaml`. Los formularios de conceptos están en `Views/CreateConceptView.xaml` y `Views/AddConceptSetView.xaml`. `MainViewModel` procesa las líneas y expone el conteo añadido; `MainWindow.xaml.cs` oculta el aviso con un `DispatcherTimer` de WPF, exclusivo de esta notificación visual. `ListFileService` comprueba los duplicados sobre el CSV actual y guarda el conjunto completo. No se crea `Assets` hasta que una funcionalidad lo necesite.

## Funciones previstas

- Ampliar la administración de listas, incluyendo la edición de nombres.
- Editar conceptos existentes.
- Importar listas desde archivos TXT, con un concepto por línea.
- Personalizar la apariencia de la aplicación.
- Extender las traducciones en español e inglés a las nuevas funcionalidades.
- Incorporar una animación al espacio reservado en el temporizador.

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
