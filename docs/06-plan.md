# 06 — Plan de ejecución hasta la entrega

## 0. Estado al 25/10

| Hecho | Pendiente |
| --- | --- |
| TP1 diseñado (`GDD.md`, `DIAGRAMS.md`) | Commitear el TP1 (sigue sin versionar) |
| Arquitectura declarada en el PDF relevada y trazada | Verificar compilación en el editor |
| Doctrina, arquitectura, tests, prototipos, trazabilidad y defensa documentados | Generar las escenas (`VoidHex → Build Prototype Scenes`) |
| Scaffolding de los 3 prototipos: asmdefs, dominio, aplicación, adaptadores, tests | Completar los comportamientos marcados `☐` en `04-trazabilidad.md` |
| VContainer 1.19.0 resuelto por el Package Manager | Ensayos de defensa y tag `tp2` |
| Lógica pura compilada con `netstandard2.1` sin errores | — |

**Restricción real:** 3 días (25, 26, 27) + 28 de buffer. El riesgo no es el código: es el alcance.

## 1. Orden de trabajo (por riesgo)

| Orden | Bloque | Por qué |
| --- | --- | --- |
| 1 | Verificar compilación en el editor | Un error de asmdef bloquea todo; detectarlo hoy |
| 2 | Generar y probar las 3 escenas | Sin escena no hay demo, y la demo es 2/3 de la nota |
| 3 | Completar los tests `☐` de P1 | Es donde vive la propiedad ISP/Observer que se defiende |
| 4 | Completar los tests `☐` de P3 | Son pocos y acotados |
| 5 | Ensayo de las demos de propiedad | D2, D4 y D6 son las que convencen |
| 6 | README final, matriz al día y tag | Se cierra con la suite verde |

## 2. Calendario

### Sábado 25/10

- [x] Relevar la arquitectura declarada en el PDF (ISP+Observer, pipeline async, VContainer+SO).
- [x] Scaffolding completo: 9 assemblies, contratos, adaptadores y suite inicial.
- [x] Documentación `docs/00..06` alineada con el PDF.
- [ ] **Enfocar Unity** y revisar que no haya errores de compilación.
- [ ] Commit del TP1 + scaffolding + docs.
- [ ] Generar escenas con `VoidHex → Build Prototype Scenes` y abrir las tres.

### Domingo 26/10

- [ ] Completar B1.5, B1.6, B1.9–B1.13 (P1): reactividad, presupuesto, determinismo, LOS, redibujos.
- [ ] Completar B3.5 y B3.8 (P3).
- [ ] **Checkpoint con el docente:** alcance, exclusiones, 1 test por prototipo.
- [ ] Ensayar D1, D2, D3.

### Lunes 27/10

- [ ] Ensayar D4, D5, D6 (OCP, reemplazo, data-driven).
- [ ] Mutation demo ×3 y reversión con `git checkout`.
- [ ] Suite completa desde cero (Edit + Play) y captura de resultados.
- [ ] README con comandos verificados y matriz de trazabilidad sin `☐`.

### Martes 28/10 (buffer)

- [ ] Dos ensayos cronometrados (≤ 20 min).
- [ ] Tag `tp2` y mail antes de las 23:59.
- [ ] Solo bugs bloqueantes.

## 3. Riesgos y mitigación

| Riesgo | Probabilidad | Mitigación |
| --- | --- | --- |
| Error de asmdef o de referencia a VContainer | Media | Verificar hoy en el editor; el paquete ya está resuelto |
| Play Mode inestable | Media | `yield return null`, sin física ni `WaitForSeconds` |
| Tests async colgados | Media | Presentación controlada por `TaskCompletionSource`; sin `Thread.Sleep` |
| Scope creep (arte, IA, undo) | Alta | Anti-objetivos de `docs/00` §3 |
| Documentación desactualizada | Media | Actualizar el mismo día que cambia el código |
| Tag movido o entrega por rama | Baja | Procedimiento de §4 |

## 4. Procedimiento de entrega

```powershell
# 1. Suite verde y todo commiteado
git -C c:\Users\Mati\HexPG status --short

# 2. Tag anotado sobre el commit evaluado
git -C c:\Users\Mati\HexPG tag -a tp2 -m "TP2: 3 prototipos + tests"
git -C c:\Users\Mati\HexPG push origin main --tags

# 3. Enviar el enlace del TAG (no de la rama) a diego.freijo@udelaciudad.edu.ar
#    antes del 28/10 23:59. No mover el tag después.
```

Reentrega: tag `rtp2` (04/11 23:59). Defensa: 04/11, 20 min + 10 de preguntas.

## 5. Checklist final

- [ ] Repositorio clonable; sin `Library/`, `Temp/`, `Logs/`, `UserSettings/`.
- [ ] TP1 incluido (`GDD.md`, `DIAGRAMS.md`).
- [ ] 3 prototipos en carpetas independientes, 1 escena cada uno.
- [ ] Tests verdes en Edit Mode y Play Mode.
- [ ] README con versión de Unity y pasos reproducibles (incluye `Build Prototype Scenes`).
- [ ] Documentación por prototipo: alcance, exclusiones, ajustes, decisiones.
- [ ] Matriz comportamiento → test → defecto completa.
- [ ] Dos ensayos de defensa hechos.
- [ ] Tag `tp2` creado y enviado por mail.
