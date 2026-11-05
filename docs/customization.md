# Personalización de Chucao

Las rutas siguientes parten de `src/Chucao`, salvo las carpetas `Assets` y `tools`, que están en la raíz del repositorio. Después de modificar recursos o código, se recompila la aplicación.

## Estilo y dimensiones

| Qué ajustar | Dónde |
|---|---|
| Colores de texto, fondos, botones y hover | `Resources/Palette.xaml`: recursos `...Color` y sus pinceles `...Brush`. Se utiliza `#RRGGBB` o `#AARRGGBB` con transparencia. |
| Esquinas y sombras de botones | `Palette.xaml`: `ButtonCornerRadius`, `ButtonShadowOffsetX` y `ButtonShadowOffsetY`. La sombra es un desplazamiento sólido. |
| Espacio entre botones | `Palette.xaml`: `ListCardMargin`, `LeftActionMargin`, `RightActionMargin` y `DialogActionMargin`; los ajustes locales están en cada vista. |
| Fuentes, pesos, tamaños y colores por grupo de texto | `Resources/Typography.xaml`: `AppFontFamily`, `RegularFontWeight`, `EmphasisFontWeight`, claves `...FontSize` y pinceles de texto. Los archivos de fuente se enlazan en `Chucao.csproj`. |
| Plantillas de botones, scrollbar, selector de minutos y slider | `Resources/Controls.xaml`. Las variantes de botones heredan de `AppButtonStyle`; el volumen usa `VolumeSliderStyle`. |
| Tamaño de la ventana | `Views/MainWindow.xaml`: `Width`, `Height` y `ResizeMode`. |
| Imagen de portada | `Views/ListsView.xaml`: `HomeBirdImage`, propiedades `Width`, `Height`, `Stretch` y `Margin`. |
| Tarjetas de listas, botón «+» y temporizador | `ListsView.xaml`: botón del `DataTemplate`, `AddListButton` y `TimerButton`. Se ajustan `Width`, `MinHeight`, `Padding` y `Margin`. |
| Cifras del reloj | `Typography.xaml`: `TimerDigitsFontSize`; `Palette.xaml`: `TimerDigitCellWidth` y `TimerSeparatorCellWidth`. Si se aumenta la fuente, se comprueba que los dígitos quepan en sus celdas. |

Las medidas son unidades de WPF. `Margin` y `Padding` siguen el orden **izquierda, arriba, derecha, abajo**. El marco y la barra de título nativos usan `WindowFrameColor` y `WindowCaptionColor` mediante `Views/NativeWindowColors.cs`, en Windows compatible.

## Imágenes, icono, sonido e idiomas

- `Assets/Images/stand.png`: portada y ave detenida; `fly1.png` y `fly2.png`: aleteo; `tronco.png`: troncos.
- `Assets/Images/icono.png`: imagen fuente del icono. Cuando cambia, se regenera `icono.ico` con el siguiente comando:

```powershell
.\tools\Convert-PngToIcon.ps1 -InputPath .\Assets\Images\icono.png -OutputPath .\Assets\Images\icono.ico -Force
```

`Chucao.csproj` enlaza los archivos como recursos y configura `ApplicationIcon`. Si se cambian nombres o rutas, se ajustan sus elementos `Resource` o las propiedades `ApplicationImageFile`, `ApplicationIconFile`, `BirdFly1ImageFile` y `BirdFly2ImageFile`. `App.xaml.cs` carga las imágenes mediante alias internos.

El sonido fuente se configura con `TimerCompletionSoundFile` en `Chucao.csproj`; se copia como `Assets/Sounds/TimerFinished.wav` junto al ejecutable. Volumen y activación se cambian desde Configuraciones. Al sustituir recursos, se actualizan sus créditos y licencias.

Los textos de interfaz están en `Resources/Translations.csv`, con columnas `Key,es,en`. Se conservan las claves utilizadas por el código y se completan ambos idiomas. La fuente del engranaje se configura aparte en `SettingsIconTextStyle`.

## Escena y animación del temporizador

En `Palette.xaml`:

| Recurso | Función |
|---|---|
| `AnimationCanvasWidth`, `AnimationCanvasHeight`, `AnimationCornerRadius` | Dimensiones y esquinas del Canvas. El `Viewbox` puede reducirlo proporcionalmente si falta espacio. |
| `AnimationColor`, `AnimationGroundColor`, `AnimationPathColor` | Colores del cielo, terreno y camino. |
| `BirdBaseHeight` | Altura común del ave al 100 %. El ancho conserva la proporción de cada imagen. |
| `BirdInitialScale`, `BirdFinalScale` | Multiplicadores inicial y final: `0.5` equivale a 50 %; `1` a 100 %. El crecimiento ocurre solo durante los vuelos. |

En `Views/TimerView.xaml.cs`:

- `animationWaypoints`: puntos `new(x, y)` entre 0 y 1, en orden de recorrido. El origen está arriba a la izquierda; X crece a la derecha e Y hacia abajo.
- `stumpPlacements`: filas `(X, Y, Width, Flip)`. X es el centro horizontal relativo, Y el borde superior relativo y Width la fracción del ancho del Canvas. `Flip = true` refleja la imagen. Se ajustan independientemente del recorrido.
- `FinalFlightSeconds`: duración del último vuelo; `EarlierFlightIncrementSeconds`: incremento por cada vuelo anterior. El tiempo restante se reparte entre las esperas. Si los vuelos exceden la duración total, se usan vuelos de igual duración sin esperas.
- `FlightFrameSeconds`: segundos por imagen de aleteo; un valor positivo menor acelera la alternancia.

El horizonte y la forma del camino están en el Canvas de referencia de `Views/TimerView.xaml`: `Canvas.Top` del `Rectangle` y `Points` del `Polygon`. La animación comparte el reloj del temporizador; pausa, continuación y reinicio se coordinan con `TimerViewModel`.

[README del proyecto](../README.md).
