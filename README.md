# HexPG — Void Hex Tactics

**TP2 — Programación de Videojuegos III (2026)** · Implementación y tests en Unity.

| | |
| --- | --- |
| Integrantes | 1 |
| Docente | Diego Freijo |
| Unity | **6000.5.9f1** (URP 17.6, Test Framework 1.7.0) |
| Entrega | tag `tp2` — 28/10 23:59 |
| Reentrega | tag `rtp2` — 04/11 23:59 |
| Defensa oral | 04/11 (20 min + 10 de preguntas) |

## Qué es esto

Tres **pruebas de concepto** que implementan los problemas técnicos del TP1. No es un juego:
es una demostración acotada de arquitectura, contratos y tests.

| # | Prototipo | Problema del TP1 | Solución implementada |
| --- | --- | --- | --- |
| P1 | `P1_HexGrid` | Grilla hexagonal y pathfinding con costo variable | Coordenadas cúbicas en C# puro + A* determinista |
| P2 | `P2_TurnFlow` | Turnos, economía de PA y comandos intercalados | FSM + patrón comando con estado inmutable y undo por snapshot |
| P3 | `P3_Loadout` | Slots, módulos MK I/II/III y estadísticas | Datos + composición; `ScriptableObject` como adaptador |

Diseño de referencia (TP1): [`GDD.md`](./GDD.md) · [`DIAGRAMS.md`](./DIAGRAMS.md).

## Estado

Se actualiza el mismo día que cambia el código.

| Prototipo | Dominio | Escena | Tests Edit | Tests Play | Doc |
| --- | --- | --- | --- | --- | --- |
| P1 HexGrid | ☐ | ☐ | ☐ | — | ☑ |
| P2 TurnFlow | ☐ | ☐ | ☐ | ☐ | ☑ |
| P3 Loadout | ☐ | ☐ | ☐ | — | ☑ |

## Estructura

```text
Assets/Prototypes/
  P1_HexGrid/{Runtime/Domain, Runtime/Unity, Tests/EditMode, Scenes, README.md}
  P2_TurnFlow/{Runtime/Domain, Runtime/Unity, Tests/EditMode, Tests/PlayMode, Scenes, README.md}
  P3_Loadout/{Runtime/Domain, Runtime/Unity, Tests/EditMode, Scenes, README.md}
docs/   doctrina, arquitectura, tests, prototipos, trazabilidad, defensa, plan
```

Cada prototipo es independiente: no comparte escena, estado estático ni carpeta con otro.
La separación de capas se impone con asmdefs (`VoidHex.Pn.Domain` con `noEngineReferences: true`).

## Cómo abrir

1. Instalar Unity **6000.5.9f1** (Unity Hub).
2. Unity Hub → *Add* → *Add project from disk* → seleccionar la raíz de este repositorio.
3. Abrir el proyecto y esperar la importación de paquetes (`com.unity.test-framework` ya está en `Packages/manifest.json`).

## Cómo ejecutar cada prototipo

| Prototipo | Escena | Qué se ve |
| --- | --- | --- |
| P1 | `Assets/Prototypes/P1_HexGrid/Scenes/P1_HexGrid.unity` | Mapa 12×12, origen/destino por clic, ruta óptima o motivo de rechazo |
| P2 | `Assets/Prototypes/P2_TurnFlow/Scenes/P2_TurnFlow.unity` | Barra de PA, comandos Mover/Atacar/Escudo, Deshacer y log de eventos |
| P3 | `Assets/Prototypes/P3_Loadout/Scenes/P3_Loadout.unity` | 4 slots, módulos MK I/II/III y panel de estadísticas recalculadas |

Abrir la escena y presionar **Play**. Cada prototipo se ejecuta solo; no requiere los otros.

## Cómo correr los tests

### Desde el editor (recomendado)

`Window → General → Test Runner`:

1. **EditMode** → `Run All` (dominio de P1, P2, P3 y adaptador de assets).
2. **PlayMode** → `Run All` (integración de P2 con el ciclo de vida de Unity).

### Desde línea de comandos

Con el editor cerrado:

```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\6000.5.9f1\Editor\Unity.exe"

& $unity -batchmode -nographics -projectPath . -runTests `
  -testPlatform EditMode -testResults Logs/editmode-results.xml -quit

& $unity -batchmode -nographics -projectPath . -runTests `
  -testPlatform PlayMode -testResults Logs/playmode-results.xml -quit
```

Los resultados quedan en `Logs/*.xml`. Código de salida `0` = suite verde.

**Requisitos de reproducibilidad:** no hay pasos manuales, ni dependencia de orden, ni assets
creados a mano. Cada test prepara y limpia lo suyo.

## Documentación

| Documento | Contenido |
| --- | --- |
| [`docs/00-doctrina.md`](./docs/00-doctrina.md) | Pilares, reglas no negociables, anti-objetivos, definición de terminado |
| [`docs/01-arquitectura.md`](./docs/01-arquitectura.md) | Capas, asmdefs, estructura, contratos y **ajustes respecto del TP1** |
| [`docs/02-doctrina-de-tests.md`](./docs/02-doctrina-de-tests.md) | Qué testear, Edit vs Play Mode, reglas y trampas |
| [`docs/03-prototipos.md`](./docs/03-prototipos.md) | Por prototipo: alcance, comportamientos, supuestos, exclusiones, contratos y propiedad demostrada |
| [`docs/04-trazabilidad.md`](./docs/04-trazabilidad.md) | Matriz comportamiento → test → defecto que detecta → cumplimiento de la consigna |
| [`docs/05-defensa.md`](./docs/05-defensa.md) | Guion de 20 minutos, demos, banco de preguntas |
| [`docs/06-plan.md`](./docs/06-plan.md) | Plan hasta la entrega, riesgos y procedimiento del tag |

## Alcance

Lo que **no** incluye este TP, y por qué, está en
[`docs/03-prototipos.md`](./docs/03-prototipos.md): arte, IA enemiga, animaciones, efectos de
terreno, guardado, optimización para mapas grandes y modificadores multiplicativos. Cada exclusión
tiene justificación y una ruta de extensión.
