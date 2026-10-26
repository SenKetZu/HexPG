# HexPG — Void Hex Tactics

**TP2 — Programación de VideoJuegos III (2026)** · Implementación y tests en Unity.

| | |
| --- | --- |
| Integrantes | 1 |
| Docente | Diego Freijo |
| Unity | **6000.5.9f1** (Test Framework 1.7.0, VContainer 1.19.0) |
| Entrega | tag `tp2` — 28/10 23:59 |
| Reentrega | tag `rtp2` — 04/11 23:59 |
| Defensa oral | 04/11 (20 min + 10 de preguntas) |

## Qué es esto

Tres pruebas de concepto que implementan los **tres desafíos arquitectónicos declarados en el TP1**.
No es un juego: es una demostración acotada de arquitectura, contratos y tests.

| # | Prototipo | Problema declarado en el TP1 | Solución implementada |
| --- | --- | --- | --- |
| P1 | `P1_GridState` | Exponer el mapa a IA, comandos y UI sin God Object ni polling en `Update()` | **ISP + Observer**: `IGridQueryService` (lectura) e `IGridStatePublisher` (eventos) |
| P2 | `P2_CommandPipeline` | Alternar PA validando en C# e invocando animaciones async | **Pipeline de `ICommand` + ejecución asíncrona** con cola serializada |
| P3 | `P3_Loadout` | Conectar servicios sin singletons y definir módulos MK I–III | **VContainer DI + ScriptableObjects (Flyweight)** |

Diseño de referencia (TP1): [`GDD.md`](./GDD.md) · [`DIAGRAMS.md`](./DIAGRAMS.md).

## Estado

Se actualiza el mismo día que cambia el código.

| Prototipo | Estructura | Dominio | Escena | Tests Edit | Tests Play |
| --- | --- | --- | --- | --- | --- |
| P1 GridState | ☑ | ☑ | ☐ | ☑ (5) | — |
| P2 CommandPipeline | ☑ | ☑ | ☐ | ☑ (7) | ☑ (1) |
| P3 Loadout | ☑ | ☑ | ☐ | ☑ (9) | — |

> Las escenas se generan con `VoidHex → Build Prototype Scenes` (menú del editor).

## Estructura

```text
Assets/Prototypes/
  P1_GridState/{Runtime/Domain, Runtime/Unity, Tests/EditMode, Scenes, README.md}
  P2_CommandPipeline/{Runtime/Domain, Runtime/Application, Runtime/Unity,
                      Tests/EditMode, Tests/PlayMode, Scenes, README.md}
  P3_Loadout/{Runtime/Domain, Runtime/Application, Runtime/Unity,
              Tests/EditMode, Scenes, Assets, README.md}
  Editor/  PrototypeSceneBuilder.cs    genera escenas y assets de P3
           PrototypeSceneBootstrap.cs  las genera solo si faltan
Assets/Shared/UiKit/   UI nativa + carga de modelos 3D (solo presentación)
Assets/ThirdParty/Kenney/SpaceKit/   modelos CC0 usados por las escenas
  docs/    doctrina, arquitectura, tests, prototipos, trazabilidad, defensa, plan, presentación
  tools/   ppt/ (deck) · audit/ (lógica sin Unity) · verify/ (referencias, escenas, tests) · assets/
```

Cada prototipo es independiente: no comparte escena, estado estático ni carpeta con otro.
La separación de capas se impone con asmdefs: los assemblies de dominio declaran
`"noEngineReferences": true`, así que un `using UnityEngine;` no compila.

## Cómo abrir

1. Instalar Unity **6000.5.9f1** (Unity Hub).
2. Unity Hub → *Add* → *Add project from disk* → seleccionar la raíz de este repositorio.
3. Esperar la importación. El proyecto usa el registro **OpenUPM** para VContainer
   (`Packages/manifest.json`); requiere conexión la primera vez.

## Cómo ejecutar cada prototipo

Las escenas se generan **solas** la primera vez que Unity compila el proyecto: si falta alguna,
`PrototypeSceneBootstrap` las crea (`[InitializeOnLoad]`, una vez por sesión, sin pisar cambios).
Para forzar la regeneración: menú **`VoidHex → Build Prototype Scenes`**.
Para abrirlas: **`VoidHex → Open P1/P2/P3 … Scene`**.

| Prototipo | Escena | Qué se ve y qué se puede hacer |
| --- | --- | --- |
| P1 | `P1_GridState.unity` | Mapa 12×12 con celdas reales coloreadas (asteroides, nebulosas, ocupación). El HUD permite bloquear/liberar celdas, ocupar y **reescribir una celda con el mismo valor**: el contador de eventos no debe subir. Muestra la ruta y su costo |
| P2 | `P2_CommandPipeline.unity` | Nave y enemigo animados por `ShipView`. El HUD envía comandos intercalados (mover/atacar/escudo), fuerza un rechazo, termina el turno y pasa a turno enemigo, mostrando PA y fase en vivo |
| P3 | `P3_Loadout.unity` | HUD con los 4 slots, estadísticas recalculadas y botones para equipar cada módulo del catálogo (incluye MK III) y quitar por slot |

Abrir la escena y presionar **Play**. Cada prototipo se ejecuta solo; los paneles son uGUI nativa.

## Assets 3D

Los modelos son del **Kenney Space Kit** (CC0, dominio público): naves, asteroide, cristales y
turret. Se cargan por nombre en runtime (`Resources/Models/<Nombre>.fbx`) desde
`Assets/ThirdParty/Kenney/SpaceKit`, con atribución y licencia incluidas en esa carpeta.

Los materiales **no** vienen del kit: los genera `VoidHex.UiKit.ModelLibrary` con el shader URP y
la paleta de la UI, para que escena y paneles compartan el mismo lenguaje visual. Si un modelo
faltara, la vista cae a una primitiva: la demo no se rompe por un asset ausente.

Para reinstalar el subconjunto de modelos (requiere el zip descomprimido en `_downloads/`):

```powershell
python tools\assets\install_kenney_spacekit.py
```

## Cómo correr los tests

### Desde el editor (recomendado)

`Window → General → Test Runner`:

1. **EditMode → Run All** (dominio, aplicación y adaptadores de los tres prototipos).
2. **PlayMode → Run All** (integración de P2 con el ciclo de vida de Unity).

### Desde línea de comandos

```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\6000.5.9f1\Editor\Unity.exe"

& $unity -batchmode -nographics -projectPath . -runTests `
  -testPlatform EditMode -testResults Logs/editmode-results.xml -quit

& $unity -batchmode -nographics -projectPath . -runTests `
  -testPlatform PlayMode -testResults Logs/playmode-results.xml -quit
```

Los resultados quedan en `Logs/*.xml` (ignorados por git). Código de salida `0` = suite verde.

> Requiere el editor **cerrado**: Unity toma un lock sobre el proyecto.

## Verificación rápida sin abrir Unity

Cuatro chequeos que no necesitan el editor y atajan los errores de configuración más comunes:

```powershell
python tools\verify\check_asmdef_refs.py     # usings vs referencias declaradas en cada asmdef
python tools\verify\check_layers.py          # MonoBehaviours, dominio puro y destrucción de objetos
dotnet run --project tools\audit\Audit.csproj # ejecuta la lógica pura y compara con las aserciones
dotnet build tools\verify\VerifyUnity.csproj  # compila las capas Unity contra los DLLs reales
```

`check_asmdef_refs.py` detecta `CS0234`/`CS0012` (falta de referencias) antes de recompilar.
`check_layers.py` hace cumplir las separaciones que el proyecto declara:

| Regla | Qué impide |
| --- | --- |
| R1 | Un `MonoBehaviour` fuera de `Unity/`, `Editor/` o tests: la regla deja de depender del motor |
| R2 | Tipos de Unity en un assembly con `noEngineReferences: true` |
| R3 | `Object.Destroy` directo: en el editor no destruye y tira "Destroy may not be called from edit mode" |

`Audit` detecta aserciones de test que no coinciden con el comportamiento real.

## Documentación

| Documento | Contenido |
| --- | --- |
| [`docs/00-doctrina.md`](./docs/00-doctrina.md) | Los tres desafíos declarados, pilares, reglas no negociables, anti-objetivos |
| [`docs/01-arquitectura.md`](./docs/01-arquitectura.md) | Capas, asmdefs, estructura, dependencias y **ajustes respecto del TP1** |
| [`docs/02-doctrina-de-tests.md`](./docs/02-doctrina-de-tests.md) | Qué testear, Edit vs Play Mode, reglas y trampas |
| [`docs/03-prototipos.md`](./docs/03-prototipos.md) | Por prototipo: alcance, comportamientos, exclusiones, contratos y propiedad demostrada |
| [`docs/04-trazabilidad.md`](./docs/04-trazabilidad.md) | Matriz comportamiento → test → defecto que detecta, y cumplimiento de la consigna |
| [`docs/05-defensa.md`](./docs/05-defensa.md) | Guion de 20 minutos, demos y banco de preguntas |
| [`docs/06-plan.md`](./docs/06-plan.md) | Plan hasta la entrega, riesgos y procedimiento del tag |
| [`docs/07-presentacion.md`](./docs/07-presentacion.md) | Contenido slide por slide, listo para volcar a PowerPoint |
| [`docs/08-assessment-materiales.md`](./docs/08-assessment-materiales.md) | **Diagnóstico medido** de por qué los modelos del kit no se ven bien, y qué queda abierto |
| [`docs/presentacion/`](./docs/presentacion/) | **Deck `.pptx` de 21 slides** + diagramas PNG + cómo importarlo a Google Slides |

## Alcance

Lo que **no** incluye, y por qué, está en [`docs/03-prototipos.md`](./docs/03-prototipos.md):
arte, IA enemiga, undo/redo, efectos de terreno, guardado, mapas 3D y modificadores multiplicativos.
Cada exclusión tiene justificación y una ruta de extensión.
