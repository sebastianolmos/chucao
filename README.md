# Chucao: Práctica de conceptos

Aplicación de escritorio para Windows que permite practicar la explicación oral de conceptos mediante selección aleatoria, sin repetirlos hasta completar una ronda.


## Estado

Proyecto en etapa inicial. Las siguientes funciones se implementarán progresivamente.

## Funciones previstas

- Crear, editar y administrar listas de conceptos.
- Agregar y editar conceptos.
- Importar listas desde archivos TXT, con un concepto por línea.
- Seleccionar conceptos aleatoriamente sin repetición dentro de cada ronda.
- Guardar las listas y el progreso en CSV entre ejecuciones.
- Personalizar la apariencia de la aplicación.
- Cambiar el idioma de la interfaz entre español e inglés desde el menú o la vista principal, sin reiniciar ni perder el progreso.
- Incorporar posteriormente un temporizador para la práctica oral.

## Tecnologías

- C# y .NET 10.
- WPF para la interfaz gráfica.
- CSV para persistencia, con CsvHelper como biblioteca prevista.

## Desarrollo

Requisitos:

- Windows.
- SDK de .NET 10.
- VS Code con C# Dev Kit o una versión compatible de Visual Studio con desarrollo de escritorio de .NET.

Una vez creado el proyecto en `src/Chucao`, ejecutar desde la raíz del repositorio:

```powershell
dotnet build .\src\Chucao\Chucao.csproj
dotnet run --project .\src\Chucao\Chucao.csproj
```

## Distribución

Se prevé publicar una versión autocontenida para Windows que incluya el runtime de .NET y pueda abrirse mediante doble clic.

Los archivos de datos se conservarán fuera del ejecutable para mantener las listas y el progreso al actualizar la aplicación.

---

## Summary

**Chucao: Concept Practice** is a Windows desktop application for practicing oral explanations of randomly selected concepts, without repetition until a round is complete.

The project is at an early stage. Planned features include:

- Create and manage concept lists.
- Import TXT files with one concept per line.
- Select random concepts without repetition within each round.
- Save lists and progress in CSV files between sessions.
- Customize the interface.
- Switch between Spanish and English from the main menu or main view without restarting the application or losing progress.
- Add a practice timer in a later version.

The application uses C#, .NET 10, and WPF, with CsvHelper planned for CSV storage.

Development requires Windows and the .NET 10 SDK. From the repository root:

```powershell
dotnet build .\src\Chucao\Chucao.csproj
dotnet run --project .\src\Chucao\Chucao.csproj
```

A self-contained Windows release is planned. User data will remain outside the executable.
