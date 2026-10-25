# 00 — Doctrina y pilares de desarrollo

> Documento normativo del TP2. Si una decisión contradice este documento, o se cambia el
> documento o se cambia la decisión. No se improvisa en el código.

## 0. De dónde sale esta doctrina

| Fuente | Qué aporta |
| --- | --- |
| Consigna TP2 §4.1–§4.5 | El alcance, los entregables y los criterios de evaluación |
| Consigna TP2 §3 | 1/3 entrega, **2/3 defensa oral**; mínimos por prototipo |
| Devolución TP1 (nota 9) | "Los problemas y soluciones están bien… en las slides no dejás las cosas muy claras" |
| Charla previa a la entrega TP1 | El diseño acordado es un encargo; los ajustes se documentan, no se esconden |
| `GDD.md` + `DIAGRAMS.md` (TP1) | El diseño de referencia que hay que implementar |

La devolución dice algo incómodo y útil: **el trabajo técnico estuvo bien y la comunicación no**.
El TP2 no se arregla escribiendo más código: se arregla haciendo que cada decisión sea
**explícita, verificable y contable en 20 minutos**.

## 1. Los siete pilares

### Pilar 1 — Un prototipo = un problema del TP1 = una carpeta = una escena

No se entrega un juego: se entregan 3 pruebas de concepto técnicamente pulidas (§4.1).
Cada una vive en `Assets/Prototypes/Pn_*/`, con su propia escena y su propia documentación.

- **Verificación:** el árbol de carpetas y la tabla de estado del `README.md`.
- **Prohibido:** prototipos que compartan escena, estado estático o "arreglos" cruzados.

### Pilar 2 — El dominio no conoce Unity

Regla de dependencia: `Presentación → Aplicación → Dominio`. El dominio es C# puro y se testea
sin abrir una escena.

- **Verificación mecánica:** el `.asmdef` del dominio declara `"noEngineReferences": true`.
  Si alguien escribe `using UnityEngine;` en el dominio, **no compila**. La regla no depende de
  la disciplina, la impone el compilador.
- **Origen:** Solución C del TP1 (coordenadas cúbicas en C# puro) llevada a todo el proyecto.

### Pilar 3 — El test es el contrato ejecutable

Cada comportamiento comprometido tiene al menos un test que lo valida, y cada test declara
**qué defecto detectaría** (§4.2). Un test que no puede fallar por un motivo concreto no aporta.

- **Verificación:** matriz de trazabilidad (`04-trazabilidad.md`), columna "defecto que detecta".
- **Prohibido:** tests que solo verifican que un método no lanza; tests acoplados al orden.

### Pilar 4 — Las propiedades de diseño se demuestran, no se afirman

Si la solución promete extensibilidad, se agrega una variante en vivo. Si promete desacoplamiento,
se sustituye una dependencia. Si promete control de estado, se muestra quién puede mutarlo (§4.3).

- **Verificación:** una demo concreta por prototipo, ensayada, con test o paso a paso.
- **Formato:** "Promesa → evidencia → archivo/línea".

### Pilar 5 — Contratos explícitos

Toda operación relevante declara: qué entradas acepta, qué garantiza, qué estados son válidos,
quién puede modificarlos y cómo representa la ausencia de valor (§4.4).

- **Verificación:** sección "Contratos" de cada prototipo en `03-prototipos.md`.
- **Regla:** los estados inválidos de juego son **resultados** (con motivo), no excepciones.
  Las excepciones quedan para precondiciones rotas (error del programador).

### Pilar 6 — El alcance se declara y se justifica

Excluir un caso es legítimo **si se justifica**; no es legítimo excluirlo porque la implementación
no lo resuelve (§4.1). Cada prototipo lista supuestos, exclusiones y cómo se extendería.

- **Verificación:** tabla de exclusiones con columna "justificación" y "cómo se extiende".

### Pilar 7 — La entrega se explica sola

El 66 % de la nota es oral. La documentación es el guion de la defensa, no un trámite (§4.5).
Cada prototipo debe poder contarse en 5 minutos: problema → promesa → evidencia → decisión → exclusión.

- **Verificación:** `05-defensa.md` y un ensayo cronometrado antes del 04/11.

## 2. Reglas no negociables

- [ ] No se entrega un juego completo; se entregan 3 pruebas de concepto.
- [ ] Cada prototipo: carpeta propia + una sola escena que ejecuta la prueba.
- [ ] Todo comportamiento comprometido está implementado **y** testeado.
- [ ] La suite completa pasa en verde desde cero (Edit Mode y Play Mode).
- [ ] Los tests no dependen del orden, ni de preparación manual no documentada.
- [ ] Los tests limpian lo que crean (GameObjects, assets temporales, archivos).
- [ ] Play Mode **solo** donde importa el ciclo de vida de Unity (en este TP: P2).
- [ ] El dominio no referencia `UnityEngine` (impuesto por asmdef).
- [ ] Ningún cambio sustancial respecto del TP1 sin validar con el docente y documentarlo.
- [ ] `README.md` explica cómo abrir, ejecutar y testear; indica Unity **6000.5.9f1**.
- [ ] El TP1 (`GDD.md`, `DIAGRAMS.md`) queda versionado en el repositorio como referencia.
- [ ] Se entrega por **tag** (`tp2` / `rtp2`), no por rama; el tag no se mueve.
- [ ] La documentación se escribe en presente y se actualiza el mismo día que cambia el código.
- [ ] Nada de patrones decorativos: cada patrón responde a un problema del TP1.

## 3. Anti-objetivos (lo que NO vamos a hacer)

| Anti-objetivo | Por qué |
| --- | --- |
| Arte, animaciones, VFX, audio | No demuestran ninguna propiedad de diseño (§4.1) |
| Menús, guardado, progresión | Fuera del alcance de los 3 problemas técnicos |
| Un test por método | Se testean comportamientos y casos límite, no cobertura |
| `Option<T>` casero, DI container, event bus global | Sobre-ingeniería para 3 días; se explica por qué no |
| DOTS/ECS, multihilo, optimización prematura | El problema del TP1 no es performance extrema |
| Mover el tag después de enviarlo | La consigna lo prohíbe explícitamente |

## 4. Definición de terminado (por prototipo)

Un prototipo está terminado cuando:

1. Los comportamientos de `03-prototipos.md` funcionan en la escena.
2. Cada comportamiento tiene test(es) que pasan y que fallan si se rompe la regla.
3. Los casos límite declarados están cubiertos o explícitamente excluidos con justificación.
4. La propiedad de diseño tiene una demostración ensayada (test o paso a paso).
5. Su sección de documentación está completa: alcance, exclusiones, ajustes vs TP1, decisiones.
6. Se puede explicar en 5 minutos sin leer el código.

## 5. Convenciones de lenguaje

- C# 9 / .NET Standard 2.1 (Unity 6000.5.9f1): **no** `record struct`, **no** `required`,
  **no** primary constructors de clase.
- Nombres: `TryX` (intento con `out`), `CanX` (consulta), `XResult` (resultado con motivo),
  `XState` (estado), `XService` (cálculo sin estado propio).
- Un tipo público por archivo. Clases de dominio `sealed` por defecto.
- Structs de valor `readonly`; colecciones expuestas como `IReadOnlyList<T>`.
- `null` no representa ausencia lógica: se usa `TryGet…(out …)` o un resultado explícito.
  `null` solo aparece en campos serializados de Unity y se valida en el borde.
