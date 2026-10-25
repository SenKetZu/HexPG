# P1 — Exposición del estado de la grilla (ISP + Observer)

**Problema del TP1:** ¿cómo exponer el mapa (asteroides, ocupación) a IA, comandos y UI sin un
God Object ni *polling* en `Update()`?
**Solución del TP1 implementada:** segregación de interfaces + eventos — `IGridQueryService`
(lectura síncrona) e `IGridStatePublisher` (eventos C# puros).

## Qué demuestra

1. La IA y el pathfinding solo ven `IGridQueryService`: **no pueden mutar** el mapa.
2. La vista solo ve `IGridStatePublisher`: **no puede alterar reglas**, y redibuja únicamente
   cuando hay una mutación real.
3. No hay `Update()` consultando el modelo: cero trabajo por frame.
4. El dominio es C# puro (`noEngineReferences: true`), testeable sin escena.

## Cómo se ejecuta

Escena `Scenes/P1_GridState.unity` → Play. `GridBootstrap` construye un mapa 12×12, bloquea dos
celdas y calcula una ruta; `GridPresenter` registra cada redibujo en la consola.
Los tests de contrato están en `Tests/EditMode/GridContractTests.cs`.

## Nota de alcance

La matemática hexagonal y el A* son el **sustrato** de este prototipo, no su tesis: la tesis es que
el estado se expone con dos contratos segregados y se propaga por eventos.
