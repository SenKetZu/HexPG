# Void Hex Tactics — Diagramas de Arquitectura

Diagramas de **capas**, **flujo** y **clases** derivados del GDD (TP1: Diseño de Arquitectura en Unity).

---

## 1. Diagrama de Capas (Layered Architecture)

Separación estricta entre **Dominio (C# puro)**, **Aplicación (orquestación)** y **Presentación (Unity)**, con las dependencias apuntando siempre hacia el dominio.

```mermaid
flowchart TB
    subgraph P["🖥️ Presentación / Unity (MonoBehaviour)"]
        direction LR
        P1["HexGridRenderer<br/>(Tilemap 2D / 3D View)"]
        P2["HangarUI<br/>EquipmentSlotUI"]
        P3["ActionPointBar<br/>CommandButtons"]
        P4["InputHandler<br/>(Clicks / Input System)"]
        P5["ModuleAssets<br/>ScriptableObject Assets"]
    end

    subgraph A["⚙️ Aplicación (Orquestación / Servicios)"]
        direction LR
        A1["TurnStateMachine<br/>(PlayerTurn / EnemyTurn / Executing)"]
        A2["TurnCommandProcessor<br/>(Validar + Ejecutar + Undo)"]
        A3["ActionPointValidator"]
        A4["ShipEquipmentHandler<br/>(Slots ocupados/vacíos)"]
        A5["PathfindingService<br/>(A* sobre hexágonos)"]
    end

    subgraph D["🧠 Dominio (C# Puro — sin UnityEngine)"]
        direction LR
        D1["HexCoordinates<br/>(struct q,r,s)"]
        D2["HexGridLogic<br/>(mapa de HexCellData)"]
        D3["HexCellData<br/>(costo, LOS, ocupado)"]
        D4["ICommand<br/>MoveCommand / AttackCommand"]
        D5["UnitStats<br/>ShipStatsCalculator"]
        D6["ShipModuleData<br/>(interfaz de datos)"]
        D7["AbilityData"]
    end

    P1 --> D2
    P2 --> A4
    P3 --> A2
    P4 --> A2
    P5 -.lee datos.-> A4

    A1 --> A2
    A2 --> A3
    A2 --> A5
    A2 --> D4
    A4 --> D5
    A5 --> D1
    A5 --> D2
    D2 --> D3
    D3 --> D1
    D5 --> D6
    D4 --> D7

    classDef domain fill:#1e3a5f,stroke:#63b3ed,color:#fff
    classDef app fill:#2d3748,stroke:#ecc94b,color:#fff
    classDef pres fill:#22543d,stroke:#68d391,color:#fff
    class D1,D2,D3,D4,D5,D6,D7 domain
    class A1,A2,A3,A4,A5 app
    class P1,P2,P3,P4,P5 pres
```

**Regla de dependencia:** `Presentación → Aplicación → Dominio`. El dominio **nunca** referencia `UnityEngine`, por lo que es 100% testeable con NUnit.

---

## 2. Diagrama de Flujo — Core Loop (Bucle Principal)

```mermaid
flowchart TD
    Start([Inicio]) --> Hangar["🏗️ Estación Espacial / Hangar"]
    Hangar --> Equip["Equipar módulos<br/>(MK I, MK II, MK III)"]
    Equip --> Accept["Aceptar Contrato"]
    Accept --> SectorMap["🗺️ Mapa de Sector<br/>Selección de Nodos"]
    SectorMap --> NodeType{"Tipo de Nodo"}

    NodeType -->|Combate / Élite| Combat["⚔️ Combate Táctico Hexagonal"]
    NodeType -->|Recolección| Collect["📦 Misión de Recolección"]

    Combat --> TurnStart["Iniciar Turno"]
    TurnStart --> SpendPA["Gastar Puntos de Acción (PA)"]
    SpendPA --> Result{"Resultado"}
    Collect --> Result

    Result -->|Victoria| Loot["💎 Botín y Recompensas"]
    Result -->|Derrota| Tow["🚑 Remolque a la base<br/>(pierde botín no asegurado)"]

    Loot --> Secure{"¿Botín asegurado?"}
    Secure -->|Sí| Hangar
    Secure -->|No| Tow
    Tow --> Hangar

    Hangar --> Repair["Reparar nave"]
    Repair --> Upgrade["Mejorar / Comprar módulos"]
    Upgrade --> Hangar

    classDef base fill:#1a365d,stroke:#63b3ed,color:#fff
    classDef combat fill:#742a2a,stroke:#fc8181,color:#fff
    classDef reward fill:#22543d,stroke:#68d391,color:#fff
    class Hangar,Equip,Accept,SectorMap,Repair,Upgrade base
    class Combat,TurnStart,SpendPA combat
    class Loot,Collect reward
```

---

## 3. Diagrama de Flujo — Turno y Ejecución de Comandos Intercalados

```mermaid
flowchart TD
    TurnBegin(["Inicio de Turno"]) --> StateEnter["TurnStateMachine<br/>→ PlayerTurnState"]
    StateEnter --> Refill["Recargar PA = ShipStats.TotalActionPoints"]
    Refill --> WaitInput["Esperar input del jugador"]

    WaitInput --> PlayerAction{"¿Acción elegida?"}
    PlayerAction -->|Mover| BuildMove["Crear MoveCommand<br/>(path desde PathfindingService)"]
    PlayerAction -->|Atacar| BuildAttack["Crear AttackCommand"]
    PlayerAction -->|Habilidad| BuildAbility["Crear AbilityCommand"]
    PlayerAction -->|Terminar| EndTurn

    BuildMove --> Validate["ActionPointValidator<br/>CanExecute(unit)?"]
    BuildAttack --> Validate
    BuildAbility --> Validate

    Validate -->|PA insuficiente ❌| Reject["Rechazar acción<br/>(feedback UI)"]
    Reject --> WaitInput
    Validate -->|PA suficiente ✅| ExecState["TurnStateMachine<br/>→ ExecutingActionState"]

    ExecState --> Exec["await Command.ExecuteAsync(unit)"]
    Exec --> Consume["Consumir PA<br/>+ Emitir OnActionPointsChanged"]
    Consume --> Undo{"¿Deshacer?"}
    Undo -->|Sí| UndoOp["Command.Undo()<br/>(devolver PA + reponer posición)"]
    Undo -->|No| BackToPlayer
    UndoOp --> BackToPlayer["→ PlayerTurnState"]
    BackToPlayer --> WaitInput
    WaitInput -.->|Nota: Mover→Disparar→Mover| PlayerAction

    EndTurn["Fin de Turno: activar anomalías gravitacionales"] --> EnemyState["TurnStateMachine<br/>→ EnemyTurnState"]
    EnemyState --> EnemyAI["IA encola comandos enemigos"]
    EnemyAI --> TurnBegin

    classDef state fill:#2c5282,stroke:#63b3ed,color:#fff
    classDef cmd fill:#553c9a,stroke:#b794f4,color:#fff
    class StateEnter,ExecState,BackToPlayer,EnemyState,EndTurn state
    class BuildMove,BuildAttack,BuildAbility,Exec,UndoOp cmd
```

---

## 4. Diagrama de Secuencia — Ejecución de `MoveCommand`

```mermaid
sequenceDiagram
    autonumber
    participant UI as InputHandler / UI
    participant APV as ActionPointValidator
    participant PF as PathfindingService
    participant TCP as TurnCommandProcessor
    participant SB as StateMachine
    participant CMD as MoveCommand
    participant U as UnitController
    participant BAR as ActionPointBar (UI)

    UI->>PF: RequestPath(origin, target, grid)
    PF-->>UI: List<HexCoordinates> path
    UI->>CMD: new MoveCommand(target, path, costPerTile)
    UI->>TCP: Submit(CMD)
    TCP->>APV: CanExecute(unit)
    APV-->>TCP: true (PA suficientes)
    TCP->>SB: Transition(ExecutingActionState)
    TCP->>CMD: await ExecuteAsync(unit)
    CMD->>U: ConsumeActionPoints(cost)
    U-->>BAR: OnActionPointsChanged(cost) [evento C#]
    CMD->>U: await MoveAlongPathAsync(path)
    U-->>CMD: (animación finalizada)
    CMD-->>TCP: completed
    TCP->>SB: Transition(PlayerTurnState)
    Note over UI,BAR: UI nunca consulta al modelo por frame;<br/>reacciona al evento OnActionPointsChanged
```

---

## 5. Diagrama de Flujo — Cálculo de Estadísticas al Equipar (Problema 3)

```mermaid
flowchart TD
    HangarEnter(["Abrir Hangar"]) --> LoadBase["Cargar ShipBaseStatsSO"]
    LoadBase --> LoadSlots["ShipEquipmentHandler<br/>enumera EquipmentSlots"]
    LoadSlots --> ShowSlots["UI: mostrar slots ocupados / vacíos"]

    ShowSlots --> UserEquip{"¿Acción del jugador?"}
    UserEquip -->|Equipar módulo| CheckSlot{"¿Slot del tipo correcto?<br/>(SlotType)"}
    UserEquip -->|Quitar módulo| Remove["Vaciar slot"]
    UserEquip -->|Cambiar MK I → MK II| Remove

    CheckSlot -->|No ❌| RejectUI["Rechazar equipamiento"]
    RejectUI --> ShowSlots
    CheckSlot -->|Sí ✅| Assign["Asignar ShipModuleSO al slot"]
    Remove --> Recalc

    Assign --> Recalc["ShipStatsCalculator.RecalculateStats()"]
    Recalc --> SumBase["Iniciar con stats base de la nave"]
    SumBase --> Loop["foreach módulo equipado:<br/>+ bonusActionPoints<br/>+ bonusShields<br/>+ bonusMovementRange"]
    Loop --> Abilities["Consolidar habilidades otorgadas<br/>(grantedAbility)"]
    Abilities --> Apply["Aplicar UnitStats a la nave"]
    Apply --> RefreshUI["Actualizar HangarUI:<br/>PA | Escudos | Movimiento | Habilidades"]
    RefreshUI --> ShowSlots

    classDef data fill:#553c9a,stroke:#b794f4,color:#fff
    classDef ui fill:#22543d,stroke:#68d391,color:#fff
    class LoadBase,Recalc,SumBase,Loop,Abilities,Apply data
    class HangarEnter,ShowSlots,RefreshUI ui
```

---

## 6. Diagrama de Clases — Problema 1 (Grilla Hexagonal)

```mermaid
classDiagram
    class HexCoordinates {
        <<readonly struct>>
        +int Q
        +int R
        +int S
        +HexCoordinates(int q, int r)
        +static int Distance(a, b)
        +bool Equals(other)
        +int GetHashCode()
    }

    class HexDirection {
        <<enumeration>>
        NE
        E
        SE
        SW
        W
        NW
    }

    class HexCellData {
        +HexCoordinates Coordinates
        +int MovementCost
        +bool BlocksLineOfSight
        +bool IsOccupied
    }

    class HexGridLogic {
        -Dictionary~HexCoordinates,HexCellData~ _cells
        +int Width
        +int Height
        +HexCellData GetCell(HexCoordinates c)
        +bool Contains(HexCoordinates c)
        +IEnumerable~HexCoordinates~ Neighbors(HexCoordinates c)
        +IEnumerable~HexCoordinates~ CellsInRange(HexCoordinates c, int range)
    }

    class PathfindingService {
        +List~HexCoordinates~ FindPath(HexCoordinates start, HexCoordinates goal, int maxCost)
        -int Heuristic(HexCoordinates a, HexCoordinates b)
    }

    class LineOfSightService {
        +bool HasLineOfSight(HexCoordinates from, HexCoordinates to)
    }

    class HexGridRenderer {
        <<MonoBehaviour>>
        -HexGridLogic _grid
        +void RenderGrid()
        +void OnCellClicked(HexCoordinates c)
    }

    HexGridLogic "1" *-- "many" HexCellData : contiene
    HexCellData --> HexCoordinates : usa
    PathfindingService ..> HexGridLogic : consulta
    PathfindingService ..> HexCoordinates : calcula
    LineOfSightService ..> HexGridLogic : consulta
    HexGridRenderer --> HexGridLogic : lee (solo lectura)
    HexGridLogic ..> HexDirection : navega
```

---

## 7. Diagrama de Clases — Problema 2 (Comandos, Turnos y PA)

```mermaid
classDiagram
    class ICommand {
        <<interface>>
        +int ActionPointCost
        +bool CanExecute(UnitController unit)
        +Task ExecuteAsync(UnitController unit)
        +void Undo(UnitController unit)
    }

    class MoveCommand {
        -HexCoordinates _targetTile
        -List~HexCoordinates~ _path
        +int ActionPointCost
        +bool CanExecute(unit)
        +Task ExecuteAsync(unit)
        +void Undo(unit)
    }

    class AttackCommand {
        -HexCoordinates _target
        -AbilityData _ability
        +int ActionPointCost
        +bool CanExecute(unit)
        +Task ExecuteAsync(unit)
        +void Undo(unit)
    }

    class AbilityCommand {
        -AbilityData _ability
        +int ActionPointCost
    }

    class UnitController {
        +HexCoordinates Position
        +int CurrentActionPoints
        +UnitStats Stats
        +event OnActionPointsChanged
        +void ConsumeActionPoints(int amount)
        +void RefundActionPoints(int amount)
        +void SetPosition(HexCoordinates c)
        +Task MoveAlongPathAsync(List~HexCoordinates~ path)
    }

    class ActionPointValidator {
        +bool CanAfford(UnitController unit, ICommand cmd)
    }

    class TurnCommandProcessor {
        -Stack~ICommand~ _history
        +Task SubmitAsync(ICommand cmd, UnitController unit)
        +void UndoLast(UnitController unit)
    }

    class ITurnState {
        <<interface>>
        +void Enter()
        +void Exit()
        +void Tick()
    }

    class PlayerTurnState {
        +void Enter()
        +void Exit()
    }
    class EnemyTurnState {
        +void Enter()
        +void Exit()
    }
    class ExecutingActionState {
        +void Enter()
        +void Exit()
    }

    class TurnStateMachine {
        -ITurnState _current
        +void Transition(ITurnState next)
        +ITurnState Current
    }

    ICommand <|.. MoveCommand
    ICommand <|.. AttackCommand
    ICommand <|.. AbilityCommand
    TurnCommandProcessor --> ICommand : ejecuta / apila
    TurnCommandProcessor --> ActionPointValidator : valida
    TurnCommandProcessor --> TurnStateMachine : cambia estado
    ActionPointValidator ..> UnitController : consulta PA
    ICommand ..> UnitController : opera sobre
    ITurnState <|.. PlayerTurnState
    ITurnState <|.. EnemyTurnState
    ITurnState <|.. ExecutingActionState
    TurnStateMachine o-- ITurnState : estado actual
```

---

## 8. Diagrama de Clases — Problema 3 (Módulos y Estadísticas)

```mermaid
classDiagram
    class SlotType {
        <<enumeration>>
        Weapon
        Propulsion
        Shield
        Exotic
    }

    class ShipModuleSO {
        <<ScriptableObject>>
        +string moduleName
        +SlotType slotType
        +int tier
        +int bonusActionPoints
        +int bonusShields
        +int bonusMovementRange
        +AbilityDataSO grantedAbility
    }

    class ShipBaseStatsSO {
        <<ScriptableObject>>
        +int baseActionPoints
        +int baseShields
        +int baseMovementRange
        +int baseHull
    }

    class AbilityDataSO {
        <<ScriptableObject>>
        +string abilityName
        +int paCost
        +int damage
        +int range
    }

    class EquipmentSlot {
        +SlotType Type
        +ShipModuleSO Equipped
        +bool IsEmpty
        +bool CanAccept(ShipModuleSO module)
        +void Equip(ShipModuleSO module)
        +void Unequip()
    }

    class ShipEquipmentHandler {
        -List~EquipmentSlot~ _slots
        +IReadOnlyList~EquipmentSlot~ Slots
        +bool TryEquip(ShipModuleSO module)
        +void Unequip(int slotIndex)
        +IEnumerable~ShipModuleSO~ EquippedModules
        +event OnLoadoutChanged
    }

    class ShipStatsCalculator {
        +int TotalActionPoints
        +int TotalShields
        +int TotalMovementRange
        +List~AbilityDataSO~ AvailableAbilities
        +void RecalculateStats(ShipBaseStatsSO baseStats, IEnumerable~ShipModuleSO~ modules)
    }

    class UnitStats {
        +int ActionPoints
        +int Shields
        +int MovementRange
        +int Hull
        +IReadOnlyList~AbilityData~ Abilities
    }

    class ShipController {
        <<MonoBehaviour>>
        +ShipEquipmentHandler Equipment
        +ShipStatsCalculator Calculator
        +UnitStats ApplyLoadout()
    }

    ShipEquipmentHandler "1" *-- "many" EquipmentSlot : contiene
    EquipmentSlot --> ShipModuleSO : referencia
    EquipmentSlot ..> SlotType : filtra por
    ShipStatsCalculator ..> ShipBaseStatsSO : lee base
    ShipStatsCalculator ..> ShipModuleSO : suma modificadores
    ShipStatsCalculator --> UnitStats : produce
    ShipModuleSO --> AbilityDataSO : otorga
    ShipController --> ShipEquipmentHandler
    ShipController --> ShipStatsCalculator
```

---

## 9. Diagrama de Estados — Máquina de Turnos

```mermaid
stateDiagram-v2
    [*] --> Setup
    Setup --> PlayerTurn : Iniciar Partida

    PlayerTurn --> ExecutingAction : Comando validado (PA OK)
    ExecutingAction --> PlayerTurn : Execución completa / Undo
    ExecutingAction --> PlayerTurn : PA insuficiente → rechazado

    PlayerTurn --> EnemyTurn : Jugador termina turno
    EnemyTurn --> Resolution : IA sin comandos pendientes
    Resolution --> PlayerTurn : Aplicar anomalías gravitacionales<br/>+ recargar PA

    PlayerTurn --> Victory : Todos los enemigos destruidos
    PlayerTurn --> Defeat : Nave del jugador destruida
    EnemyTurn --> Victory : Última nave enemiga destruida
    EnemyTurn --> Defeat : Nave del jugador destruida

    Victory --> [*]
    Defeat --> [*]

    note right of PlayerTurn
        El jugador puede intercalar:
        Mover → Atacar → Mover → Escudo
    end note
    note right of ExecutingAction
        Sub-estado asíncrono:
        await Command.ExecuteAsync()
    end note
```

---

## 10. Diagrama de Flujo — Entorno Táctico Interactivo

```mermaid
flowchart TD
    UnitMove["Nave se mueve / termina turno"] --> CheckTile{"¿Tipo de celda?"}

    CheckTile -->|Asteroides| Cover["Cobertura rígida<br/>Bloquea proyectiles en línea recta"]
    CheckTile -->|Nebulosa| Nebula["Oculta la nave<br/>Impide fijación de misiles"]
    CheckTile -->|Anomalía Gravitacional| Gravity["Programar desplazamiento<br/>al final del turno"]
    CheckTile -->|Vacío| Free["Sin efecto"]

    Cover --> LOS["LineOfSightService.HasLineOfSight()"]
    LOS -->|Bloqueado ❌| AttackFail["AttackCommand rechazado"]
    LOS -->|Visible ✅| AttackOK["AttackCommand válido"]
    Nebula --> Targeting["Targeting de misiles deshabilitado"]
    Nebula --> Stealth["Nave no detectada por IA"]
    Gravity --> EndOfTurn["TurnStateMachine: fin de turno"]
    EndOfTurn --> Displace["Desplazar naves según vector gravitacional"]

    AttackOK --> ApplyDmg["Aplicar daño a escudos / casco"]
    AttackFail --> Feedback["Feedback UI"]
    Free --> AttackOK

    classDef hazard fill:#744210,stroke:#f6ad55,color:#fff
    classDef ok fill:#22543d,stroke:#68d391,color:#fff
    classDef fail fill:#742a2a,stroke:#fc8181,color:#fff
    class Cover,Nebula,Gravity hazard
    class AttackOK,ApplyDmg,Stealth,Displace ok
    class AttackFail,Feedback fail
```

---

## Leyenda de Capas

| Capa | Responsabilidad | Depende de | Testeable sin Unity |
| --- | --- | --- | --- |
| **Presentación** (`MonoBehaviour`, UI, Tilemap) | Render, input, animación | Aplicación + Dominio | ❌ No |
| **Aplicación** (Servicios, FSM, Processor) | Orquestar, validar, ejecutar, deshacer | Dominio | ⚠️ Parcial |
| **Dominio** (structs, lógica pura, interfaces) | Reglas de negocio, matemática | *Nada externo* | ✅ Sí (NUnit) |

> **Principio rector:** todas las flechas de dependencia apuntan hacia el **Dominio**. Esto garantiza que la grilla hexagonal, el patrón comando y el cálculo de estadísticas sean independientes del motor gráfico, cumpliendo con SRP, OCP y facilitando los Unit Tests.
