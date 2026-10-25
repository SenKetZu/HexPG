# 06 — Plan de ejecución hasta la entrega

## 0. Estado al 25/10

| Hecho | Pendiente |
| --- | --- |
| TP1 diseñado (`GDD.md`, `DIAGRAMS.md`) | Commitear el TP1 (hoy está sin versionar) |
| Consigna TP2 analizada y trazada | Crear asmdefs y carpetas de prototipos |
| Doctrina, arquitectura, tests y defensa documentados | Implementar P1, P2 y P3 + tests |
| — | README de entrega y tag `tp2` |

**Restricción real:** 3 días (25, 26, 27) + 28 de buffer. El riesgo no es el código: es el
alcance. Por eso el orden prioriza lo que se defiende.

## 1. Orden de implementación (por riesgo, no por número)

| Orden | Bloque | Por qué |
| --- | --- | --- |
| 1 | P1 dominio + tests | Es el más pequeño y valida el esquema de asmdefs |
| 2 | P2 dominio + tests | Es el más grande; si se atrasa, es el que más duele |
| 3 | P2 Play Mode | Es el único punto frágil (ciclo de vida) |
| 4 | P3 dominio + tests + adaptador SO | Es acotado y data-driven |
| 5 | Escenas de demo | Solo lo necesario para mostrar; sin arte |
| 6 | README, matriz final, tag | Se cierra con la suite verde |

## 2. Calendario

### Sábado 25/10

- [ ] Commit del TP1 (`GDD.md`, `DIAGRAMS.md`) + `docs/` completo.
- [ ] `.gitignore`: agregar `.codebase-memory/` (hoy está sin ignorar).
- [ ] Crear `Assets/Prototypes/P1_HexGrid/**` con asmdefs y compilar en vacío.
- [ ] P1 dominio: `HexCoordinates`, `HexGrid`, `Pathfinder`, `PathResult`, `LineOfSight`.
- [ ] P1 tests B1.1–B1.8 verdes.
- [ ] Escena P1 mínima (gizmos o tiles simples) + demo.

### Domingo 26/10

- [ ] P2 dominio: `TurnState`, `ICommand`, comandos, `TurnSession`, FSM.
- [ ] P2 tests B2.1–B2.7 verdes.
- [ ] **Checkpoint con el docente:** mostrar alcance, exclusiones y 1 test por prototipo
      (la consigna recomienda validar antes de la entrega).
- [ ] P2 Play Mode B2.8 + escena con barra de PA y log.

### Lunes 27/10

- [ ] P3 dominio: `ModuleDefinition`, `Loadout`, `ShipStatsCalculator`, `EquipResult`.
- [ ] P3 tests B3.1–B3.8 verdes (incluye adaptador `ShipModuleAsset`).
- [ ] Escena P3 con slots y panel de stats.
- [ ] README final con comandos verificados.
- [ ] Matriz `04-trazabilidad.md` actualizada con nombres reales de tests.
- [ ] Suite completa desde cero (Edit + Play) y captura de resultados.

### Martes 28/10 (buffer, no desarrollo)

- [ ] Ensayo cronometrado 1 y 2 (`05-defensa.md`).
- [ ] Verificar las 5 demos y el plan B.
- [ ] Tag `tp2` y envío del mail **antes de las 23:59**.
- [ ] Solo bugs bloqueantes. Nada de features nuevas.

## 3. Riesgos y mitigación

| Riesgo | Probabilidad | Mitigación |
| --- | --- | --- |
| Los asmdefs no compilan y bloquean todo | Media | Crearlos **antes** de escribir lógica y compilar en vacío el día 25 |
| P2 Play Mode inestable | Media | `yield return null`, duración 0, sin física ni `WaitForSeconds` |
| Scope creep (arte, menús, IA) | Alta | Anti-objetivos de `00-doctrina.md` §3 |
| Documentación desactualizada | Media | Actualizar el mismo día que cambia el código (regla de `00` §2) |
| Tests que pasan por casualidad | Media | Columna "defecto que detecta" completa; mutation demo |
| Tag movido o entrega por rama | Baja | Procedimiento de §4 y checklist final |

## 4. Procedimiento de entrega

```powershell
# 1. Verificar que todo está commiteado y la suite está verde
git -C c:\Users\Mati\HexPG status --short

# 2. Tag anotado sobre el commit evaluado
git -C c:\Users\Mati\HexPG tag -a tp2 -m "TP2: 3 prototipos + tests"
git -C c:\Users\Mati\HexPG push origin main --tags

# 3. Enviar el enlace del TAG (no de la rama) a diego.freijo@udelaciudad.edu.ar
#    antes del 28/10 23:59. No mover el tag después.
```

Reentrega: tag `rtp2` (04/11 23:59). La defensa es el 04/11, 20 min + 10 de preguntas.

## 5. Checklist final de entrega

- [ ] Repositorio clonable; sin `Library/`, `Temp/`, `Logs/`, `UserSettings/`.
- [ ] TP1 incluido (`GDD.md`, `DIAGRAMS.md`).
- [ ] 3 prototipos en carpetas independientes, 1 escena cada uno.
- [ ] Tests verdes en Edit Mode y Play Mode.
- [ ] README con versión de Unity y pasos reproducibles.
- [ ] Documentación por prototipo: alcance, exclusiones, ajustes vs TP1, decisiones.
- [ ] Matriz comportamiento → test → defecto completa.
- [ ] Dos ensayos de defensa hechos.
- [ ] Tag `tp2` creado y enviado por mail.
