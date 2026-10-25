# P2 — Pipeline de comandos asíncrono

**Problema del TP1:** ¿cómo alternar PA (mover → atacar → mover) validando en C# e invocando
animaciones en Unity de forma asíncrona?
**Solución del TP1 implementada:** pipeline de `ICommand` inmutables. Cada comando valida PA,
ejecuta la lógica de forma **instantánea** en el Core y deja que la presentación anime **después**.

## Qué demuestra

1. **Desacoplamiento temporal:** al volver de `Validate`/`Execute`, el estado ya cambió; la
   animación sigue en curso. La regla no espera frames.
2. **Cola asíncrona:** dos comandos enviados seguidos no se intercalan; el segundo espera a que
   termine la animación del primero.
3. **OCP:** agregar `ShieldCommand` (o un `TractorBeamCommand`) no toca el pipeline ni las fases.
4. **Rechazo con motivo:** `CommandValidation` transporta el motivo y los PA requeridos/disponibles.
5. **Sin FSM monolítica:** solo tres fases (`PlayerTurn`, `Executing`, `EnemyTurn`), no un estado
   por combinación de acciones.

## Cómo se ejecuta

Escena `Scenes/P2_CommandPipeline.unity` → Play. `TurnDemoDriver` ejecuta mover → atacar → mover
y `ShipView` completa cada animación desde `Update` (nunca desde el dominio).

## Dónde están los tests

- `Tests/EditMode/CommandPipelineTests.cs`: validación, rechazo sin efectos, lógica antes de la
  animación, cola serializada, PA exactos, recarga de turno, fase enemiga.
- `Tests/PlayMode/TurnDemoDriverPlayModeTests.cs`: integración con el ciclo de vida de Unity.

## Decisión respecto del TP1

El PDF declara **UniTask / Task**. Se usa `Task` (BCL) para que la suite sea reproducible sin
dependencias externas; el punto de sustitución es una sola interfaz, `ICommandPresentation`, más
el adaptador `ShipView`. Cambiar a UniTask no toca el dominio ni los tests.
