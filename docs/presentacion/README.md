# Presentación TP2 — Void Hex Tactics

Contenido listo para exponer, generado desde [`docs/07-presentacion.md`](../07-presentacion.md).

## Qué hay acá

| Archivo | Qué es |
| --- | --- |
| `TP2-VoidHex-Tactics.pptx` | Deck de 26 slides, 16:9, con diagramas **nativos** (vectores editables), capturas reales de las tres escenas, el código de cada patrón aplicado, el código de los tests y notas del orador |
| `assets/escena-p1.png` | Captura real de `P1_GridState` en Play (4 eventos, 144 celdas, costo 20) |
| `assets/escena-p2.png` | Captura real de `P2_CommandPipeline` en Play (PA 6 → 1, hull 6 → 3) |
| `assets/escena-p3.png` | Captura real de `P3_Loadout` en Play (PA 6 · escudos 5 · movimiento 4) |
| `assets/diagrama-p1-isp-observer.png` | P1: dos contratos segregados sobre un único `GridModel` |
| `assets/diagrama-p2-pipeline.png` | P2: `ICommand` → validar → ejecutar → notificar → animar |
| `assets/diagrama-p2-ocp.png` | P2: comandos intercambiables sin tocar el pipeline |
| `assets/diagrama-p3-di.png` | P3: `LifetimeScope` registrando e inyectando dependencias |

Los PNG son para usar en Google Slides, Keynote o cualquier editor que no abra el `.pptx` con fidelidad.

Las tres capturas `escena-*` salen del editor (`capture_game_view`), no son mockups. Si cambia una
escena, se vuelven a tomar y se pisan esos archivos: `build_deck.py` las lee de ahí.

## Estructura del deck

| Slides | Bloque | Tiempo |
| --- | --- | --- |
| 1–3 | Marco: qué entrego y qué se puede abrir | 2:30 |
| 4–8 | **P1** ISP + Observer | 5:00 |
| 9–13 | **P2** Pipeline async | 5:00 |
| 14–18 | **P3** DI + Data-driven | 4:30 |
| 19–22 | Cómo se testea, trazabilidad, exclusiones y ajustes | 2:30 |
| 23 | Cierre | 0:30 |
| 24–26 | **Respaldo** (no se muestran salvo que pregunten) | — |

Slides de **código de patrón**: 6 (ISP + Observer), 11 (Command + cola), 16 (DI + DIP),
17 (Flyweight + inmutable). Slides de **código de test**: 7, 12 y 18. La 19 explica **cómo se
testea cada patrón**, que es la pregunta que suele venir después.

Cada slide tiene **notas del orador** con lo que hay que decir y cuánto tiempo tomar.

## Cómo regenerar

```powershell
python tools/ppt/build_deck.py      # genera el pptx y los PNG de diagrama
python tools/ppt/verify_deck.py     # valida límites y desbordes de texto
python tools/ppt/preview_deck.py    # PPTX -> PNG por slide, para revisar a ojo
```

Requisitos: `python-pptx` y `matplotlib`.

Para que las diapositivas usen una tipografía más portable (Google Slides, Mac):

```powershell
$env:DECK_FONT="Arial"; $env:DECK_MONO="Consolas"; python tools/ppt/build_deck.py
```

## Cómo abrirlo

**Local:** doble clic en el `.pptx` (PowerPoint, Keynote, LibreOffice Impress). Las notas del orador
están en el panel inferior o en *Vista → Página de notas*.

**Google Slides (la forma simple y recomendada):**

1. Subir `TP2-VoidHex-Tactics.pptx` a Google Drive.
2. Clic derecho → *Abrir con → Google Slides*. Se convierte solo.
3. Revisar que los diagramas mantengan posición (son formas nativas, se convierten bien).
4. *Archivo → Guardar como presentación de Google* si querés editarlo ahí.

> Segoe UI no existe en Google Slides: se sustituye por una fuente del sistema. Si vas a presentar
> desde Google, regenerá con `DECK_FONT=Arial` para que no se muevan las líneas.

**Google Slides por script:** es posible con Apps Script (`SlidesApp`) o con la Slides API, pero
requiere un proyecto de Google Cloud y credenciales OAuth. Para 20 slides no conviene: el camino
`.pptx → subir → abrir con Slides` hace lo mismo en dos clics y conserva los diagramas.

## Antes de presentar

- [ ] Correr `python tools/ppt/verify_deck.py`: debe dar `problemas: 0`.
- [x] Número de tests de las slides 14 y 17: **81** (80 Edit Mode + 1 Play Mode), verificado el
      04/11 en [`../evidencia/`](../evidencia).
- [ ] Copiar la captura del Test Runner como respaldo de esos números.
- [ ] Ensayar dos veces con cronómetro (≤ 20 min).
- [ ] Probar el archivo en la máquina donde vas a exponer (el docente ofrece la suya).
