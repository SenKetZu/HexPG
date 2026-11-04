# Evidencia de la suite

Salida real de las corridas del **04/11/2026** (Unity 6000.5.9f1, editor abierto).

| Archivo | Qué es |
| --- | --- |
| `tests-editmode.json` | 80 tests de Edit Mode: 80 pasan, 0 fallan |
| `tests-playmode.json` | 1 test de Play Mode: pasa |
| `p1-grid-hud.png` | Captura de la escena P1 en Play: **4 eventos**, 144 celdas, ruta de **21 celdas / costo 20** |
| `p3-loadout-stats.png` | Captura de la escena P3 en Play: **PA 6 · escudos 5 · movimiento 4** |

Reparto de Edit Mode: P1 5 · P2 7 · P3 9 · GameLoop 59.

## Cómo se regenera

Con el editor abierto (el pipeline responde en `127.0.0.1:7800`):

```powershell
unity command --project-path . run_tests --mode EditMode
unity command --project-path . run_tests --mode PlayMode --async_tests
unity command --project-path . test_status
```

`normalize.py` convierte la salida cruda del CLI (`Command / Success / Result / Parameters`)
a JSON legible. La corrida cruda queda en `Logs/` (ignorado por git).

## Nota

`-batchmode -runTests` no arranca el runner en este entorno: devuelve 0 resultados sin error.
Hay que usar el pipeline con el editor abierto.
