# 05 — Defensa oral (2/3 de la nota)

## 1. Diagnóstico de la devolución del TP1

> "Los problemas y soluciones están bien… **en las slides no dejás las cosas muy claras**.
> Ayudó mucho tu defensa oral."

La nota 9 confirma la hipótesis: **el contenido técnico estaba aprobado y la presentación no
transmitía**. En TP2 eso pesa el doble (2/3 de la nota). Por eso cada slide debe sostener **una
sola afirmación con una evidencia**.

## 2. Reglas de slides (no negociables)

1. **Título = afirmación**, no etiqueta. ❌ "P1: HexGrid" → ✅ "El pathfinding corre sin abrir Unity".
2. **Una evidencia visible por slide:** nombre del test en verde, captura del Test Runner, o una
   flecha del diagrama. Nunca "confiá en mí".
3. **Cero muros de código.** Máximo 8 líneas de código, con la línea clave resaltada.
4. **Un diagrama por bloque**, no cinco. Si el diagrama necesita explicación, está mal hecho.
5. **Números concretos:** "8 de 8 tests verdes", "3 comandos, 5 PA, 0 deriva", "4 slots, 3 MK".
6. **La exclusión va en la slide**, no solo en la doc: demuestra criterio de alcance.
7. **Ensayo cronometrado 2 veces.** Si sobra tiempo, se recorta; si falta, se pierde la nota.

## 3. Guion de 20 minutos

| Tiempo | Bloque | Contenido | Evidencia |
| --- | --- | --- | --- |
| 0:00–2:00 | Marco | 3 prototipos = 3 problemas del TP1. No es un juego: es arquitectura verificable. Regla de capas impuesta por asmdef | Diagrama de capas (1 slide) |
| 2:00–7:00 | **P1** | Problema (grilla + costo variable) → decisión (cúbicas, dominio puro) → demo de pathfinding con obstáculo → test de costo mínimo → exclusión (3D) | Escena P1 + 2 tests |
| 7:00–13:00 | **P2** | Problema (acciones intercaladas) → decisión (FSM + comando, estado inmutable) → demo mover→atacar→mover→undo → test de rechazo sin efecto → Play Mode | Escena P2 + Test Runner |
| 13:00–17:00 | **P3** | Problema (módulos y stats) → decisión (datos + composición, SO como adaptador) → demo equipar MK I → MK II → test de idempotencia → agregar MK III sin código | Escena P3 + test nuevo |
| 17:00–19:00 | Cierre | Matriz de trazabilidad: cada comportamiento tiene test y defecto declarado. Ajustes vs TP1 | `04-trazabilidad.md` |
| 19:00–20:00 | Cierre | Qué aprendí al implementar (2 frases) y qué dejaría fuera | — |

**Regla de oro:** cada bloque sigue el mismo orden → *problema → promesa → evidencia → decisión →
exclusión*. Repetir la estructura hace la exposición predecible y fácil de seguir.

## 4. Demos en vivo (ensayadas)

| # | Demo | Qué se muestra | Riesgo | Plan B |
| --- | --- | --- | --- | --- |
| D1 | P1: click origen/destino con obstáculo | Ruta óptima y rechazo por presupuesto | Click impreciso | Captura/GIF |
| D2 | P2: Mover → Atacar → Mover → Deshacer | PA exactos y estado restaurado | UI chica | Log en consola |
| D3 | P3: equipar MK I → MK II → quitar | Stats recalculadas, sin doble conteo | Asset mal asignado | Test en Test Runner |
| D4 | Extensibilidad | Agregar `ShieldCommand` / MK III en vivo | Tipeo en vivo | Rama preparada y ya testeada |
| D5 | Mutation demo (si preguntan) | Romper 1 línea → test rojo | Nervios | Mostrar diff y revertir |

**Mutation demo preparada** (demuestra que los tests detectan defectos reales):

- P1: `Distance` dividiendo por 3 → falla B1.1.
- P2: consumir PA antes de validar → falla B2.3.
- P3: sumar el módulo reemplazado → falla B3.3.

## 5. Banco de preguntas

| Pregunta | Respuesta corta |
| --- | --- |
| ¿Por qué no un juego completo? | La consigna pide 3 pruebas de concepto pulidas (§4.1); el juego agrega contenido que no demuestra ninguna propiedad de diseño |
| ¿Cómo garantizás que el dominio no usa Unity? | `noEngineReferences: true` en el asmdef: si aparece `using UnityEngine`, no compila |
| ¿Por qué no usás `null` para "sin módulo"? | `TryGetModule(out …)` hace explícita la ausencia y el compilador obliga a manejarla; `null` se olvida y explota lejos del origen |
| ¿Cuándo usás excepciones? | Solo precondiciones rotas (mapa `null`): error del programador. Un estado inválido de juego es un resultado con motivo |
| ¿Por qué el `ScriptableObject` no está en el dominio? | Porque arrastra `UnityEngine`; el SO es un adaptador de datos y el dominio recibe `ModuleDefinition` (ajuste documentado vs TP1) |
| ¿Por qué comandos sincrónicos y no `async`? | El dominio no espera frames; la animación es Presentación. Un `Task` en el dominio acopla la regla al motor y hace los tests frágiles |
| ¿Por qué Undo por snapshot? | La aritmética de devolución deriva; el snapshot restaura el estado exacto y se verifica comparando todo el estado |
| ¿Por qué Play Mode solo en P2? | Porque ahí importa que Unity invoque al adaptador y emita el evento; en P1/P3 la regla no vive en el motor |
| ¿Cómo agrego una variante? | P2: nueva clase `ICommand` (sin tocar FSM ni sesión). P3: nuevo `ModuleDefinition`/asset (sin tocar código). P1: nuevo `ICellCostProvider` |
| ¿Qué test detectaría un bug real? | (Mutation demo) Rompo una línea concreta y muestro el test que se pone rojo |
| ¿Los tests son suficientes? | La matriz cubre cada comportamiento y cada caso límite declarado; las exclusiones están justificadas, no escondidas |
| ¿Por qué no ECS/DOTS/DI container/`Option<T>`? | Sobre-ingeniería para 3 días y 3 problemas acotados; cada patrón debe responder a un problema del TP1 |
| ¿Qué pasa con mapas enormes? | Fuera de alcance declarado (≤ 12×12); se extiende cambiando la cola de prioridad, sin tocar el contrato |
| ¿Quién puede mutar el estado? | En P2 solo `TurnSession`; en P3 el `Loadout` es inmutable y `TryEquip` devuelve uno nuevo |
| ¿Qué cambió respecto del TP1? | 6 ajustes en `01-arquitectura.md` §5; ninguno cambia qué resuelve cada prototipo, solo cómo se respeta la regla de dependencia |

## 6. Errores que ya cometimos y no se repiten

| Error TP1 | Corrección TP2 |
| --- | --- |
| Slides que no se entendían solas | Título-afirmación + evidencia visible en cada slide |
| Explicar de memoria | Todo apunta a un archivo, test o línea concreta |
| Decisiones implícitas | Contratos escritos y ADR cortos (`01` §5, `03`) |
| Alcance difuso | Tabla de exclusiones justificadas por prototipo |
| Depender del oral para aclarar | La documentación se entiende sin vos |

## 7. Checklist previo a la defensa

- [ ] Dos ensayos cronometrados (≤ 20 min).
- [ ] Las 3 escenas abren sin errores en consola.
- [ ] Test Runner verde en Edit Mode y Play Mode, desde cero.
- [ ] Las 5 demos probadas 2 veces, con plan B.
- [ ] Mutation demo ensayada y revertida con `git checkout`.
- [ ] Banco de preguntas leído en voz alta.
- [ ] Slides sin código extenso ni diagramas ilegibles.
