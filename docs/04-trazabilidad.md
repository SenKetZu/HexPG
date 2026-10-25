# 04 — Trazabilidad y cumplimiento de la consigna

## 1. Matriz comportamiento → test → defecto → evidencia

> Nombres de test definitivos al implementar; esta matriz es el contrato de la suite.
> La columna "defecto que detecta" responde a §4.2 de la consigna.

### P1 — HexGrid

| ID | Test | Modo | Defecto que detectaría | Evidencia en defensa |
| --- | --- | --- | --- | --- |
| B1.1 | `Distance_IsSymmetricAndZeroForSameCell` | Edit | Fórmula mal aplicada (usar `r` en lugar de `s`, o no dividir por 2) | Test verde + fórmula en slide |
| B1.2 | `Neighbors_ReturnsSixCells_AndIsSymmetric` | Edit | Vecinos duplicados o fuera de mapa en el borde | Test verde |
| B1.3 | `FindPath_WithVariableCost_ReturnsMinimumCostPath` | Edit | Algoritmo voraz que elige la ruta más corta en pasos, no la más barata | Test con dos rutas de igual longitud y distinto costo |
| B1.4 | `FindPath_WhenBudgetExceeded_ReturnsUnreachable` | Edit | Devolver ruta parcial y dejar al jugador en una celda inválida | Test verde |
| B1.5 | `FindPath_WhenGoalBlockedOrMissing_ReturnsExplicitFailure` | Edit | `null` silencioso o excepción por caso de juego esperable | Test de los 3 motivos |
| B1.6 | `FindPath_SameInput_ProducesSamePathTwice` | Edit | Desempate por orden de `Dictionary`/heap inestable | Dos corridas comparadas |
| B1.7 | `HasLineOfSight_BlockerInMiddle_ReturnsFalse_EndpointsDoNotBlock` | Edit | Bloquear por la celda del atacante u objetivo (siempre falsos negativos) | Test con 3 escenarios |
| B1.8 | `FindPath_DoesNotMutateGrid` | Edit | A* que escribe marcas de visitado en el mapa real | Comparar mapa antes/después |

### P2 — TurnFlow

| ID | Test | Modo | Defecto que detectaría | Evidencia en defensa |
| --- | --- | --- | --- | --- |
| B2.1 | `StartTurn_RefillsActionPoints_ToMaximum` | Edit | Acumular PA entre turnos (economía rota) | Test verde |
| B2.2 | `Submit_InterleavedCommands_SpendsExactActionPoints` | Edit | Cobrar de menos o aplicar costo dos veces | Test con 3 comandos |
| B2.3 | `Submit_WhenInsufficientActionPoints_RejectsWithReasonAndKeepsState` | Edit | Consumir PA parcial antes de rechazar | Estado comparado completo |
| B2.4 | `Submit_AppliesEffectExactlyOnce` | Edit | Doble ejecución por evento/reintento | Hull/posición comparados |
| B2.5 | `UndoLast_RestoresPreviousSnapshot_AndSecondUndoIsRejected` | Edit | Undo aritmético con deriva, o undo infinito | Snapshot completo |
| B2.6 | `TurnStateMachine_InvalidTransition_IsRejectedAndKeepsState` | Edit | FSM que acepta cualquier transición | Tabla de transiciones |
| B2.7 | `Submit_OutsidePlayerTurn_RejectsWithWrongPhase` | Edit | Ejecutar comandos en turno enemigo | Test verde |
| B2.8 | `Driver_SubmitsCommandOnStart_RaisesEventOnlyWhenAccepted` | **Play** | Evento emitido por rechazo (UI muestra gasto inexistente); evento no emitido por no invocar el driver | Test con `yield return null` |

> Los tests de P2 referencian `VoidHex.P2.Domain` y `VoidHex.P2.Application`. El asmdef de tests
> debe declarar ambas referencias; si falta `Application`, la suite no compila (se detecta en el día 1).

### P3 — Loadout

| ID | Test | Modo | Defecto que detectaría | Evidencia en defensa |
| --- | --- | --- | --- | --- |
| B3.1 | `TryEquip_WhenSlotTypeMismatch_ReturnsWrongSlotTypeAndKeepsLoadout` | Edit | Aceptar cualquier módulo en cualquier slot | Test verde |
| B3.2 | `Calculate_AddsAllBonuses_ToBaseStats` | Edit | Olvidar un modificador o pisar el valor base | Test con 2 módulos |
| B3.3 | `TryEquip_ReplacingModule_DoesNotDoubleCount` | Edit | Sumar el módulo viejo y el nuevo | MK I → MK II |
| B3.4 | `Unequip_RemovesExactBonuses` | Edit | Dejar residuo del módulo anterior | Estado igual al inicial |
| B3.5 | `Calculate_ConsolidatesAbilities_WithoutDuplicates` | Edit | Duplicar habilidades compartidas (Flyweight mal entendido) | Lista comparada |
| B3.6 | `Calculate_IsIdempotent_AndDoesNotMutateInputs` | Edit | Mutar el SO/definición y acumular bonificaciones en cada llamada | Dos cálculos comparados |
| B3.7 | `ShipModuleAsset_MapsToDefinition_AllFields` | Edit (integración) | Campo sin mapear (p. ej. `Tier`) que rompe el reemplazo | Asset creado en el test |
| B3.8 | `Calculate_ModuleWithoutAbility_AddsNothing` | Edit | Agregar `null`/`""` a la lista de habilidades | Lista vacía |

## 2. Matriz de cumplimiento de la consigna

| Requisito | Dónde se cumple | Evidencia |
| --- | --- | --- |
| §2: TP1 incluido como referencia | Raíz del repo | `GDD.md`, `DIAGRAMS.md` versionados |
| §2: prototipos + tests + instrucciones | `README.md`, `Assets/Prototypes/**` | Suite verde + pasos reproducibles |
| §2: documentación de alcance y exclusiones | `03-prototipos.md` | Tablas por prototipo |
| §2: tag de entrega | `06-plan.md` §4 | `git tag tp2` empujado |
| §3.1: 3 prototipos independientes, una escena cada uno | `Assets/Prototypes/Pn_*` | Estructura de carpetas |
| §3.1: comportamiento implementado y funcionando | Escenas de demo | Recorrido en vivo |
| §3.1: tests relevantes que pasan | `docs/02`, matriz §1 | Test Runner |
| §3.1: explicar implementación y decisiones | `docs/03`, `docs/05` | Defensa |
| §3.2: defender con código y tests | `05-defensa.md` | Demo + preguntas |
| §4.1: problema/solución del TP1 explícitos | `03-prototipos.md` (encabezado de cada uno) | Slide 1 de cada bloque |
| §4.1: supuestos y exclusiones justificadas | `03-prototipos.md` | Tablas de exclusión |
| §4.1: ajustes respecto del TP1 | `01-arquitectura.md` §5, `03` §final | Lista de 6 ajustes |
| §4.2: qué verifica cada test y qué error detecta | Matriz §1 | Columna "defecto" |
| §4.2: reproducibilidad, sin orden ni pasos manuales | `02-doctrina-de-tests.md` §3 | Suite desde cero |
| §4.2: Edit/Play Mode donde corresponde | `02` §2 | Solo P2 en Play Mode |
| §4.3: demostrar propiedades del diseño | `03` (tabla por prototipo) | Demo ensayada |
| §4.4: contratos, `null`, mutabilidad, excepciones | `01` §6, `03` (contratos) | Preguntas de defensa |
| §4.5: README con cómo ejecutar y correr tests | `README.md` | Verificación en vivo |

## 3. Checklist de mínimos (§3.1)

- [ ] P1: B1.1–B1.8 implementados y verdes.
- [ ] P2: B2.1–B2.8 implementados y verdes (incluye 1 Play Mode).
- [ ] P3: B3.1–B3.8 implementados y verdes.
- [ ] Cada prototipo se ejecuta solo con su escena.
- [ ] Cada prototipo se explica en ≤ 5 minutos.
- [ ] Cada prototipo tiene su demo de propiedad de diseño ensayada.
- [ ] La suite completa pasa en una máquina limpia con Unity 6000.5.9f1.
