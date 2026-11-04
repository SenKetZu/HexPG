# 05 — Defensa oral (2/3 de la nota)

## 1. Diagnóstico de la devolución del TP1

> "Los problemas y soluciones están bien… **en las slides no dejás las cosas muy claras**.
> Ayudó mucho tu defensa oral."

Nota 9: el contenido técnico estaba aprobado y la presentación no transmitía. En TP2 eso pesa el
doble (2/3). Cada slide debe sostener **una sola afirmación con una evidencia**.

## 2. Reglas de slides (no negociables)

1. **Título = afirmación.** ❌ "P1: GridState" → ✅ "La vista no puede tocar el mapa: no compila".
2. **Una evidencia visible por slide:** nombre del test en verde, captura del Test Runner o una
   flecha del diagrama. Nunca "confiá en mí".
3. **El código que se muestra es el test real y completo** (≤ 20 líneas), no un fragmento inventado.
   Un test por prototipo, con sus aserciones y su mensaje de fallo.
4. **Un diagrama por bloque.** Si necesita explicación, está mal hecho.
5. **Números concretos:** "13 tests verdes", "3 PA gastados, 0 de deriva", "4 slots, 3 MK".
6. **La exclusión va en la slide**, no solo en la doc: demuestra criterio de alcance.
7. **Ensayo cronometrado 2 veces.**

## 3. Guion de 20 minutos

Cada bloque repite el mismo orden: **problema declarado → promesa → evidencia → decisión → exclusión**.

| Tiempo | Bloque | Contenido | Evidencia |
| --- | --- | --- | --- |
| 0:00–2:30 | Marco | Tres prototipos = los tres desafíos del TP1; las tres escenas reales | Tabla desafío → solución → prototipo + capturas |
| 2:30–7:30 | **P1 ISP + Observer** | Problema (God Object / polling) → dos contratos segregados → **código de las tres interfaces y del evento** → demo: mutar una celda (1 redibujo), reescribirla igual (0 redibujos) → test de reflexión → exclusión (event bus) | Slide de código + escena P1 + 2 tests |
| 7:30–12:30 | **P2 Pipeline async** | Problema (mover→atacar→mover) → **`ICommand` y el núcleo del procesador con la cola** → demo: el estado ya cambió mientras la animación está pendiente → `ShieldCommand` nuevo sin tocar el pipeline | Slide de código + escena P2 + Test Runner |
| 12:30–17:00 | **P3 DI + Data-driven** | Problema (GameManager.Instance / Service Locator) → **`LifetimeScope`, `[Inject]`, constructor con la interfaz** → **Flyweight: asset → `ModuleDefinition`, loadout inmutable** → demo: MK I → MK II, quitar, crear un MK III como asset | Slides de código + escena P3 + test |
| 17:00–19:00 | **Cómo se testea** | Tabla patrón → propiedad verificada → test, y por qué el único Play Mode es el adaptador de P2 | Slide 19 + `04-trazabilidad.md` |
| 19:00–20:00 | Cierre | Lo que quedó afuera con su ruta de extensión; qué aprendí al implementar | Slides 21–23 |

## 4. Demos en vivo (ensayadas)

| # | Demo | Qué se muestra | Riesgo | Plan B |
| --- | --- | --- | --- | --- |
| D1 | P1: bloquear celda y reescribirla igual | 1 evento vs 0 eventos; sin `Update()` | Log poco visible | Captura de consola |
| D2 | P1: `using UnityEngine;` en el dominio | El proyecto **no compila** ⇒ la regla de capas es real | Romper el proyecto en vivo | Mostrar el `.asmdef` y el error ya capturado |
| D3 | P2: comando aceptado con animación pendiente | `State` ya mutado; `Phase = Executing` | Timing | Test `Submit_AppliesLogicBeforeTheAnimationCompletes` |
| D4 | P2: `ShieldCommand` agregado en vivo | Pipeline y fases intactos (OCP) | Tipeo | Rama preparada y testeada |
| D5 | P3: equipar MK I → MK II → quitar | Stats exactas, sin doble conteo | Asset mal asignado | Test `TryEquip_ReplacingModule_DoesNotDoubleCount` |
| D6 | P3: crear un MK III como asset | Sin recompilar código (data-driven) | Inspector | Test `ShipModuleAsset_MapsToDefinition_AllFields` |

**Mutation demo preparada** (demuestra que los tests detectan defectos reales):

- P1: emitir evento aunque el valor no cambie → falla `SetCell_WithSameValue_DoesNotRaiseChange`.
- P2: esperar la animación antes de mutar el estado → falla `Submit_AppliesLogicBeforeTheAnimationCompletes`.
- P3: sumar el módulo anterior y el nuevo → falla `TryEquip_ReplacingModule_DoesNotDoubleCount`.

## 5. Banco de preguntas

| Pregunta | Respuesta corta |
| --- | --- |
| ¿Por qué no un juego completo? | El alcance declarado son 3 pruebas de concepto pulidas; agregar contenido no demuestra ninguna propiedad de diseño |
| ¿Cómo garantizás que el dominio no usa Unity? | `noEngineReferences: true`: si aparece `using UnityEngine`, no compila (demo D2) |
| ¿Qué gana ISP acá? | La IA recibe un contrato sin mutaciones y la vista uno sin reglas; ningún consumidor puede hacer lo del otro |
| ¿Por qué no un Event Bus global? | Dependencias ocultas, difícil de depurar y no resuelve las lecturas síncronas de la IA (descartado en el TP1) |
| ¿Cómo evitás polling en `Update()`? | La vista no tiene `Update`; se suscribe a `Changed` y redibuja solo ante mutación real |
| ¿Qué pasa si el evento se emite sin cambio real? | Está prohibido por contrato (`Set*` devuelve `false` y no notifica) y cubierto por test |
| ¿Por qué comandos inmutables? | Permiten validar sin efectos, serializar la cola y razonar sobre el antes/después |
| ¿Por qué `Task` y no UniTask? | El PDF declara "UniTask / Task"; `Task` no agrega dependencias de red y la sustitución es una sola interfaz |
| ¿La animación no condiciona la regla? | B2.3: al volver del `Submit`, el estado ya cambió y el `Task` sigue pendiente |
| ¿Cómo evitás que dos comandos se intercalen? | Cola con `SemaphoreSlim`: el segundo espera a que termine la animación del primero |
| ¿Por qué no una FSM con un estado por acción? | El PDF la descarta: explota en combinaciones. Solo hay 3 fases |
| ¿Por qué VContainer y no un Singleton? | El Singleton oculta dependencias y bloquea tests; VContainer inyecta por constructor y permite dobles |
| ¿Por qué no Service Locator? | Mantiene dependencias ocultas dentro de las clases; es el mismo problema con otro nombre |
| ¿Cómo probás DIP? | `EquipmentService` recibe `IModuleCatalog`; el test inyecta `FakeCatalog` y cuenta las consultas |
| ¿Cómo testean un patrón? | No se testea el patrón: se testea la propiedad que promete. ISP por reflexión, Observer con contador de eventos, Command con el `Task` pendiente, DI con un doble, Flyweight comparando los campos |
| ¿Qué es el Flyweight acá? | Un asset por módulo, compartido por todas las naves; el dominio solo copia datos |
| ¿Por qué el SO no está en el dominio? | Arrastra `UnityEngine`; es un adaptador. El dominio recibe `ModuleDefinition` |
| ¿Por qué un slot por tipo? | Hace imposible el estado inválido "módulo en slot incompatible"; se elimina en vez de testearse |
| ¿Cuándo usás excepciones? | Solo precondiciones rotas (catálogo `null`). Un estado inválido de juego es un resultado con motivo |
| ¿Por qué no `null` para ausencia? | `TryGetModule(out …)` obliga a manejarla; `null` se olvida y explota lejos del origen |
| ¿Los tests son suficientes? | La matriz cubre cada comportamiento y cada caso límite declarado; las exclusiones están justificadas |
| ¿Qué cambió respecto del TP1? | 6 ajustes en `docs/01-arquitectura.md` §5; ninguno cambia qué resuelve cada prototipo |
| ¿Qué test detectaría un bug real? | Mutation demo: rompo una línea y muestro el test que se pone rojo |

## 6. Errores del TP1 que no se repiten

| Error TP1 | Corrección TP2 |
| --- | --- |
| Slides que no se entendían solas | Título-afirmación + evidencia visible |
| Explicar de memoria | Todo apunta a un archivo, test o línea |
| Decisiones implícitas | Contratos escritos y ajustes documentados |
| Alcance difuso | Tabla de exclusiones justificadas por prototipo |
| Depender del oral para aclarar | La documentación se entiende sin vos |

## 7. Checklist previo a la defensa

- [ ] Dos ensayos cronometrados (≤ 20 min).
- [ ] Las 3 escenas abren sin errores en consola.
- [ ] Test Runner verde en Edit Mode y Play Mode, desde cero.
- [ ] Las 6 demos probadas dos veces, con plan B.
- [ ] Mutation demo ensayada y revertida con `git checkout`.
- [ ] Banco de preguntas leído en voz alta.
- [ ] Slides sin código extenso ni diagramas ilegibles.
