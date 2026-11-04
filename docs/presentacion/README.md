# Presentación TP2 — Void Hex Tactics

Contenido listo para exponer, generado desde [`docs/07-presentacion.md`](../07-presentacion.md).

## Qué hay acá

| Archivo | Qué es |
| --- | --- |
| `TP2-VoidHex-Tactics.pptx` | Deck de 20 slides, 16:9, con diagramas **nativos** (vectores editables), el código real de un test por prototipo y notas del orador |
| `assets/diagrama-p1-isp-observer.png` | P1: dos contratos segregados sobre un único `GridModel` |
| `assets/diagrama-p2-pipeline.png` | P2: `ICommand` → validar → ejecutar → notificar → animar |
| `assets/diagrama-p2-ocp.png` | P2: comandos intercambiables sin tocar el pipeline |
| `assets/diagrama-p3-di.png` | P3: `LifetimeScope` registrando e inyectando dependencias |

Los PNG son para usar en Google Slides, Keynote o cualquier editor que no abra el `.pptx` con fidelidad.

## Estructura del deck

| Slides | Bloque | Tiempo |
| --- | --- | --- |
| 1–2 | Marco: qué entrego | 2:00 |
| 3–6 | **P1** ISP + Observer | 5:00 |
| 7–10 | **P2** Pipeline async | 5:30 |
| 11–13 | **P3** DI + Data-driven | 4:00 |
| 14–16 | Tests, exclusiones y ajustes | 2:30 |
| 17 | Cierre | 1:00 |
| 18–20 | **Respaldo** (no se muestran salvo que pregunten) | — |

Las slides de evidencia (5, 9 y 13) muestran el código del test que sostiene el número, copiado
literalmente de `Assets/Prototypes/*/Tests`.

Cada slide tiene **notas del orador** con lo que hay que decir y cuánto tiempo tomar.

## Cómo regenerar

```powershell
python tools/ppt/build_deck.py      # genera el pptx y los PNG
python tools/ppt/verify_deck.py     # valida límites y desbordes de texto
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
- [ ] Reemplazar el número de tests de la slide 14 y la 17 por el real del Test Runner.
- [ ] Copiar la captura del Test Runner como respaldo de esos números.
- [ ] Ensayar dos veces con cronómetro (≤ 20 min).
- [ ] Probar el archivo en la máquina donde vas a exponer (el docente ofrece la suya).
