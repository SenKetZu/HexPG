# P3 — Servicios inyectados y módulos data-driven

**Problema del TP1:** ¿cómo conectar servicios sin singletons estáticos (`GameManager.Instance`) y
definir módulos MK I–III?
**Solución del TP1 implementada:** inyección de dependencias con **VContainer** + módulos como
assets **ScriptableObject** (Flyweight).

## Qué demuestra

1. **DIP real:** `EquipmentService` recibe `IModuleCatalog` por constructor. En los tests se
   inyecta un catálogo falso, sin tocar la escena ni el motor.
2. **Sin acceso global:** no hay `Instance`, ni Service Locator, ni búsquedas en runtime.
3. **Flyweight:** todos los módulos son assets compartidos; el dominio recibe copias de datos.
4. **Data-driven:** agregar un MK III es crear un asset; no se recompila código.
5. **Composición pura:** las estadísticas se calculan sumando bonificaciones sin mutar la base.

## Cómo se ejecuta

Escena `Scenes/P3_Loadout.unity` → Play. `EquipmentLifetimeScope` registra el catálogo, la config
de la nave y el servicio; `LoadoutDemo` recibe `IEquipmentService` por `[Inject]` y equipa módulos.

## Decisión respecto del TP1

Un slot por tipo hace que la incompatibilidad de tipo sea **imposible por construcción**: no se
puede equipar un módulo en un slot equivocado porque el slot lo determina el propio módulo. El
único caso inválido es un `SlotType` fuera de rango, que se rechaza explícitamente. Es una
simplificación deliberada: elimina un estado inválido en lugar de testearlo.
