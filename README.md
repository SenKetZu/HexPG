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
  Editor/PrototypeSceneBuilder.cs      genera escenas y assets de P3
docs/   doctrina, arquitectura, tests, prototipos, trazabilidad, defensa, plan
```

Cada prototipo es independiente: no comparte escena, estado estático ni carpeta con otro.
La separación de capas se impone con asmdefs: los assemblies de dominio declaran
`"noEngineReferences": true`, así que un `using UnityEngine;` no compila.

## Cómo abrir

1. Instalar Unity **6000.5.9f1** (Unity Hub).
2. Unity Hub → *Add* → *Add project from disk* → seleccionar la raíz de este repositorio.
3. Esperar la importación. El proyecto usa el registro **OpenUPM** para VContainer
   (`Packages/manifest.json`); requiere conexión la primera vez.

## Cómo generar y ejecutar cada prototipo

Una sola vez: menú **`VoidHex → Build Prototype Scenes`**. Crea las tres escenas y los assets de P3.

| Prototipo | Escena | Qué se ve |
| --- | --- | --- |
| P1 | `Assets/Prototypes/P1_GridState/Scenes/P1_GridState.unity` | Mapa 12×12 con celdas bloqueadas, ruta óptima dibujada con gizmos y un log por **mutación real** |
| P2 | `Assets/Prototypes/P2_CommandPipeline/Scenes/P2_CommandPipeline.unity` | Secuencia mover → atacar → mover; cada animación se completa desde `Update` |
| P3 | `Assets/Prototypes/P3_Loadout/Scenes/P3_Loadout.unity` | `LifetimeScope` inyecta el servicio; se equipan MK I / MK II y se registran las stats |

Abrir la escena y presionar **Play**. Cada prototipo se ejecuta solo.

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

Los resultados quedan en `Logs/*.xml`. Código de salida `0` = suite verde.

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

## Alcance

Lo que **no** incluye, y por qué, está en [`docs/03-prototipos.md`](./docs/03-prototipos.md):
arte, IA enemiga, undo/redo, efectos de terreno, guardado, mapas 3D y modificadores multiplicativos.
Cada exclusión tiene justificación y una ruta de extensión.
