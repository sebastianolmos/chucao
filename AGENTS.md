# Instrucciones para trabajar en Chucao

## Contexto

Chucao es una aplicación de escritorio para Windows que permite practicar la explicación oral de conceptos mediante selección aleatoria sin repetición por ronda.

El desarrollo debe ser incremental y el código debe resultar comprensible para revisión humana.

## Tecnologías y alcance

- Utilizar C#, .NET 10 y WPF.
- Mantener el proyecto de aplicación en `src/Chucao`.
- Utilizar CSV para guardar listas y progreso.
- Utilizar CsvHelper para leer y escribir CSV.
- Importar TXT en UTF-8, con un concepto por línea.
- No introducir servicios remotos, bases de datos ni frameworks adicionales sin una necesidad acordada.
- Mantener el temporizador de práctica separado de la persistencia y de los avisos visuales temporales.

## Forma de trabajo

- Leer estas instrucciones y el código relevante antes de modificar el proyecto.
- Implementar cambios pequeños y centrados en la tarea.
- Evitar refactorizaciones ajenas a la tarea.
- Mantener una separación sencilla entre interfaz, lógica de práctica y acceso a archivos.
- Ubicar vistas y su código visual en `Views`, estado y acciones de pantalla en `ViewModels`, acceso a archivos en `Services` y traducciones en `Resources`. Crear `Models` y `Assets` solo cuando se necesiten.
- Evitar arquitectura y abstracciones innecesarias.
- Utilizar nombres de código descriptivos en inglés.
- Centralizar fuentes y tamaños en `Resources/Typography.xaml`, paleta y medidas visuales en `Resources/Palette.xaml`, y plantillas en `Resources/Controls.xaml`. Conservar la fuente de símbolos del engranaje y los ajustes tipográficos existentes.
- Explicar las decisiones relevantes y los conceptos nuevos de C# o WPF.
- Aclarar decisiones funcionales ambiguas que afecten al comportamiento o a los datos. Resolver detalles rutinarios con criterio propio.

## Idiomas de la interfaz

- La aplicación debe soportar español e inglés.
- Incluir un botón o selector visible en el menú o la vista principal para alternar entre «Español» y «English».
- El cambio de idioma debe aplicarse durante la ejecución, sin reiniciar la aplicación ni perder el progreso o el estado de la práctica.
- Traducir los textos de interfaz: botones, menús, etiquetas, diálogos, avisos y mensajes de error.
- Centralizar los textos traducibles en recursos de localización separados de la lógica.
- Evitar textos de interfaz incrustados directamente en C# o XAML cuando deban traducirse.
- No traducir ni modificar automáticamente los conceptos o los nombres de listas ingresados por el usuario.
- Iniciar en inglés si no hay una preferencia guardada y recordar la elección entre ejecuciones.
- Comprobar el funcionamiento en ambos idiomas, incluyendo el cambio durante una práctica.
- Verificar que las traducciones se muestren completas y que el selector pueda utilizarse con teclado.

## Reglas funcionales y datos

- No repetir conceptos durante la ronda de la lista correspondiente.
- Un concepto cuenta como visto al marcarlo manualmente o seleccionarlo al azar. Una nueva ronda comienza solo al reiniciar explícitamente la lista.
- Distinguir «ya apareció» de «práctica completada» si el comportamiento acordado requiere ambos estados.
- Conservar listas y progreso entre ejecuciones.
- Guardar datos modificables fuera del ejecutable, en una carpeta persistente del usuario.
- No sobrescribir ni reiniciar datos existentes sin una acción explícita del usuario.
- Guardar cada lista en un CSV con columnas `Concept,SeenOrder`, sin columnas de identificadores ni versión. El nombre visible se recupera del nombre de archivo interno codificado; permitir caracteres no válidos para nombres de archivo Windows.
- Leer y escribir CSV con una biblioteca que maneje separadores, comillas y caracteres especiales.
- Manejar listas vacías, líneas vacías y archivos inválidos con mensajes comprensibles.
- Acordar la política de duplicados al implementar la importación.
- Cada concepto nuevo debe tener de 1 a 30 caracteres, no estar vacío y guardarse con `SeenOrder = 0`. Rechazar textos exactamente iguales dentro de la misma lista (comparación sensible a mayúsculas); no eliminar duplicados antiguos automáticamente.
- Marcar como visto asigna `max(SeenOrder) + 1` y oculta el concepto; el mayor orden positivo identifica el último visto. Reiniciar explícitamente devuelve todos los estados a 0. Eliminar borra solo la fila elegida y no se revierte al reiniciar. Distinguir una lista vacía de una con todos sus conceptos vistos.
- Sortear solo entre conceptos con estado 0 y guardar el elegido como visto antes de mostrarlo. Volver o cerrar el cuadro no deshace la selección; si no quedan disponibles, desactivar el sorteo sin reiniciar automáticamente.
- Al agregar conjuntos pegados, aceptar líneas con `-` inicial, quitar solo ese guion y los espacios exteriores, y omitir vacíos, textos de más de 30 caracteres y duplicados exactos (incluidos los vistos y los del mismo bloque). Conservar el orden y guardar los válidos con estado 0 en una sola operación.
- Tras agregar un concepto o conjunto, mostrar solo la cantidad añadida junto a «Volver» durante 10 segundos, sin ventana de resultado. Cada nueva adición reinicia el plazo y salir de la lista limpia el aviso. Traducir el texto y admitir el conteo 0.
- El temporizador usa minutos enteros de 1 a 59 (1 por defecto), persistidos en `settings.csv`. Inicia manualmente y admite pausa/continuación; llega a 00:00 sin valores negativos. Volver lo detiene y restaura la vista de origen; abrirlo de nuevo prepara la duración completa. Escape en configuraciones descarta cambios; «Volver» los guarda.
- Configuraciones guarda duración, volumen (0–1, 0.6 por defecto) y sonido activado/desactivado juntos. Probar sonido usa el volumen pendiente incluso si el sonido final está desactivado. Los créditos se superponen a Configuraciones; «Volver» o Escape cierra solo los créditos y conserva los cambios pendientes.

## Comprobación

- Después de cambios de código, ejecutar:
  `dotnet build .\src\Chucao\Chucao.csproj`
- Añadir y ejecutar pruebas pertinentes para sorteo, rondas y persistencia cuando se implementen.
- No crear pruebas que solo reproduzcan detalles de implementación.
- Comprobar la interfaz en Windows cuando el entorno lo permita.
- Una compilación correcta no sustituye la revisión de enlaces de datos, interacción y apariencia.
- Si una comprobación no puede ejecutarse, informar la limitación y los pasos de revisión manual.
- No afirmar que una comprobación pasó si no se ejecutó.

## Git y documentación

- No hacer commits ni push salvo que el usuario los solicite.
- Conservar los cambios existentes del usuario.
- Evitar subir salidas de compilación, archivos personales de práctica o credenciales.
- Al terminar una tarea, revisar si README.md o AGENTS.md quedaron desactualizados.
- Actualizar información factual obsoleta y explicar los cambios.
- Proponer cambios de acuerdos en lugar de modificarlos unilateralmente.
- Mantener AGENTS.md breve y vigente.
- Registrar tareas pendientes en un plan separado si hace falta.
- Informar qué cambió, cómo se comprobó y qué revisión manual queda pendiente.
