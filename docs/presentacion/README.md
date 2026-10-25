# Presentación TP2 — Void Hex Tactics

Contenido listo para exponer, generado desde [`docs/07-presentacion.md`](../07-presentacion.md).

## Qué hay acá

| Archivo | Qué es |
| --- | --- |
| `TP2-VoidHex-Tactics.pptx` | Deck de 21 slides, 16:9, con diagramas **nativos** (vectores editables) y notas del orador |
| `assets/diagrama-capas.png` | Capas Presentación → Aplicación → Dominio |
| `assets/diagrama-p1-isp-observer.png` | P1: dos contratos segregados sobre un único `GridModel` |
| `assets/diagrama-p2-pipeline.png` | P2: `ICommand` → validar → ejecutar → notificar → animar |
| `assets/diagrama-p2-ocp.png` | P2: comandos intercambiables sin tocar el pipeline |
| `assets/diagrama-p3-di.png` | P3: `LifetimeScope` registrando e inyectando dependencias |

Los PNG son para usar en Google Slides, Keynote o cualquier editor que no abra el `.pptx` con fidelidad.

## Estructura del deck

| Slides | Bloque | Tiempo |
| --- | --- | --- |
| 1–3 | Marco: qué entrego y cómo se sostiene | 2:00 |
| 4–7 | **P1** ISP + Observer | 5:00 |
| 8–11 | **P2** Pipeline async | 5:30 |
| 12–14 | **P3** DI + Data-driven | 4:00 |
| 15–17 | Tests, exclusiones y ajustes vs TP1 | 2:30 |
| 18 | Cierre | 1:00 |
| 19–21 | **Respaldo** (no se muestran salvo que pregunten) | — |

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
requiere un proyecto de Google Cloud y credenciales OAuth. Para 21 slides no conviene: el camino
`.pptx → subir → abrir con Slides` hace lo mismo en dos clics y conserva los diagramas.

## Antes de presentar

- [ ] Correr `python tools/ppt/verify_deck.py`: debe dar `problemas: 0`.
- [ ] Reemplazar el número de tests de la slide 15 y la 18 por el real del Test Runner.
- [ ] Copiar la captura del Test Runner como respaldo de esos números.
- [ ] Ensayar dos veces con cronómetro (≤ 20 min).
- [ ] Probar el archivo en la máquina donde vas a exponer (el docente ofrece la suya).
