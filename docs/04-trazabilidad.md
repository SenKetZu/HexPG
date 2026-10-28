# 04 — Trazabilidad y cumplimiento de la consigna

> Estado al 25/10 (scaffolding). `✅` = test escrito; `☐` = comportamiento definido, test pendiente.
> La columna **"defecto que detectaría"** responde al punto §4.2 de la consigna.

## 1. P1 — Exposición del estado (ISP + Observer)

| ID | Test | Modo | Estado | Defecto que detectaría |
| --- | --- | --- | --- | --- |
| B1.1 | `IGridStatePublisher_ExposesOnlyEvents` | Edit | ✅ | Agregar una mutación al contrato de publicación y filtrarla al presentador |
| B1.2 | `IGridQueryService_ExposesNoMutatingMembers` | Edit | ✅ | Dar a la IA un método para escribir el mapa (God Object encubierto) |
| B1.3 | `SetCell_WithSameValue_DoesNotRaiseChange` | Edit | ✅ | Notificar por *polling* encubierto: eventos en cada escritura, aunque no cambie nada |
| B1.4 | `SetCell_WithDifferentValue_RaisesExactlyOneChange` | Edit | ✅ | Emitir dos eventos por una mutación (doble redibujo) o perder el cambio |
| B1.5 | `SetOccupied_OnlyRaisesWhenItChanges` | Edit | ☐ | Recalcular ocupación sin cambio real |
| B1.6 | `Unsubscribe_StopsReceivingEvents` | Edit | ☐ | Fugas de suscripción: la vista sigue redibujando tras desactivarse |
| B1.7 | Firma `Pathfinder(IGridQueryService)` + B1.8 | Edit | ✅ | Acoplar el pathfinding al modelo concreto y romper ISP |
| B1.8 | `FindPath_AvoidsBlockedCells_AndReturnsMinimumCost` | Edit | ✅ | Algoritmo voraz que elige menos pasos en vez de menor costo |
| B1.9 | `FindPath_WhenBudgetExceeded_ReturnsBudgetExceeded` | Edit | ☐ | Devolver ruta parcial y mover al jugador más de lo permitido |
| B1.10 | `FindPath_WhenGoalBlockedOrMissing_ReturnsExplicitFailure` | Edit | ☐ | `null` silencioso o excepción ante un estado de juego esperable |
| B1.11 | `FindPath_SameInput_ProducesSamePathTwice` | Edit | ☐ | Desempate inestable por orden de `Dictionary` o heap |
| B1.12 | `HasLineOfSight_BlockerInMiddle_ReturnsFalse_EndpointsDoNotBlock` | Edit | ☐ | Bloquear con la celda del atacante u objetivo |
| B1.13 | `Presenter_RedrawsZeroTimesWithoutMutation` | Edit | ☐ | `Update()` que redibuja siempre (la propiedad central del desafío) |

## 2. P2 — Pipeline de comandos asíncrono

| ID | Test | Modo | Estado | Defecto que detectaría |
| --- | --- | --- | --- | --- |
| B2.1 | `Submit_RejectedCommand_KeepsStateAndDoesNotAnimate` | Edit | ✅ | Consumir PA antes de validar, o animar una acción inexistente |
| B2.2 | ídem B2.1 (aserción `presentation.Calls == 0`) | Edit | ✅ | Llamar a la vista aunque el comando se rechace |
| B2.3 | `Submit_AppliesLogicBeforeTheAnimationCompletes` | Edit | ✅ | Acoplar la regla a la animación (lógica aplicada al terminar el frame) |
| B2.4 | ídem B2.3 (aserción de fase al completar) | Edit | ✅ | Quedar en `Executing` si la animación termina |
| B2.5 | `Submit_SecondCommand_QueuesUntilTheFirstAnimationCompletes` | Edit | ✅ | Intercalar dos comandos durante una animación (estado corrupto) |
| B2.6 | `Submit_InterleavedCommands_SpendsExactActionPoints` | Edit | ✅ | Cobrar de menos o dos veces en acciones intercaladas |
| B2.7 | `Submit_NewCommandVariant_RequiresNoPipelineChange` | Edit | ✅ | Obligar a modificar el pipeline por cada habilidad nueva |
| B2.8 | `EndTurn_RefillsActionPointsToMaximum` | Edit | ✅ | Acumular PA entre turnos (economía rota) |
| B2.9 | `Submit_DuringEnemyTurn_IsRejectedWithWrongPhase` | Edit | ✅ | Permitir al jugador actuar en el turno enemigo |
| B2.10 | `Driver_OnStart_SubmitsCommandsAndCompletesAnimations` | **Play** | ✅ | Que Unity nunca invoque el adaptador, o que la animación no complete |

## 3. P3 — DI + módulos data-driven

| ID | Test | Modo | Estado | Defecto que detectaría |
| --- | --- | --- | --- | --- |
| B3.1 | `EquipmentService_WithInjectedFakeCatalog_ResolvesModulesById` | Edit | ✅ | Dependencia oculta de un singleton que impide inyectar un doble |
| B3.2 | `Calculate_AddsAllBonusesToBaseStats` | Edit | ✅ | Olvidar un modificador o pisar el valor base |
| B3.3 | `TryEquip_ReplacingModule_DoesNotDoubleCount` | Edit | ✅ | Sumar el módulo anterior y el nuevo a la vez |
| B3.4 | `Unequip_RemovesExactBonuses` | Edit | ✅ | Dejar residuo del módulo quitado |
| B3.5 | `Calculate_DeduplicatesSharedAbilities` | Edit | ☐ | Duplicar habilidades compartidas (Flyweight mal entendido) |
| B3.6 | `Calculate_IsIdempotent_AndDoesNotMutateInputs` | Edit | ✅ | Acumular bonificaciones en cada cálculo (mutar la base) |
| B3.7 | `ShipModuleAsset_MapsToDefinition_AllFields` | Edit (integración) | ✅ | Campo sin mapear (p. ej. `Tier`) que rompe el reemplazo |
| B3.8 | `TryGetModule_OnEmptySlot_ReturnsFalse` | Edit | ☐ | Representar el slot vacío con un módulo "vacío" y calcular basura |
| B3.9 | `EquipmentService_UnknownModule_KeepsStatsUnchanged` | Edit | ✅ | Aplicar parcialmente un equipamiento fallido |
| B3.10 | `TryEquip_WithUnknownSlotType_ReturnsWrongSlotTypeAndKeepsLoadout` | Edit | ✅ | Aceptar un slot inexistente y escribir fuera del arreglo |
| B3.11 | `TryEquip_ReturnsNewLoadout_WithoutMutatingTheOriginal` | Edit | ✅ | Mutar el loadout compartido y romper comparaciones/undo |

## 4. Cumplimiento de la consigna

| Requisito | Dónde se cumple | Evidencia |
| --- | --- | --- |
| §2: entrega del TP1 como referencia | raíz del repo | `GDD.md`, `DIAGRAMS.md` (verificar que estén commiteados) |
| §2: código de prototipos + tests | `Assets/Prototypes/**` | Suite en Test Runner |
| §2: instrucciones de apertura y ejecución | `README.md` | Pasos reproducibles + Unity 6000.5.9f1 |
| §2: documentación de alcance y exclusiones | `docs/03-prototipos.md` | Tablas por prototipo |
| §2: tag de entrega | `docs/06-plan.md` §4 | `tp2` / `rtp2` |
| §3.1: 3 prototipos independientes, una escena cada uno | `Assets/Prototypes/Pn_*` | Escenas generadas por `PrototypeSceneBuilder` |
| §3.1: comportamiento implementado y funcionando | escenas de demo | Recorrido en vivo |
| §3.1: tests relevantes que pasan | matrices §1–§3 | Test Runner |
| §3.1: explicar implementación y decisiones | `docs/03`, `docs/05` | Defensa |
| §3.2: defender con código y tests | `docs/05-defensa.md` | Demo + preguntas |
| §4.1: problema/solución del TP1 explícitos | `docs/03` (encabezado de cada prototipo) | Cita literal del PDF |
| §4.1: supuestos y exclusiones justificadas | `docs/03` | Tablas de exclusión |
| §4.1: ajustes respecto del TP1 | `docs/03` §final | 6 ajustes |
| §4.2: qué verifica cada test y qué error detecta | matrices §1–§3 | Columna "defecto" |
| §4.2: reproducibilidad, sin orden ni pasos manuales | `docs/02` §3 | Suite desde cero |
| §4.2: Edit/Play Mode donde corresponde | `docs/02` §2 | Solo P2 en Play Mode |
| §4.3: demostrar propiedades del diseño | `docs/03` (tabla por prototipo) | ISP por reflexión, OCP, DIP con fake |
| §4.4: contratos, `null`, mutabilidad, excepciones | `docs/01` §6, `docs/03` | Preguntas de defensa |
| §4.5: README con cómo ejecutar y testear | `README.md` | Verificación en vivo |

## 5. Checklist de mínimos (§3.1)

- [x] Scaffolding de los 3 prototipos con asmdefs y dominio sin `UnityEngine`.
- [x] Suite verde: **80 tests EditMode** (P1 5 · P2 7 · P3 9 · GameLoop 59) y **1 PlayMode** (P2).
- [x] Comportamientos marcados en §1–§3, todos con test.
- [x] Escenas generadas y verificadas (`VoidHex/Build Prototype Scenes` + `check_scenes.py`).
- [x] Separaciones de capas verificadas por `tools/verify/check_layers.py` (R1, R2, R3).
- [ ] Cada prototipo explicado en ≤ 5 minutos.
- [ ] Demos de propiedad de diseño ensayadas (ISP, OCP, DIP/Flyweight).
- [ ] Tag `tp2` y envío por mail.
