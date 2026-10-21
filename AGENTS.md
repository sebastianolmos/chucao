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
- Implementar el temporizador únicamente cuando forme parte de la tarea solicitada.

## Forma de trabajo

- Leer estas instrucciones y el código relevante antes de modificar el proyecto.
- Implementar cambios pequeños y centrados en la tarea.
- Evitar refactorizaciones ajenas a la tarea.
- Mantener una separación sencilla entre interfaz, lógica de práctica y acceso a archivos.
- Ubicar vistas y su código visual en `Views`, estado y acciones de pantalla en `ViewModels`, acceso a archivos en `Services` y traducciones en `Resources`. Crear `Models` y `Assets` solo cuando se necesiten.
- Evitar arquitectura y abstracciones innecesarias.
- Utilizar nombres de código descriptivos en inglés.
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
- Antes de implementar las rondas, acordar cuándo un concepto cuenta como utilizado y cómo comienza una nueva ronda.
- Distinguir «ya apareció» de «práctica completada» si el comportamiento acordado requiere ambos estados.
- Conservar listas y progreso entre ejecuciones.
- Guardar datos modificables fuera del ejecutable, en una carpeta persistente del usuario.
- No sobrescribir ni reiniciar datos existentes sin una acción explícita del usuario.
- Guardar cada lista en un CSV con columnas `Concept,SeenOrder`, sin columnas de identificadores ni versión. El nombre visible se recupera del nombre de archivo interno codificado; permitir caracteres no válidos para nombres de archivo Windows.
- Leer y escribir CSV con una biblioteca que maneje separadores, comillas y caracteres especiales.
- Manejar listas vacías, líneas vacías y archivos inválidos con mensajes comprensibles.
- Acordar la política de duplicados al implementar la importación.

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
