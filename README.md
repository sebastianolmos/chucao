# Chucao: Práctica de conceptos

Aplicación de escritorio para Windows que permite practicar la explicación oral de conceptos mediante selección aleatoria, sin repetirlos hasta completar una ronda.


## Estado

Proyecto en etapa inicial. Las siguientes funciones se implementarán progresivamente.

La ventana inicial tiene tamaño fijo y permite cambiar entre inglés y español con el botón ES/EN de la esquina superior derecha. La barra de la ventana muestra «Chucao». Dentro de la aplicación se muestra «Chucao» en grande y debajo el subtítulo traducido «Práctica de conceptos» / «Concept Practice». El primer inicio utiliza inglés y los cambios se guardan en `%LocalAppData%\Chucao\settings.csv` (columnas `Key,Value`). Si la preferencia es ilegible, se avisa y se conserva el archivo sin sobrescribirlo.

Las traducciones están centralizadas en `src/Chucao/Resources/Translations.csv`, con columnas `Key,es,en`. El archivo se integra como recurso en el ejecutable; CsvHelper lee las traducciones y lee/escribe las preferencias.

La vista principal muestra cuadros de listas en dos columnas dentro de un contenedor con desplazamiento y un botón «+» al final. Se pueden crear listas con nombres de hasta 30 caracteres (no vacíos), abrir su detalle y eliminarlas con confirmación. «Volver» permite regresar sin eliminarlas. Los formularios permiten cancelar con el botón correspondiente o con Escape.

Las listas se descubren al iniciar desde `%LocalAppData%\Chucao\Lists`, sin listas de ejemplo. Crear una lista escribe su CSV; confirmar su eliminación borra ese archivo. La interfaz se actualiza después de que la operación termina correctamente. Los nombres no cambian al alternar el idioma.

Cada CSV utiliza exclusivamente las columnas `Concept,SeenOrder`. Una lista nueva contiene solo la cabecera. `SeenOrder` admite 0 para conceptos no vistos y enteros positivos sin repetir para el orden de aparición. Todavía no hay edición de conceptos ni práctica. Los CSV no reconocidos, ilegibles o inválidos se omiten con un aviso y permanecen sin cambios; las listas válidas se cargan igualmente.

Los nombres visibles admiten caracteres como `/`, `:` o `?`: el archivo usa `list-` seguido del nombre codificado como bytes UTF-8 en hexadecimal y la extensión `.csv`. Esa codificación reversible permite recuperar el nombre sin columnas adicionales ni un catálogo. No se debe renombrar manualmente el archivo. Se recortan los espacios exteriores y se rechazan nombres repetidos sin distinguir mayúsculas de minúsculas; nunca se sobrescribe una lista existente. Para respaldar las listas, copiar la carpeta completa.

## Organización del código

- `src/Chucao/Views`: ventana contenedora y vistas separadas de listas, detalle, creación y confirmación de eliminación. Sus archivos `.xaml.cs` solo coordinan presentación, foco y desplazamiento, además de conectar el selector de idioma con los servicios existentes.
- `src/Chucao/ViewModels`: estado de las listas, navegación y formularios en `MainViewModel`; `RelayCommand` conecta los botones con sus acciones mediante `ICommand` de WPF.
- `src/Chucao/Services`: localización, lectura/escritura de preferencias y persistencia de listas en `ListFileService`.
- `src/Chucao/Resources`: traducciones.

La aplicación arranca desde `Views/MainWindow.xaml`. No se crean carpetas `Models` ni `Assets` hasta que una funcionalidad las necesite.

## Funciones previstas

- Ampliar la administración de listas, incluyendo la edición de nombres.
- Agregar y editar conceptos.
- Importar listas desde archivos TXT, con un concepto por línea.
- Seleccionar conceptos aleatoriamente sin repetición dentro de cada ronda.
- Incorporar la edición de conceptos y el guardado del progreso sobre el formato CSV existente.
- Personalizar la apariencia de la aplicación.
- Extender las traducciones en español e inglés a las nuevas funcionalidades.
- Incorporar posteriormente un temporizador para la práctica oral.

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

## Distribución

Se prevé publicar una versión autocontenida para Windows que incluya el runtime de .NET y pueda abrirse mediante doble clic.

Los archivos de datos se conservarán fuera del ejecutable para mantener las listas y el progreso al actualizar la aplicación.

---

## Summary

**Chucao: Concept Practice** is a Windows desktop application for practicing oral explanations of randomly selected concepts, without repetition until a round is complete.

The project is at an early stage. Planned features include:

- Extend list management, including renaming.
- Import TXT files with one concept per line.
- Select random concepts without repetition within each round.
- Add concept editing and progress saving using the existing CSV format.
- Customize the interface.
- Extend Spanish and English translations to new features.
- Add a practice timer in a later version.

The initial window already supports switching between English and Spanish with the top-right ES/EN button. It starts in English without a saved preference and remembers changes in `%LocalAppData%\Chucao\settings.csv`. Translations are embedded from `src/Chucao/Resources/Translations.csv`.

The main view displays scrollable list cards in two columns and a final + button. Users can create lists with nonblank names up to 30 characters, open a detail view, return, and delete with confirmation. Lists persist as individual CSVs in `%LocalAppData%\Chucao\Lists`, with only the `Concept,SeenOrder` columns. Internal filenames encode display names as UTF-8 hexadecimal, allowing names containing Windows-reserved characters without an additional catalog. Duplicate names are rejected case-insensitively. Invalid files are reported and left unchanged. New lists contain only the header; concept editing is still pending.

The application uses C#, .NET 10, WPF, and CsvHelper for CSV storage.

Development requires Windows and the .NET 10 SDK. From the repository root:

```powershell
dotnet build .\src\Chucao\Chucao.csproj
dotnet run --project .\src\Chucao\Chucao.csproj
```

A self-contained Windows release is planned. User data will remain outside the executable.
