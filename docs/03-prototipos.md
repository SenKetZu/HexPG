# 03 — Prototipos: alcance, exclusiones y decisiones

> Cada sección es la página que se defiende en 5 minutos. La matriz de tests está en
> `04-trazabilidad.md`.

---

## P1 — Grilla hexagonal y pathfinding

**Problema del TP1:** matemática de la grilla hexagonal y pathfinding con costo variable
(`GDD.md` §4.3.1 y §4.3.2; `DIAGRAMS.md` §6).
**Solución del TP1 implementada:** Solución C — coordenadas cúbicas $q+r+s=0$ en C# puro,
separadas del render.

### Alcance comprometido

Dado un mapa finito de celdas transitables con costo y celdas bloqueadas, `FindPath` devuelve la
ruta de **costo mínimo** entre dos celdas dentro de un presupuesto `maxCost`, o un resultado
`Unreachable` explícito. Se incluye distancia, vecinos, rango y línea de visión.

### Comportamientos

| ID | Comportamiento |
| --- | --- |
| B1.1 | `Distance` es simétrica, vale 0 entre iguales y cumple el invariante $q+r+s=0$ |
| B1.2 | `Neighbors` devuelve exactamente las 6 celdas adyacentes dentro del mapa, y la relación es simétrica |
| B1.3 | `FindPath` devuelve la ruta de costo mínimo (el costo de entrar a una celda es su `MovementCost`; la celda inicial no se paga) |
| B1.4 | Si el objetivo es alcanzable pero excede `maxCost`, devuelve `Unreachable` con motivo `BudgetExceeded` (no una ruta parcial) |
| B1.5 | Si el objetivo está bloqueado o no existe, devuelve `Unreachable` con motivo explícito, sin lanzar excepción |
| B1.6 | Dos ejecuciones con la misma entrada devuelven la misma ruta (desempate determinista) |
| B1.7 | `HasLineOfSight` bloquea si una celda intermedia bloquea; los extremos no bloquean |
| B1.8 | El mapa no se muta durante la búsqueda (misma cantidad y costos antes y después) |

### Supuestos

- Coordenadas axiales `(q, r)` dentro de un mapa acotado (≤ 12×12) definido por el prototipo.
- `MovementCost >= 1` para celdas transitables; los bloqueos se representan por costo infinito
  explícito, no por `null`.
- Los bloqueos son estáticos durante la búsqueda (una sola consulta).
- Un solo agente: las unidades no bloquean el paso entre sí dentro de este prototipo.

### Exclusiones (y por qué)

| Exclusión | Justificación | Cómo se extendería |
| --- | --- | --- |
| Alturas / mapas 3D, varias capas | El TP1 no las modela; agregarlas cambia la representación, no la decisión de diseño | `HexCoordinates` + nivel `y` y `Neighbors` por capa |
| Movimiento diagonal / arcos de disparo curvos | No existen en grillas hexagonales; cubrirlos sería modelar otro problema | Regla de adyacencia en `HexGrid` |
| Mapas grandes y optimización (JPS, buckets) | Con ≤ 12×12 el costo no es el problema; el TP1 promete claridad, no throughput | Cambiar el `IPriorityQueue` interno sin tocar el contrato |
| Niebla de guerra / información parcial | Es un sistema de presentación y conocimiento, ajeno a la geometría | Capa de `VisibilityService` sobre `HexGrid` |
| Los blockers como unidades móviles | Requiere un modelo de turnos (P2), que este prototipo no tiene | Recalcular rutas por evento de movimiento |
| Renderizado con Tilemap | El render es una vista sin reglas; no hay comportamiento que testear | `HexGridRenderer` ya consume el dominio |

### Contratos

```csharp
// Dominio puro. No hay null: la ausencia es un resultado explícito.
PathResult FindPath(HexCoordinates start, HexCoordinates goal, int maxCost);

readonly struct PathResult
{
    bool IsFound;                 // true solo si hay ruta completa
    IReadOnlyList<HexCoordinates> Path; // vacía si IsFound == false
    int TotalCost;
    PathFailure Failure;          // None | Unreachable | BudgetExceeded | InvalidStart | InvalidGoal
}
```

- **Precondiciones:** `maxCost >= 0`; el mapa debe existir. Mapa `null` ⇒ `ArgumentNullException`
  (error del programador, no estado de juego).
- **Garantías:** si `IsFound`, `Path[0] == start`, `Path[^1] == goal`, sin celdas bloqueadas y
  `TotalCost <= maxCost`.
- **Quién muta:** nadie. `HexGrid` se construye una vez y se consulta; el pathfinder no escribe.
- **Determinismo:** orden de vecinos fijo (`NE, E, SE, SW, W, NW`) y desempate por
  `(f, g, orden de inserción)`.

### Propiedad de diseño y demostración

| Promesa | Evidencia |
| --- | --- |
| Lógica independiente de Unity | Toda la suite de P1 corre en Edit Mode sin escena; el `.asmdef` del dominio tiene `noEngineReferences: true` |
| Extensibilidad sin tocar el algoritmo | Test con un `ICellCostProvider` alternativo (terreno "lento"): A* no se modifica |
| Determinismo | B1.6 corre la búsqueda 2 veces y compara ruta y costo |

### Escena de demo

`P1_HexGrid.unity`: mapa de 12×12 con celdas bloqueadas, selección de origen y destino por clic,
ruta resaltada y mensaje de rechazo (`Unreachable` / `BudgetExceeded`).

---

## P2 — Turnos, economía de PA y patrón comando

**Problema del TP1:** flujo de turnos, PA y ejecución de comandos intercalados
(`GDD.md` §4.3.1 Problema 2; `DIAGRAMS.md` §3, §4, §7, §9).
**Solución del TP1 implementada:** FSM + `ICommand`, con los ajustes de `01-arquitectura.md` §5.

### Alcance comprometido

Un turno de un jugador con un pozo de PA que se recarga al inicio. El jugador intercala comandos
(mover, atacar, escudo) en cualquier orden; cada comando se valida **antes** de ejecutarse, se
aplica exactamente una vez, y puede deshacerse restaurando el estado anterior exacto. La FSM
impide transiciones fuera de fase.

### Comportamientos

| ID | Comportamiento |
| --- | --- |
| B2.1 | Al iniciar el turno, los PA se recargan a `MaxActionPoints` (no se acumulan entre turnos) |
| B2.2 | Se pueden intercalar 3 comandos válidos en un turno y el PA final es exactamente el esperado |
| B2.3 | Un comando sin PA suficiente se rechaza con `NotEnoughActionPoints(required, available)` y el estado queda **idéntico** |
| B2.4 | Un comando aceptado aplica su efecto exactamente una vez (sin doble aplicación) |
| B2.5 | `Undo` restaura el snapshot anterior (PA, celda, escudos, hull enemigo) y el historial se consume una sola vez |
| B2.6 | La FSM acepta solo transiciones declaradas; una transición inválida devuelve motivo y no cambia de estado |
| B2.7 | Un comando en fase incorrecta se rechaza con `WrongPhase` sin efectos |
| B2.8 | Se emite un evento por comando aceptado y **ninguno** por comando rechazado (validado en Play Mode) |

### Supuestos

- Un jugador y un objetivo (dummy) con hull; no hay múltiples unidades ni iniciativa.
- El costo en PA de cada comando lo define el comando, no el estado.
- El turno enemigo ejecuta una **lista de comandos prefijada** (guion), no IA.
- El fin de turno no aplica anomalías gravitacionales ni efectos de terreno (fuera de alcance).
- La animación es instantánea para los tests (el dominio no espera frames).

### Exclusiones (y por qué)

| Exclusión | Justificación | Cómo se extendería |
| --- | --- | --- |
| IA de decisiones enemigas | El problema es el flujo de turnos y comandos, no la búsqueda de decisiones | El turno enemigo ya consume una cola de `ICommand`; se enchufa un planificador |
| Animaciones y espera real | El dominio no debe depender de frames; la animación es Presentación | Evento `CommandResolved` → animación → callback al `TurnSession` |
| Condiciones de victoria/derrota | No hacen falta para probar la economía de PA y la FSM; agregan estados no testeados | Nuevos estados en la FSM + regla al final del `Resolution` |
| Efectos de terreno (anomalías, nebulosas) | Son reglas de otro sistema (entorno) y mezclan dos problemas | `ITurnEndEffect` aplicado en `Resolution` |
| Guardado / carga de turno | No es una propiedad del diseño evaluada | Serializar `TurnState` + historial |
| Multijugador / red | Cambia el modelo de autoridad y el alcance completo | Comandos serializables y `TurnSession` autoritativa |

### Contratos

```csharp
// Estado inmutable: el turno entero son 4 enteros.
readonly struct TurnState
{
    int PlayerCell; int ActionPoints; int Shields; int EnemyHull;
    TurnState WithActionPoints(int ap); // y análogos
}

// Validar y ejecutar son pasos separados y explícitos.
interface ICommand
{
    int ActionPointCost { get; }
    CommandValidation Validate(TurnState state);   // Allowed | Rejected(reason, required, available)
    TurnState Execute(TurnState state);            // solo si Validate fue Allowed
}

// Único dueño del estado y del historial. Capa Aplicación (C# puro).
sealed class TurnSession
{
    TurnState State { get; }
    CommandOutcome Submit(ICommand command);       // valida, aplica, apila snapshot, emite evento
    UndoResult UndoLast();                         // restaura snapshot o HistorialVacío
}
```

- **Precondiciones:** `command != null`; `ActionPointCost >= 0`.
- **Garantías:** un `Submit` rechazado no muta `State` ni el historial; `UndoLast` deja el estado
  bit a bit igual al previo al último comando aceptado.
- **Quién muta:** solo `TurnSession`. Los comandos son funciones puras sobre `TurnState`.
- **Ausencia:** `UndoResult` explícito (`Undone` / `NothingToUndo`), nunca `null`.

### Propiedad de diseño y demostración

| Promesa | Evidencia |
| --- | --- |
| Extensibilidad (Open/Closed) | Se agrega `ShieldCommand` (o `TeleportCommand`) sin tocar `TurnSession` ni la FSM; solo un test nuevo |
| Desacoplamiento | Se sustituye el ejecutor por un fake y se verifica que la UI no consulta el estado por frame, sino que reacciona al evento |
| Control de estado | `TurnState` inmutable y solo `TurnSession` lo reemplaza; transición inválida de FSM testeados en B2.6 |
| Undo exacto | Snapshot, no aritmética: B2.5 compara el estado completo antes/después |

### Escena de demo

`P2_TurnFlow.unity`: barra de PA, botones Mover / Atacar / Escudo / Deshacer / Terminar turno y un
log de eventos. El `TurnDemoDriver` ejecuta un comando al iniciar para el test de Play Mode.

---

## P3 — Slots, módulos y recálculo de estadísticas

**Problema del TP1:** slots de equipamiento, módulos MK I/II/III y modificadores
(`GDD.md` §4.3.1 Problema 3; `DIAGRAMS.md` §5 y §8).
**Solución del TP1 implementada:** data-driven + composición, con el `ScriptableObject` como
adaptador (no como modelo de dominio).

### Alcance comprometido

Una nave con 4 slots tipados (Weapon, Propulsion, Shield, Exotic). Equipar/quitar módulos
recalcula estadísticas **sumando** modificadores sobre las base, consolida habilidades otorgadas y
nunca muta las definiciones ni las estadísticas base.

### Comportamientos

| ID | Comportamiento |
| --- | --- |
| B3.1 | Un módulo solo entra en un slot de su `SlotType`; si no coincide, `WrongSlotType` y loadout intacto |
| B3.2 | Las estadísticas calculadas = base + suma de bonificaciones de los módulos equipados |
| B3.3 | Reemplazar MK I por MK II recalcula sin contar dos veces el módulo anterior |
| B3.4 | Quitar un módulo elimina exactamente sus bonificaciones (vuelve a los valores previos) |
| B3.5 | Las habilidades otorgadas se consolidan sin duplicados por `Id`, preservando orden |
| B3.6 | El cálculo es idempotente y no muta `ShipBaseStats` ni `ModuleDefinition` |
| B3.7 | El adaptador `ShipModuleAsset → ModuleDefinition` mapea todos los campos (integración, Edit Mode) |
| B3.8 | Un módulo sin habilidad otorgada no agrega nada a la lista de habilidades (ausencia explícita) |

### Supuestos

- 4 slots, uno por tipo; cada slot admite un módulo o está vacío.
- Los modificadores son **aditivos** (el TP1 menciona suma/multiplicación; ver exclusión).
- Las definiciones de módulos son inmutables una vez construidas.
- No hay requisitos de nivel, peso, costo ni inventario.

### Exclusiones (y por qué)

| Exclusión | Justificación | Cómo se extendería |
| --- | --- | --- |
| Modificadores multiplicativos / porcentuales | Ningún módulo del alcance los usa; la composición aditiva ya demuestra la propiedad | `StatModifier` con `Op` (`Add`/`Multiply`) aplicado en orden declarado |
| Set bonuses / sinergias | Regla de contenido, no de arquitectura; agrega combinaciones no testeadas | Etapa de post-proceso sobre el loadout |
| Inventario, compra, drops | Sistema económico ajeno al problema de equipamiento | `IInventory` como dependencia del servicio |
| Persistencia del loadout | No demuestra la propiedad de recálculo | Serializar `ModuleDefinition.Id` por slot |
| UI de drag & drop | Presentación; el servicio ya expone `TryEquip`/`Unequip` | `LoadoutDemo` con botones (ya incluido) |
| Varios slots del mismo tipo | El alcance fija 4 slots únicos; multiplicarlo no cambia la decisión | `Loadout` con `List<Slot>` y política de duplicados |

### Contratos

```csharp
readonly struct ModuleDefinition
{
    string Id; SlotType Slot; int Tier;
    int BonusActionPoints; int BonusShields; int BonusMovement;
    string GrantedAbilityId;      // string.Empty = sin habilidad
}
readonly struct ShipBaseStats { int ActionPoints; int Shields; int Movement; int Hull; }
readonly struct ShipStats { int ActionPoints; int Shields; int Movement; int Hull; IReadOnlyList<string> Abilities; }

EquipResult Loadout.TryEquip(ModuleDefinition module); // Ok(Loadout) | WrongSlotType | UnknownSlot
ShipStats ShipStatsCalculator.Calculate(ShipBaseStats baseStats, IEnumerable<ModuleDefinition> modules);
```

- **Precondiciones:** `baseStats` válido; módulos con `Id` no vacío.
- **Garantías:** `Calculate` es pura, total (no lanza por módulos vacíos) e idempotente;
  no muta argumentos; `TryEquip` no muta el loadout original (devuelve uno nuevo).
- **Ausencia:** slot vacío ⇒ `TryGetModule(out …) == false`; habilidad ausente ⇒ `string.Empty`,
  nunca `null` (el `null` solo existe en el campo serializado del SO y se valida en el borde).

### Propiedad de diseño y demostración

| Promesa | Evidencia |
| --- | --- |
| Extensibilidad por datos | Se agrega "Cañón MK III" creando un `ModuleDefinition` nuevo (o un asset), **sin cambiar código**; test B3.2 con el nuevo módulo |
| Desacoplamiento del motor | El cálculo corre en Edit Mode sin `UnityEngine`; el SO es un adaptador testeado aparte (B3.7) |
| Transparencia referencial | B3.6 calcula dos veces y compara; verifica que base y definiciones quedan iguales |
| Control del estado | `Loadout` inmutable: `TryEquip` devuelve uno nuevo; el loadout original no cambia (test) |

### Escena de demo

`P3_Loadout.unity`: 4 slots, botones de módulos MK I/II/III, panel de estadísticas y habilidades
que se actualiza al equipar/quitar.

---

## Cambios respecto del TP1 (resumen para la defensa)

1. **ScriptableObject fuera del cálculo:** el dominio recibe `ModuleDefinition`; el SO es un
   adaptador. Se conserva el flujo data-driven y se gana testeabilidad.
2. **Comandos sincrónicos:** `Execute` devuelve el nuevo estado; la animación queda en
   Presentación. Se elimina `Task` del dominio.
3. **Undo por snapshot:** no se "devuelven" PA con aritmética; se restaura el estado completo.
4. **Validación con motivo:** `bool` → `CommandValidation` para que la UI explique y el test
   verifique la razón del rechazo.
5. **Ausencia explícita:** se reemplaza `null` por `TryGetModule` / `string.Empty` / resultados
   con motivo.
6. **FSM reducida:** solo los estados que el alcance implementa y testea.
