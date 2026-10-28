# 03 — Prototipos: alcance, exclusiones y decisiones

> Los tres prototipos corresponden **literalmente** a los tres desafíos declarados en
> `Void Hex Tactics - Arquitectura (Resumen Ejecutivo).pdf` (§ DESAFÍO 1, 2 y 3).
> Cada sección es lo que se defiende en 5 minutos: problema → promesa → evidencia → decisión → exclusión.

---

## P1 — Exposición y sincronización del estado de la grilla

**Problema declarado (PDF, Desafío 1):** ¿cómo exponer el mapa (asteroides, ocupación) a IA,
comandos y UI sin un *God Object* ni *polling* en `Update()`?
**Solución declarada:** **segregación de interfaces (ISP) + Observer**.
**Qué implementa el TP2:** dos contratos C# puros —`IGridQueryService` (lectura síncrona) e
`IGridStatePublisher` (eventos)— implementados por un único `GridModel`, con una vista que
redibuja solo al recibir un evento.

### Alcance comprometido

Un mapa finito de celdas con terreno y ocupación. La IA/pathfinding consultan el estado de forma
síncrona; la vista se suscribe y se redibuja **exclusivamente** ante mutaciones reales. Ningún
consumidor de los contratos puede mutar el mapa. No hay `Update()` consultando el modelo.

### Comportamientos

| ID | Comportamiento |
| --- | --- |
| B1.1 | `IGridStatePublisher` expone **solo eventos** (verificado por reflexión) |
| B1.2 | `IGridQueryService` **no** expone ninguna operación de mutación (verificado por reflexión) |
| B1.3 | Escribir una celda con el mismo valor **no** emite evento (no hay mutación real) |
| B1.4 | Escribir una celda con otro valor emite **exactamente un** evento con coordenada y tipo |
| B1.5 | Cambiar la ocupación emite un evento de ocupación, y solo si cambió |
| B1.6 | Un suscriptor que se da de baja deja de recibir eventos |
| B1.7 | El pathfinding depende solo de `IGridQueryService`: calcula sin poder mutar ni notificar |
| B1.8 | `FindPath` devuelve la ruta de **costo mínimo** con costo de movimiento variable |
| B1.9 | Si el costo excede `maxCost`, devuelve `BudgetExceeded` (nunca una ruta parcial) |
| B1.10 | Objetivo bloqueado o inexistente devuelve un motivo explícito, sin excepción |
| B1.11 | Dos búsquedas con la misma entrada devuelven la misma ruta (determinismo) |
| B1.12 | La línea de visión se bloquea por celdas intermedias; los extremos nunca bloquean |
| B1.13 | El presentador redibuja 0 veces sin mutación y una vez por cada mutación real |

### Supuestos

- Mapa acotado (12×12 en la demo) definido por el prototipo.
- `MovementCost >= 1` en celdas transitables; los bloqueos son costo infinito explícito.
- Los bloqueos son estáticos durante la búsqueda; un solo agente, sin colisión entre unidades.
- Un único hilo: los eventos se emiten en el hilo que muta (el principal).

### Exclusiones (y por qué)

| Exclusión | Justificación | Cómo se extendería |
| --- | --- | --- |
| Alturas / mapas 3D multicapa | El desafío es de exposición de estado, no de geometría 3D | `HexCoordinates` + nivel `y`, vecinos por capa |
| Niebla de guerra / información parcial | Es un sistema de conocimiento, ajeno a la exposición del estado | `IVisibilityService` sobre `IGridQueryService` |
| Unidades móviles como bloqueos | Necesita un modelo de turnos, que es el prototipo P2 | Suscribirse a `Changed` y recalcular rutas |
| Render con Tilemap | La vista es el **consumidor** que se demuestra; el tilemap no cambia la propiedad | `GridPresenter` ya consume el contrato |
| Mapas grandes y optimización | Con 12×12 el costo no es el problema; el desafío es el acoplamiento | Cambiar la cola de prioridad sin tocar el contrato |
| Event Bus global | El PDF lo descarta: dependencias ocultas y no resuelve lecturas síncronas | — (anti-objetivo) |

### Contratos

```csharp
// Lectura síncrona: la consumen IA, comandos y pathfinding.
public interface IGridQueryService
{
    bool Contains(HexCoordinates c);
    bool TryGetCell(HexCoordinates c, out HexCell cell);
    bool IsPassable(HexCoordinates c);
    int MovementCostToEnter(HexCoordinates c);
    bool HasLineOfSight(HexCoordinates from, HexCoordinates to);
}

// Notificación reactiva: la consume solo la presentación.
public interface IGridStatePublisher
{
    event Action<GridChange> Changed;
}

// Único dueño del estado: las mutaciones NO están en ninguna interfaz.
public sealed class GridModel : IGridQueryService, IGridStatePublisher
{
    public bool SetCell(HexCell cell);                  // true solo si cambió
    public bool SetOccupied(HexCoordinates c, bool on);  // true solo si cambió
}
```

- **Precondiciones:** dimensiones > 0 (excepción si no); `GetCell` de una celda inexistente es
  error del programador ⇒ `ArgumentOutOfRangeException`; una ruta inválida **no** es error ⇒ resultado.
- **Garantías:** un `Set*` que devuelve `false` no emite evento; un evento corresponde a una
  mutación efectiva; `PathResult.IsFound` implica extremos correctos y ninguna celda bloqueada.
- **Quién muta:** solo `GridModel`, y solo a través de métodos que no están en los contratos.
- **Ausencia:** `TryGetCell` / `PathResult` con `PathFailure`; nunca `null`.

### Propiedad de diseño y demostración

| Promesa del PDF | Evidencia |
| --- | --- |
| ISP: la vista no puede alterar reglas; la IA no escucha eventos | Tests de reflexión B1.1/B1.2 + tipos en los constructores (`Pathfinder(IGridQueryService)`) |
| Render 100% reactivo, cero CPU en `Update()` | `GridPresenter` no tiene `Update`; B1.13 cuenta redibujos |
| Testeable sin gráficos | Toda la suite de P1 corre en Edit Mode; `noEngineReferences: true` en el dominio |

**Demo en vivo:** crear el `GridModel`, bloquear una celda y mostrar un único log de redibujo;
luego reescribir la misma celda y mostrar que **no** hay log.

---

## P2 — Flujo de acciones intercaladas y sincronización

**Problema declarado (PDF, Desafío 2):** ¿cómo alternar PA (mover → atacar → mover) validando en
C# e invocando animaciones en Unity de forma asíncrona?
**Solución declarada:** **pipeline de `ICommand` + ejecución asíncrona** (UniTask / Task).
**Qué implementa el TP2:** comandos inmutables que validan PA y ejecutan la lógica al instante en
el Core, y un puerto `ICommandPresentation` que reproduce la animación **después**, con una cola
que impide intercalar comandos a mitad de una animación.

### Alcance comprometido

Un turno con pozo de PA que se recarga al inicio. El jugador intercala comandos (mover, atacar,
escudo) en cualquier orden. La validación y el efecto son **instantáneos**; la animación se espera
de forma asíncrona sin condicionar la regla. Los comandos enviados durante una animación se
serializan. Agregar una variante no modifica el pipeline.

### Comportamientos

| ID | Comportamiento |
| --- | --- |
| B2.1 | Un comando sin PA suficiente se rechaza con `NotEnoughActionPoints` y el estado queda idéntico |
| B2.2 | Un comando rechazado **no** dispara animación |
| B2.3 | Un comando aceptado aplica su efecto **antes** de que la animación termine (estado ya mutado, `Phase = Executing`) |
| B2.4 | Al completarse la animación, la fase vuelve a `PlayerTurn` |
| B2.5 | Un segundo comando enviado durante la animación **espera** (cola); no se intercala |
| B2.6 | Tres comandos intercalados gastan exactamente los PA esperados |
| B2.7 | Una variante nueva (`ShieldCommand`) funciona sin tocar el pipeline (OCP) |
| B2.8 | `EndTurn` recarga los PA al máximo, sin acumular |
| B2.9 | Durante el turno enemigo, un comando se rechaza con `WrongPhase` y no anima |
| B2.10 | Play Mode: el adaptador de Unity ejecuta la secuencia, completa las animaciones y no bloquea la lógica |

### Supuestos

- Un jugador y un enemigo fijo; el costo en PA lo declara cada comando.
- El turno enemigo se activa por método; no hay IA (fuera de alcance).
- La animación es un contador de frames en el adaptador; no hay arte.
- Un solo hilo (contexto de sincronización de Unity).

### Exclusiones (y por qué)

| Exclusión | Justificación | Cómo se extendería |
| --- | --- | --- |
| Undo / Redo | El PDF lo menciona como *habilitador*, no como requisito del prototipo; agregarlo exige un historial y su propia suite | Historial de estados antes/después en el pipeline |
| IA de decisiones | El desafío es el flujo de comandos, no la búsqueda de jugadas | El pipeline acepta cualquier `ICommand`; se encola el plan de la IA |
| Animaciones y arte reales | No demuestran la propiedad; además acoplan tiempos | `ShipView` ya es el punto de sustitución |
| Condiciones de victoria/derrota | Agregan estados no testeados y no aportan al desafío | Regla al final del turno + fase nueva |
| Efectos de terreno | Son reglas del prototipo P1 / otro sistema | Comando que consulte `IGridQueryService` |
| Red / multijugador | Cambia el modelo de autoridad y el alcance completo | Comandos serializables y pipeline autoritativo |

### Contratos

```csharp
public interface ICommand
{
    int ActionPointCost { get; }
    string VisualKey { get; }                     // dato para la vista, no comportamiento
    CommandValidation Validate(ShipModel model);
    ShipModel Execute(ShipModel model);           // instantáneo; precondición: Validate == Allowed
}

public interface ICommandPresentation
{
    Task PlayAsync(CommandExecution execution);   // puerto: el Core no espera frames
}

public sealed class CommandProcessor
{
    ShipModel State { get; }                      // único dueño del estado
    TurnPhase Phase { get; }                      // PlayerTurn | Executing | EnemyTurn
    Task<CommandOutcome> SubmitAsync(ICommand command);
    void EndTurn();
}
```

- **Precondiciones:** `command != null` y `ActionPointCost >= 0` (excepciones si no);
  `Execute` solo se invoca tras `Validate` exitoso.
- **Garantías:** un rechazo no muta `State` ni llama a la presentación; un comando aceptado aplica
  el efecto una sola vez; `Phase` vuelve a `PlayerTurn` incluso si la presentación falla.
- **Quién muta:** solo `CommandProcessor`. `ShipModel` es inmutable (`With…`).
- **Ausencia:** `CommandOutcome` con `IsAccepted` y `CommandValidation` con motivo; nunca `null`.

### Propiedad de diseño y demostración

| Promesa del PDF | Evidencia |
| --- | --- |
| Desacoplamiento temporal: validación instantánea vs animación async | B2.3: el estado ya cambió y el `Task` sigue pendiente |
| OCP: habilidad nueva sin tocar la FSM ni el controlador | B2.7 (`ShieldCommand`) y demo de `TractorBeamCommand` en vivo |
| No FSM monolítica | Solo 3 fases; ninguna por combinación de acciones |
| No corrutinas centralizadas | La espera es un `Task` en el puerto, no `WaitForSeconds` en la regla |
| No singletons | `CommandProcessor` recibe la presentación por constructor; el driver compone |

**Decisión sobre UniTask:** el PDF declara *UniTask / Task*. Se usa `Task` (BCL) para que la suite
sea reproducible sin dependencias de red. El punto de sustitución es `ICommandPresentation` +
`ShipView`; migrar a UniTask no toca dominio, pipeline ni tests.

---

## P3 — Gestión de servicios y módulos data-driven

**Problema declarado (PDF, Desafío 3):** ¿cómo conectar servicios sin singletons estáticos
(`GameManager.Instance`) y definir módulos (MK I–III)?
**Solución declarada:** **inyección de dependencias con VContainer + ScriptableObjects (Flyweight)**.
**Qué implementa el TP2:** un `LifetimeScope` que registra catálogo, configuración y servicio; el
consumidor recibe `IEquipmentService` por `[Inject]`; los módulos son assets compartidos y el
dominio recibe `ModuleDefinition` inmutables.

### Alcance comprometido

Una nave con 4 slots (un tipo por slot). Equipar/quitar módulos recalcula estadísticas sumando
bonificaciones sobre la base, consolida habilidades y nunca muta las definiciones ni la base. El
servicio se resuelve por contenedor; en tests, inyectando un catálogo falso.

### Comportamientos

| ID | Comportamiento |
| --- | --- |
| B3.1 | El servicio recibe el catálogo por constructor; con un catálogo falso resuelve módulos (DIP) |
| B3.2 | Las estadísticas = base + suma de bonificaciones de los módulos equipados |
| B3.3 | Reemplazar MK I por MK II recalcula sin contar dos veces el anterior |
| B3.4 | Quitar un módulo elimina exactamente sus bonificaciones |
| B3.5 | Las habilidades se consolidan sin duplicados |
| B3.6 | El cálculo es idempotente y no muta la base ni la colección de entrada |
| B3.7 | El adaptador `ShipModuleAsset → ModuleDefinition` mapea todos los campos |
| B3.8 | Un slot vacío es ausencia explícita (`TryGetModule == false`), no un módulo vacío |
| B3.9 | Un módulo inexistente devuelve `UnknownModule` y deja las estadísticas intactas |
| B3.10 | Un `SlotType` fuera de rango devuelve `WrongSlotType` y conserva el loadout |
| B3.11 | El loadout original no se muta al equipar (se devuelve uno nuevo) |

### Supuestos

- 4 slots, uno por tipo; los modificadores son **aditivos**.
- Las definiciones son inmutables una vez construidas.
- No hay nivel, peso, costo ni inventario.

### Exclusiones (y por qué)

| Exclusión | Justificación | Cómo se extendería |
| --- | --- | --- |
| Modificadores multiplicativos | Ningún módulo del alcance los usa; la adición ya demuestra la composición | `StatModifier` con operación y orden |
| Set bonuses / sinergias | Es contenido, no arquitectura; agrega combinaciones no testeadas | Etapa de post-proceso sobre el loadout |
| Incompatibilidad de tipo de slot | **Imposible por construcción**: un slot por tipo y el módulo declara el suyo | Varios slots por tipo con validación explícita |
| Inventario, compra, drops | Sistema económico ajeno al desafío de DI | `IInventoryService` inyectado |
| Persistencia del loadout | No demuestra la propiedad | Serializar ids por slot |
| UI de drag & drop | Presentación; el servicio ya expone `Equip`/`Unequip` | `LoadoutDemo` con botones |

### Contratos

```csharp
public readonly struct ModuleDefinition { /* Id, Slot, Tier, bonus*, GrantedAbilityId */ }

public sealed class Loadout            // inmutable
{
    EquipResult TryEquip(ModuleDefinition module);   // devuelve un loadout NUEVO
    EquipResult Unequip(SlotType slot);
    bool TryGetModule(SlotType slot, out ModuleDefinition module);
}

public interface IModuleCatalog { bool TryGet(string id, out ModuleDefinition m); }

public interface IEquipmentService
{
    Loadout Current { get; }
    ShipStats CurrentStats { get; }
    EquipResult Equip(string moduleId);
    EquipResult Unequip(SlotType slot);
    event Action<ShipStats> StatsChanged;            // solo cuando cambian de verdad
}
```

- **Precondiciones:** `catalog != null` (excepción); ids vacíos ⇒ `InvalidModule` (resultado).
- **Garantías:** `Calculate` es pura, total e idempotente; `Equip` fallido no cambia el estado;
  `StatsChanged` se emite solo si las estadísticas efectivamente cambian.
- **Quién muta:** `Loadout` es inmutable; `EquipmentService` reemplaza su referencia actual.
- **Ausencia:** `TryGetModule` / `GrantedAbilityId` vacío; nunca `null` cruza el dominio.

### Propiedad de diseño y demostración

| Promesa del PDF | Evidencia |
| --- | --- |
| DIP: inyección de mocks en tests | B3.1 con `FakeCatalog` en Edit Mode |
| Sin Singleton ni Service Locator | No existe `Instance`; el consumidor recibe la interfaz por `[Inject]` |
| Flyweight: datos compartidos | Un asset por módulo, referenciado por el catálogo; el dominio copia datos |
| Data-driven: MK III sin recompilar | Crear un asset nuevo en el editor y equiparlo en la demo |

**Demo en vivo:** en el inspector, crear un `ShipModuleAsset` MK III y equiparlo desde el catálogo,
sin tocar código ni recompilar.

---

## GameLoop — capa de composición (no es un cuarto desafío)

`Assets/GameLoop/` **no** agrega un problema nuevo del TP1. Existe para responder la pregunta
que la defensa va a recibir: *¿los tres prototipos encajan entre sí, o son tres demos sueltas?*

### Alcance comprometido

Una escena jugable (`GameLoop.unity`) que usa **los tres prototipos a la vez**:

| Etapa | Qué prototipo aporta | Qué se ve |
| --- | --- | --- |
| Hangar | P3 (VContainer + ScriptableObjects) | Equipar módulos; las stats se recalculan |
| Despliegue | P3 → P1 | La nave y la torreta ocupan celdas de la grilla |
| Combate | P1 (consulta) + P2 (pipeline) | Clic para mover, hover con camino previsualizado, área de alcance, atacar, terminar turno |
| Resolución | P2 (presentación async) | Animación de la ejecución, victoria/derrota, vuelta al hangar |

La lógica vive en `VoidHex.GameLoop.Logic` con `noEngineReferences: true`: `GameSession` decide,
`LoopDriver` orquesta contra Unity y `LoopGridView`/`GameLoopScreen` solo dibujan.

### Decisiones

- **La composición no reimplementa reglas.** `GameSession` consulta `IGridQueryService` (P1) y
  emite `ICommand` al pipeline (P2); el loadout viene de P3. Si una regla cambia, cambia en su
  prototipo, no acá.
- **El alcance de ataque se calcula en el dominio** (`AttackReach`, `CanAttackFrom`) y la vista
  solo pinta el resultado: lo que se dibuja y lo que se valida salen de la misma evaluación.
- **`IsOpenGround` vs `IsPassable`**: `IsPassable` excluye celdas ocupadas, así que usarlo para
  calcular alcance dejaba fuera la celda de la propia nave. Fue un defecto real, con test.

### Exclusiones (y por qué)

| Excluido | Por qué | Cómo se extendería |
| --- | --- | --- |
| Más de un enemigo, IA | No demuestra ninguna propiedad de los tres desafíos | `GameSession` ya itera comandos: un `TurretModel` por entidad |
| Guardado, progresión, menús | Fuera del alcance declarado (§4.1) | Capa de persistencia sobre `ShipConfig` |
| Arte y VFX | No aporta a la evaluación | El `ModelLibrary` ya centraliza la presentación |

### Propiedad de diseño y demostración

- **D1**: en la escena del bucle, la UI y la grilla conviven sin que la vista consulte el modelo
  por frame → el hover se recalcula por mouse, no por estado.
- **D2**: un comando rechazado (fuera de PA/rango) devuelve motivo y la nave no se mueve.

Evidencia: 59 tests EditMode en `Assets/GameLoop/Tests/EditMode/`
(`GameSessionTests`, `AttackAreaTests`, `HexProjectionTests`, `ResultStructInvariantTests`).

---

1. **P1 cambia de eje.** El informe largo del TP1 desarrolla la matemática hexagonal y el A*; el
   *resumen ejecutivo* declara como desafío la **exposición del estado (ISP + Observer)**. El TP2
   implementa el desafío declarado: la geometría y el A* son el sustrato que consume
   `IGridQueryService`, no la tesis del prototipo.
2. **`Task` en lugar de UniTask.** Declarado como alternativa en el PDF; elimina una dependencia
   de red y deja la sustitución en una sola interfaz.
3. **Undo/Redo fuera de alcance.** El PDF lo menciona como capacidad que el patrón habilita, no
   como requisito; se excluye con justificación y ruta de extensión.
4. **Sin FSM monolítica.** El PDF descarta un estado por combinación de acciones; se usan tres fases.
5. **ScriptableObject como adaptador.** El dominio recibe `ModuleDefinition`: se conserva el flujo
   data-driven y el dominio queda libre de `UnityEngine`.
6. **Un slot por tipo.** Elimina el estado inválido "módulo en slot incompatible" en lugar de testearlo.
