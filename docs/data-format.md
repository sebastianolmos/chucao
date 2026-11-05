# Datos locales de Chucao

La aplicación guarda sus datos fuera del ejecutable, en `%LocalAppData%\Chucao`:

```text
Chucao/
├── settings.csv
└── Lists/
    └── list-<nombre codificado>.csv
```

Los archivos son CSV en UTF-8, separados por comas y leídos/escritos con CsvHelper. Los campos con comas, comillas o saltos de línea se entrecomillan según CSV.

## Listas y progreso

Cada lista tiene un archivo con exactamente estas columnas y en este orden:

```csv
Concept,SeenOrder
Variable,1
Función,2
Recursividad,0
```

- `Concept`: texto del concepto. Las altas desde la interfaz admiten de 1 a 30 caracteres y rechazan duplicados exactos, distinguiendo mayúsculas e incluyendo los ya vistos.
- `SeenOrder = 0`: disponible para selección.
- `SeenOrder > 0`: orden de aparición, sin valores positivos repetidos en una misma lista. El mayor valor identifica el último concepto visto; los números no necesitan ser consecutivos.

Seleccionar al azar o marcar como visto asigna `max(SeenOrder) + 1` y guarda antes de actualizar el resultado en pantalla. Reiniciar pone todos los estados en 0; no recupera conceptos eliminados. La ronda no se reinicia automáticamente. Una lista vacía conserva solo la cabecera.

El nombre visible se codifica como bytes UTF-8 en hexadecimal dentro del nombre del archivo: por ejemplo, «Redes» corresponde a `list-5265646573.csv`. Esto permite nombres con caracteres como `/` o `?`, sin identificadores ni metadatos adicionales. Los nombres de listas tienen un máximo de 30 caracteres y no pueden repetirse, sin distinguir mayúsculas. Estos archivos no deben renombrarse manualmente.

«Agregar conjunto» acepta texto pegado con un guion ASCII `-` al inicio de cada línea. Quita el primer guion y los espacios exteriores; omite líneas inválidas, vacías, conceptos de más de 30 caracteres y duplicados exactos. Guarda los aceptados con estado 0 en una sola operación. No es una importación directa de CSV.

## Preferencias

`settings.csv` utiliza las columnas `Key,Value`:

| Clave | Valores | Predeterminado |
|---|---|---|
| `Language` | `en` o `es` | `en` |
| `TimerMinutes` | Entero entre 1 y 59 | `1` |
| `TimerSoundVolume` | Número entre 0 y 1, con punto decimal | `0.6` |
| `TimerSoundEnabled` | `true` o `false` | `true` |

Si no existe el archivo se usan los valores predeterminados. Los archivos antiguos sin claves del temporizador también usan sus valores predeterminados hasta que el usuario los guarde. En archivos existentes, `Language` debe estar presente y ser válido; no se admiten claves repetidas.

El cambio de idioma se guarda al alternarlo. En Configuraciones, «Volver» guarda duración, volumen y activación juntos; Escape descarta la edición. Guardar una preferencia conserva las demás claves. No se persisten el tiempo restante de una cuenta en curso ni el estado de la animación.

## Respaldo y errores

Con la aplicación cerrada, se copia toda la carpeta `%LocalAppData%\Chucao`. Para restaurar un respaldo, se cierra Chucao y se repone esa carpeta. Actualizar o mover los archivos de la aplicación no cambia esta ubicación.

Los guardados se preparan en archivos temporales antes de sustituir el CSV de destino. Las listas no reconocidas, ilegibles o inválidas se omiten con un aviso y se conservan sin cambios. Las preferencias inválidas también se conservan y se informa del problema. Eliminar una lista desde la interfaz borra su CSV.

La lógica de archivos está en `Services/ListFileService.cs` y `Services/SettingsService.cs`, dentro de `src/Chucao`; el sorteo está en `Services/ConceptSelectionService.cs`.

[README del proyecto](../README.md).
