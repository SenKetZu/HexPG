# 02 — Doctrina de tests

> La consigna evalúa **elegir qué testear y cómo** (§4.2), no la cantidad de tests.
> Un test que no puede fallar por un motivo concreto no demuestra nada.

## 1. Qué se testea

| Nivel | Qué valida | Modo | Dónde |
| --- | --- | --- | --- |
| **Unitario** | Reglas del dominio, casos límite, invariantes | Edit Mode | `Pn/Tests/EditMode` |
| **Integración** | Adaptador (SO → dominio, MonoBehaviour → dominio) | Edit Mode | `Pn/Tests/EditMode` |
| **Funcional** | El comportamiento dentro del ciclo de vida de Unity | Play Mode | `P2/Tests/PlayMode` |

Los tres aspectos son distintos: un test de integración en Edit Mode no reemplaza a un funcional
en Play Mode, y viceversa. La consigna lo advierte explícitamente.

## 2. Política de Play Mode

Se usa Play Mode **solo donde el comportamiento depende del ciclo de vida de Unity**.

| Prototipo | Play Mode | Justificación |
| --- | --- | --- |
| P1 HexGrid | ❌ | El render es una vista sin reglas; toda la decisión está en el dominio |
| P2 TurnFlow | ✅ | El adaptador MonoBehaviour ejecuta comandos al iniciar y publica eventos; hay que probar que Unity lo invoca |
| P3 Loadout | ❌ | El `ScriptableObject` se puede crear y mapear en Edit Mode; no hay ciclo de vida |

Excluir Play Mode en P1 y P3 es una decisión, no un olvido: si la lógica no vive en Unity,
testearla en Play Mode solo agrega fragilidad.

## 3. Reglas de la suite

1. **Independencia:** ningún test depende del orden ni del estado de otro. Sin `static` mutable.
2. **Repetibilidad:** mismo resultado en cada corrida. Sin `Random` sin semilla, sin `Time` real.
3. **Preparación propia:** cada test crea lo que necesita (`SetUp`) y lo destruye (`TearDown`).
4. **Sin preparación manual:** la suite pasa en un proyecto recién clonado, sin tocar la escena.
5. **Un motivo de falla:** si un test falla, el nombre y el assert dicen qué regla se rompió.
6. **Aserción sobre la regla:** se verifica el resultado observable, no la implementación interna.
7. **Caso límite explícito:** cada regla relevante tiene su caso borde o su exclusión justificada.
8. **Sin `Thread.Sleep`:** en Play Mode se usa `yield return null` / `WaitForFixedUpdate`.

## 4. Las tres preguntas que cada test debe responder

La consigna pide explicar, por prueba o grupo:

1. **¿Qué comportamiento verifica?** → ID de comportamiento (`B2.3`) en `04-trazabilidad.md`.
2. **¿Por qué ese caso es relevante?** → columna "caso" en la tabla de cada prototipo.
3. **¿Qué error de implementación detectaría?** → columna "defecto que detecta".

Si una fila no puede completar la columna 3, el test se elimina o se rediseña.

## 5. Anatomía de un test

```csharp
[Test]
public void Submit_WhenActionPointsInsufficient_KeepsStateUnchanged()
{
    // Arrange: sesión con 1 PA y un comando de costo 2
    var session = TurnSession.CreateForTests(actionPoints: 1);
    var command = new MoveCommand(targetCell: 2, cost: 2);

    // Act
    var result = session.Submit(command);

    // Assert: rechazo con motivo explícito y estado intacto
    Assert.That(result.IsAllowed, Is.False);
    Assert.That(result.Reason, Is.EqualTo(CommandRejection.NotEnoughActionPoints));
    Assert.That(session.State.ActionPoints, Is.EqualTo(1));
    Assert.That(session.State.Cell, Is.EqualTo(0));
}
```

## 6. Trampas conocidas (y cómo se evitan)

| Trampa | Síntoma | Prevención |
| --- | --- | --- |
| Test que solo verifica "no lanza" | Pasa aunque la lógica sea incorrecta | Aserción sobre valor y motivo |
| Test de orden | Pasa solo si corrió otro antes | `SetUp` por test, sin estáticos |
| Play Mode con `yield return new WaitForSeconds` | Lento y frágil | `yield return null` o duración 0 |
| Test acoplado a MonoBehaviour | Obliga a Play Mode para lógica pura | Lógica en dominio, test en Edit Mode |
| Aserción sobre `ToString()` / logs | Cambia por formato | Aserción sobre estado o resultado |
| Test que crea assets y no los borra | Contamina el proyecto | `TearDown` con `Object.DestroyImmediate` |
| Test gigante con 10 aserciones | No se sabe qué falló | Un comportamiento por test |

## 7. Cobertura mínima por prototipo

| Prototipo | Unitarios (Edit) | Integración (Edit) | Funcional (Play) |
| --- | --- | --- | --- |
| P1 | Distancia, vecinos, pathfinding, unreachable, determinismo, LOS, no mutación del mapa | — (el render es una vista sin reglas) | — |
| P2 | AP, validación, undo por snapshot, FSM, recarga de turno | — | Driver ejecuta un comando al iniciar y emite el evento |
| P3 | Tipos de slot, recálculo, reemplazo, quitar, no mutación | SO → `ModuleDefinition` | — |

El detalle de casos y nombres está en `03-prototipos.md` y `04-trazabilidad.md`.
