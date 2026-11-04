"""
Genera la presentación del TP2 (Void Hex Tactics) a partir de docs/07-presentacion.md.

Salidas
-------
docs/presentacion/TP2-VoidHex-Tactics.pptx     -> deck editable, 16:9, diagramas nativos
docs/presentacion/assets/*.png                 -> mismos diagramas como imagen (para Google Slides)

Uso
---
    python tools/ppt/build_deck.py
"""

from __future__ import annotations

import os
from pathlib import Path

from pptx import Presentation
from pptx.dml.color import RGBColor
from pptx.enum.shapes import MSO_CONNECTOR, MSO_SHAPE
from pptx.enum.text import MSO_ANCHOR, PP_ALIGN
from pptx.oxml.ns import qn
from pptx.util import Emu, Inches, Pt

# ---------------------------------------------------------------- rutas

ROOT = Path(__file__).resolve().parents[2]
OUT_DIR = ROOT / "docs" / "presentacion"
ASSET_DIR = OUT_DIR / "assets"
PPTX_PATH = OUT_DIR / "TP2-VoidHex-Tactics.pptx"

# ---------------------------------------------------------------- diseño

W, H = 13.333, 7.5
MARGIN = 0.62
CONTENT_W = W - 2 * MARGIN

BG = RGBColor(0x0F, 0x17, 0x2A)
PANEL = RGBColor(0x1E, 0x29, 0x3B)
PANEL_HI = RGBColor(0x24, 0x33, 0x4A)
BORDER = RGBColor(0x33, 0x41, 0x55)
TEXT = RGBColor(0xF8, 0xFA, 0xFC)
BODY = RGBColor(0xCB, 0xD5, 0xE1)
MUTED = RGBColor(0x94, 0xA3, 0xB8)

P1 = RGBColor(0x2D, 0xD4, 0xBF)
P2 = RGBColor(0xA7, 0x8B, 0xFA)
P3 = RGBColor(0xFB, 0xBF, 0x24)
GREEN = RGBColor(0x4A, 0xDE, 0x80)
RED = RGBColor(0xF8, 0x71, 0x71)

FONT = os.environ.get("DECK_FONT", "Segoe UI")
MONO = os.environ.get("DECK_MONO", "Consolas")


# ---------------------------------------------------------------- helpers base


def new_deck() -> Presentation:
    deck = Presentation()
    deck.slide_width = Inches(W)
    deck.slide_height = Inches(H)
    return deck


def blank(deck: Presentation, number: int, total: int):
    slide = deck.slides.add_slide(deck.slide_layouts[6])
    fill = slide.background.fill
    fill.solid()
    fill.fore_color.rgb = BG

    if number:
        label = slide.shapes.add_textbox(Inches(W - 1.0), Inches(H - 0.52), Inches(0.6), Inches(0.3))
        tf = label.text_frame
        tf.margin_left = tf.margin_right = 0
        p = tf.paragraphs[0]
        p.alignment = PP_ALIGN.RIGHT
        run = p.add_run()
        run.text = f"{number}/{total}"
        run.font.size = Pt(10)
        run.font.color.rgb = RGBColor(0x47, 0x55, 0x69)
        run.font.name = FONT
    return slide


def textbox(slide, x, y, w, h, *, anchor=MSO_ANCHOR.TOP, wrap=True):
    box = slide.shapes.add_textbox(Inches(x), Inches(y), Inches(w), Inches(h))
    tf = box.text_frame
    tf.word_wrap = wrap
    tf.vertical_anchor = anchor
    tf.margin_left = tf.margin_right = tf.margin_top = tf.margin_bottom = 0
    return box


def style_run(run, size, color, *, bold=False, font=FONT, italic=False):
    run.font.size = Pt(size)
    run.font.color.rgb = color
    run.font.bold = bold
    run.font.italic = italic
    run.font.name = font


def write(slide, x, y, w, h, text, size, color, *, bold=False, font=FONT,
          align=PP_ALIGN.LEFT, anchor=MSO_ANCHOR.TOP, spacing=1.0):
    box = textbox(slide, x, y, w, h, anchor=anchor)
    p = box.text_frame.paragraphs[0]
    p.alignment = align
    p.line_spacing = spacing
    run = p.add_run()
    run.text = text
    style_run(run, size, color, bold=bold, font=font)
    return box


def head(slide, kicker, title, accent):
    """Kicker en versalitas + título afirmación + barra de acento."""
    if kicker:
        write(slide, MARGIN, 0.42, CONTENT_W, 0.26, kicker.upper(), 10.5, accent, bold=True)

    # Título adaptativo: las afirmaciones largas bajan de cuerpo para no invadir la barra.
    if len(title) <= 55:
        size = 28
    elif len(title) <= 70:
        size = 25
    else:
        size = 23

    write(slide, MARGIN, 0.72, CONTENT_W, 0.95, title, size, TEXT, bold=True, spacing=0.95)

    bar = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(MARGIN), Inches(1.62),
                                 Inches(0.85), Inches(0.055))
    bar.fill.solid()
    bar.fill.fore_color.rgb = accent
    bar.line.fill.background()
    bar.shadow.inherit = False
    bar.adjustments[0] = 0.5


def rect(slide, x, y, w, h, *, fill=PANEL, line=BORDER, radius=0.06, width=1.0):
    shape = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(x), Inches(y), Inches(w), Inches(h))
    shape.adjustments[0] = radius
    if fill is None:
        shape.fill.background()
    else:
        shape.fill.solid()
        shape.fill.fore_color.rgb = fill
    if line is None:
        shape.line.fill.background()
    else:
        shape.line.color.rgb = line
        shape.line.width = Pt(width)
    shape.shadow.inherit = False
    shape.text_frame.text = ""
    return shape


def grid(slide, x, y, w, rows, ratios, *, row_h=0.6, font=12, head_font=10.5,
         accent=BORDER, bold_first_col=False):
    """Tabla dibujada con formas: control total del estilo, sin tema del deck."""
    total = sum(ratios)
    widths = [w * r / total for r in ratios]

    for r, row in enumerate(rows):
        cy = y + r * row_h
        is_head = r == 0
        cx = x
        for c, cell in enumerate(row):
            cw = widths[c]
            fill = PANEL_HI if is_head else PANEL
            shape = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(cx), Inches(cy), Inches(cw), Inches(row_h))
            shape.fill.solid()
            shape.fill.fore_color.rgb = fill
            shape.line.color.rgb = BORDER if not is_head else accent
            shape.line.width = Pt(0.75)
            shape.shadow.inherit = False

            box = textbox(slide, cx + 0.14, cy + 0.06, cw - 0.28, row_h - 0.12,
                          anchor=MSO_ANCHOR.MIDDLE)
            p = box.text_frame.paragraphs[0]
            p.line_spacing = 0.95
            run = p.add_run()
            run.text = str(cell)
            if is_head:
                style_run(run, head_font, MUTED, bold=True)
            else:
                style_run(run, font, TEXT if (bold_first_col and c == 0) else BODY,
                          bold=(bold_first_col and c == 0),
                          font=MONO if cell.startswith("`") and cell.endswith("`") else FONT)
                if cell.startswith("`"):
                    run.text = cell.strip("`")
            cx += cw


def code(slide, x, y, w, lines, *, accent=P1, size=11.5, line_h=0.28, pad=0.24):
    h = len(lines) * line_h + 2 * pad
    rect(slide, x, y, w, h, fill=RGBColor(0x0B, 0x12, 0x20), line=BORDER, radius=0.04)
    box = textbox(slide, x + pad, y + pad - 0.02, w - 2 * pad, h - 2 * pad)
    tf = box.text_frame
    for i, line in enumerate(lines):
        p = tf.paragraphs[0] if i == 0 else tf.add_paragraph()
        p.line_spacing = 1.0
        run = p.add_run()
        run.text = line
        is_comment = line.strip().startswith("#") or line.strip().startswith("//")
        style_run(run, size, MUTED if is_comment else accent, font=MONO)
    return h


def picture(slide, x, y, w, h, filename, *, line=BORDER):
    """Captura real de una escena, con marco para que no se confunda con el fondo."""
    path = ASSET_DIR / filename
    if not path.exists():
        raise FileNotFoundError(f"Falta la captura {path}. Ver docs/presentacion/README.md")
    rect(slide, x - 0.03, y - 0.03, w + 0.06, h + 0.06, fill=None, line=line, radius=0.02, width=1.0)
    return slide.shapes.add_picture(str(path), Inches(x), Inches(y), Inches(w), Inches(h))


def picture(slide, x, y, w, h, filename, *, line=BORDER):
    """Captura real de una escena, con marco para que no se confunda con el fondo."""
    path = ASSET_DIR / filename
    if not path.exists():
        raise FileNotFoundError(f"Falta la captura {path}. Ver docs/presentacion/README.md")
    rect(slide, x - 0.03, y - 0.03, w + 0.06, h + 0.06, fill=None, line=line, radius=0.02, width=1.0)
    return slide.shapes.add_picture(str(path), Inches(x), Inches(y), Inches(w), Inches(h))


def arrow(slide, x1, y1, x2, y2, color=MUTED, *, width=1.25, dash=False):
    conn = slide.shapes.add_connector(MSO_CONNECTOR.STRAIGHT, Inches(x1), Inches(y1), Inches(x2), Inches(y2))
    conn.line.color.rgb = color
    conn.line.width = Pt(width)
    ln = conn.line._get_or_add_ln()
    tail = ln.makeelement(qn("a:tailEnd"), {"type": "triangle", "w": "med", "len": "med"})
    ln.append(tail)
    if dash:
        for el in ln.findall(qn("a:prstDash")):
            ln.remove(el)
        ln.append(ln.makeelement(qn("a:prstDash"), {"val": "dash"}))
    return conn


def notes(slide, text):
    slide.notes_slide.notes_text_frame.text = text.strip()


def takeaway(slide, text, accent):
    rect(slide, MARGIN, H - 1.02, CONTENT_W, 0.52, fill=PANEL_HI, line=None, radius=0.12)
    bar = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(MARGIN), Inches(H - 1.02),
                                 Inches(0.06), Inches(0.52))
    bar.fill.solid()
    bar.fill.fore_color.rgb = accent
    bar.line.fill.background()
    bar.shadow.inherit = False
    write(slide, MARGIN + 0.26, H - 1.02, CONTENT_W - 0.5, 0.52, text, 12.5, BODY,
          anchor=MSO_ANCHOR.MIDDLE)


# ---------------------------------------------------------------- diagramas


def diagram_ocp(slide, x, y, w):
    labels = [("MoveCommand", P2), ("AttackCommand", P2), ("ShieldCommand", P2),
              ("TractorBeamCommand", GREEN)]
    bw = 2.15
    for i, (name, color) in enumerate(labels):
        cx = x + i * (bw + 0.28)
        custom = "en vivo" if i == 3 else "implementado"
        rect(slide, cx, y, bw, 0.72, fill=PANEL, line=color, width=1.3)
        write(slide, cx, y + 0.12, bw, 0.28, name, 10.5, color, bold=True, align=PP_ALIGN.CENTER)
        write(slide, cx, y + 0.42, bw, 0.24, custom, 8.5, MUTED, align=PP_ALIGN.CENTER)
        arrow(slide, cx + bw / 2, y + 0.74, cx + bw / 2, y + 1.14, color)

    rect(slide, x, y + 1.16, w, 0.62, fill=PANEL_HI, line=GREEN, width=1.6, radius=0.1)
    write(slide, x, y + 1.16, w, 0.62, "ICommand  →  CommandProcessor  (no cambia)", 13, GREEN,
          bold=True, align=PP_ALIGN.CENTER, anchor=MSO_ANCHOR.MIDDLE)


# ---------------------------------------------------------------- deck


def build():
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    ASSET_DIR.mkdir(parents=True, exist_ok=True)

    total = 20
    deck = new_deck()
    numbers = iter(range(1, total + 1))

    # ---------------------------------------------------------- 1 portada
    s = blank(deck, next(numbers), total)
    write(s, MARGIN, 1.55, CONTENT_W, 0.3, "PROGRAMACIÓN DE VIDEOJUEGOS III · 2026", 12, P1, bold=True)
    write(s, MARGIN, 2.0, CONTENT_W, 1.3, "Tres decisiones de arquitectura,\nverificadas con tests", 40,
          TEXT, bold=True, spacing=1.05)
    write(s, MARGIN, 3.55, CONTENT_W, 0.4, "Void Hex Tactics · TP2", 20, P2, bold=True)

    chips = [("P1_GridState", "ISP + Observer · 5 tests", P1),
             ("P2_CommandPipeline", "Pipeline async · 7 + 1 PlayMode", P2),
             ("P3_Loadout", "DI + Data-driven · 9 tests", P3)]
    for i, (folder, tag, color) in enumerate(chips):
        cx = MARGIN + i * 4.06
        rect(s, cx, 4.55, 3.82, 1.05, fill=PANEL, line=color, width=1.3)
        write(s, cx + 0.22, 4.7, 3.4, 0.3, folder, 12, TEXT, bold=True, font=MONO)
        write(s, cx + 0.22, 5.05, 3.4, 0.3, tag, 10.5, color, bold=True)

    write(s, MARGIN, 6.15, CONTENT_W, 0.6,
          "Matias Andres Fahler   ·   Unity 6000.5.9f1   ·   81 tests verdes (80 Edit Mode + 1 Play Mode)",
          13, MUTED)
    notes(s, """
15 s. Tres problemas técnicos del TP1, la solución declarada para cada uno,
y la evidencia de que funciona.

Tiempo total objetivo: 18-19 minutos, dejando margen para preguntas.
""")

    # ---------------------------------------------------------- 2 mapa
    s = blank(deck, next(numbers), total)
    head(s, "Qué entrego", "Tres prototipos, cada uno con la suite que lo verifica", P1)
    grid(s, MARGIN, 1.95, CONTENT_W, [
        ["Desafío declarado en el TP1", "Solución implementada", "Prototipo · tests"],
        ["Exponer el mapa sin God Object ni polling en Update()", "ISP + Observer", "P1_GridState · 5"],
        ["Alternar PA con animaciones asíncronas", "Pipeline ICommand + async", "P2_CommandPipeline · 7 + 1"],
        ["Servicios sin singletons + módulos MK I–III", "VContainer + ScriptableObjects", "P3_Loadout · 9"],
    ], [3.3, 2.5, 2.2], row_h=0.78, font=13, accent=P1)
    write(s, MARGIN, 5.3, CONTENT_W, 0.7,
          "El diagnóstico de cada desafío ya está en el TP1. Hoy lo que se muestra es cómo se implementó y cómo se verifica.",
          13, BODY, spacing=1.15)
    takeaway(s, "La evidencia de cada decisión es un test que corre sin abrir una escena.", P1)
    notes(s, """
30 s. No vuelvo sobre los problemas: están planteados en el TP1. Lo que cambia es que ahora está
implementado y que cada propiedad tiene un test que la verifica.

Las tres columnas: desafío declarado, solución implementada, y cuántos tests la sostienen.
""")

    # ---------------------------------------------------------- 3 las tres escenas
    s = blank(deck, next(numbers), total)
    head(s, "Lo que se puede abrir", "Tres escenas que corren solas, con UI nativa y modelos CC0", P1)

    scenes = [
        ("P1 · GridState", "escena-p1.png", P1,
         "4 eventos · 144 celdas\nruta de 21 celdas, costo 20"),
        ("P2 · CommandPipeline", "escena-p2.png", P2,
         "PA 6 → 1 · hull 6 → 3\ncola libre al terminar"),
        ("P3 · Loadout", "escena-p3.png", P3,
         "PA 6 · escudos 5\nmovimiento 4"),
    ]
    for i, (title, filename, color, caption) in enumerate(scenes):
        cx = MARGIN + i * 4.06
        write(s, cx, 1.88, 3.82, 0.26, title, 11.5, color, bold=True)
        picture(s, cx, 2.2, 3.82, 2.15, filename)
        write(s, cx, 4.5, 3.82, 0.6, caption, 10.5, BODY, spacing=1.15)

    write(s, MARGIN, 5.35, CONTENT_W, 0.35,
          "VoidHex → Open P1 / P2 / P3 Scene, Play. Cada panel es uGUI nativo: sin dependencias de UI.", 12.5, BODY)
    takeaway(s, "Los números del HUD son los mismos que assertan los tests: no hay dos verdades.", P1)
    notes(s, """
40 s. Tres capturas reales, tomadas del editor el 04/11 (las mismas están en docs/evidencia).

Lo importante no es el arte: es que el HUD muestra el dato que después se verifica en un test.
En P1, el contador de eventos queda en 4 porque el terreno inicial tiene 4 celdas no vacías.
En P2, los PA bajan de 6 a 1 con los tres comandos del guion.
En P3, el loadout de la demo da 6 / 5 / 4.

Si preguntan por los modelos: Kenney Space Kit, CC0, cargados en runtime; la escena cae a
primitivas si falta un FBX.
""")

    # ---------------------------------------------------------- 4 qué se testea
    s = blank(deck, next(numbers), total)
    head(s, "Estrategia", "Cada patrón se verifica con una técnica concreta", P2)
    grid(s, MARGIN, 1.9, CONTENT_W, [
        ["Patrón", "Cómo se verifica", "Defecto que detectaría"],
        ["ISP", "Reflexión sobre los miembros del tipo", "Agregar SetCell a la interfaz de lectura"],
        ["Observer", "Contador de eventos en el test", "Polling disfrazado de reactividad"],
        ["Command + cola", "TaskCompletionSource controlado por el test", "Regla acoplada al tiempo del arte"],
        ["OCP", "Se envía un comando nuevo por la misma interfaz", "Un switch por tipo en el procesador"],
        ["DI / DIP", "Doble inyectado por constructor", "Servicio que busca sus dependencias"],
        ["Flyweight", "Comparación campo por campo contra el asset", "Duplicar datos entre instancias"],
        ["Inmutable", "Se conserva la instancia previa y se compara", "Aliasing y doble conteo"],
    ], [2.2, 3.3, 3.5], row_h=0.52, font=10.5, head_font=10, accent=P2)
    takeaway(s, "No se testea el patrón: se testea la propiedad que promete, y debe fallar por ese motivo.", P2)
    notes(s, """
60 s. Esta slide ordena todo lo que viene después: cada patrón tiene una técnica de testeo distinta,
y un defecto concreto que el test detectaría.

El criterio es: si el test no puede fallar por ese motivo, no sirve. No hay un test por método.

Los tres bloques que siguen (P1, P2, P3) muestran el código del patrón y después el código del
test que lo sostiene.
""")

    # ---------------------------------------------------------- 4 P1 código
    s = blank(deck, next(numbers), total)
    head(s, "P1 · Patrón · ISP + Observer", "Tres contratos y un evento: nadie hace lo que no le toca", P1)

    write(s, MARGIN, 1.95, 5.85, 0.28, "ISP · un contrato por necesidad", 12, P1, bold=True)
    left_h = code(s, MARGIN, 2.28, 5.85, [
        "public interface IGridQueryService      // lectura",
        "{",
        "    HexCell GetCell(HexCoordinates c);",
        "    bool IsPassable(HexCoordinates c);",
        "    int MovementCostToEnter(HexCoordinates c);",
        "}",
        "",
        "public interface IGridStatePublisher    // notificación",
        "{",
        "    event Action<GridChange> Changed;",
        "}",
        "",
        "public interface IGridCommands          // escritura",
        "{",
        "    bool SetCell(HexCell cell);",
        "}",
    ], accent=BODY, size=8.8, line_h=0.16)
    write(s, MARGIN, 2.28 + left_h + 0.16, 5.85, 0.7,
          "GridModel implementa las tres. La vista recibe solo la de eventos; los comandos, solo la de escritura.",
          10, MUTED, spacing=1.15)

    write(s, 6.86, 1.95, 5.85, 0.28, "Observer · el presentador no consulta por frame", 12, P1, bold=True)
    right_h = code(s, 6.86, 2.28, 5.85, [
        "public bool SetCell(HexCell cell)",
        "{",
        "    if (!_cells.TryGetValue(cell.Coordinates, out var current))",
        "        return false;",
        "",
        "    if (current.Equals(cell)) return false;  // mismo valor: no notifica",
        "",
        "    _cells[cell.Coordinates] = cell;",
        "    Changed?.Invoke(new GridChange(cell.Coordinates, CellChanged));",
        "    return true;",
        "}",
        "",
        "// GridPresenter: una suscripción, sin Update()",
        "_publisher.Changed += HandleChanged;",
    ], accent=BODY, size=8.8, line_h=0.16)
    write(s, 6.86, 2.28 + right_h + 0.16, 5.85, 0.7,
          "Test: SetCell con el mismo valor devuelve false y el contador de eventos queda en 0.",
          10, MUTED, spacing=1.15)

    takeaway(s, "ISP se verifica por reflexión; el Observer, con el contador de eventos.", P1)
    notes(s, """
70 s. Este es el código real de los tres contratos y de la mutación.

ISP: la interfaz de lectura no declara ninguna mutación, así que el Pathfinder (que recibe
IGridQueryService) no tiene forma de escribir el mapa. No es disciplina: no compila si lo intenta.

Observer: la vista se suscribe una vez en OnEnable y no tiene Update(). Escribir el mismo valor
retorna false antes de tocar el diccionario, así que no hay evento ni redibujo.

Cómo se testea: IGridQueryService_ExposesNoMutatingMembers recorre los miembros por reflexión
y falla si alguien agrega SetCell a la interfaz de lectura. SetCell_WithSameValue_DoesNotRaiseChange
se suscribe al evento, escribe el mismo valor y asserta que el contador quedó en cero.
""")

    # ---------------------------------------------------------- 6 P1 evidencia
    s = blank(deck, next(numbers), total)
    head(s, "P1 · Evidencia", "Reactivo de verdad: sin mutación real, no hay trabajo", P1)

    stats = [("1", "evento", "por cada mutación real", GREEN),
             ("0", "eventos", "al reescribir el mismo valor", TEXT),
             ("4", "eventos", "al cargar 2 asteroides y 2 nebulosas", MUTED)]
    for i, (big, label, detail, color) in enumerate(stats):
        cy = 1.95 + i * 1.18
        write(s, MARGIN, cy - 0.08, 0.85, 0.72, big, 36, color, bold=True)
        write(s, MARGIN + 0.85, cy + 0.04, 3.4, 0.3, label, 13, TEXT, bold=True)
        write(s, MARGIN + 0.85, cy + 0.36, 3.4, 0.5, detail, 10.5, MUTED)

    write(s, MARGIN, 5.5, 4.2, 0.9,
          "Los dos tests de contrato usan reflexión: si alguien agrega SetCell a la interfaz de lectura, se ponen rojos.",
          11, MUTED, spacing=1.15)

    write(s, 5.25, 1.95, 7.45, 0.3, "SetCell_WithSameValue_DoesNotRaiseChange", 11.5, GREEN,
          bold=True, font=MONO)
    code(s, 5.25, 2.32, 7.45, [
        "[Test]",
        "public void SetCell_WithSameValue_DoesNotRaiseChange()",
        "{",
        "    var grid = new GridModel(4, 4);",
        "    var raised = 0;",
        "    IGridStatePublisher publisher = grid;",
        "    publisher.Changed += _ => raised++;",
        "",
        "    var unchanged = grid.GetCell(HexCoordinates.Origin);",
        "    var mutated = grid.SetCell(unchanged);",
        "",
        "    Assert.That(mutated, Is.False);",
        "    Assert.That(raised, Is.Zero,",
        '        "Una escritura sin cambio real no debe notificar a la vista.");',
        "}",
    ], accent=BODY, size=9.5, line_h=0.18)

    takeaway(s, "El evento solo se emite si la celda cambió de verdad: el polling disfrazado se cae.", P1)
    notes(s, """
60 s. Escena real: mapa 12×12 con asteroides y cristales del Kenney Space Kit.
La ruta al destino es de 21 celdas y costo 20; el contador de eventos queda en 4,
que son las 4 celdas de terreno inicial: 2 asteroides que bloquean y 2 nebulosas de costo 2.

El código de la derecha es el test que sostiene el 0: escribir el mismo valor no emite evento.
Los dos tests de contrato usan reflexión: verifican que el contrato de publicación solo
tenga eventos y que el de lectura no tenga mutaciones. Si mañana alguien agrega SetCell
a la interfaz de lectura, el test se pone rojo antes de que llegue a la UI.
""")

    # ---------------------------------------------------------- 7 P2 código
    s = blank(deck, next(numbers), total)
    head(s, "P2 · Patrón · Command + cola", "Cada acción es un objeto; el pipeline no tiene un switch", P2)

    write(s, MARGIN, 1.95, 5.85, 0.28, "Command · una clase por habilidad (OCP)", 12, P2, bold=True)
    left_h = code(s, MARGIN, 2.28, 5.85, [
        "public interface ICommand",
        "{",
        "    int ActionPointCost { get; }",
        "    string VisualKey { get; }              // dato, no lógica",
        "    CommandValidation Validate(ShipModel model);",
        "    ShipModel Execute(ShipModel model);",
        "}",
        "",
        "public ShipModel Execute(ShipModel model)   // MoveCommand",
        "    => model.WithPosition(Target)",
        "            .WithActionPoints(model.ActionPoints - ActionPointCost);",
    ], accent=BODY, size=8.8, line_h=0.16)
    write(s, MARGIN, 2.28 + left_h + 0.16, 5.85, 0.9,
          "Agregar una habilidad es escribir otra clase: el procesador sigue recibiendo ICommand, sin un switch por tipo.",
          10, MUTED, spacing=1.15)
    write(s, MARGIN, 5.55, 5.85, 0.3, "Submit_NewCommandVariant_RequiresNoPipelineChange", 9.5, GREEN, font=MONO)

    write(s, 6.86, 1.95, 5.85, 0.28, "Cola serializada · una regla a la vez", 12, P2, bold=True)
    right_h = code(s, 6.86, 2.28, 5.85, [
        "await _queue.WaitAsync();          // SemaphoreSlim(1, 1)",
        "try",
        "{",
        "    var validation = command.Validate(before);",
        "    if (!validation.IsAllowed)",
        "        return CommandOutcome.Rejected(validation, before);",
        "",
        "    SetPhase(TurnPhase.Executing);",
        "    _state = command.Execute(before);   // la regla, al instante",
        "",
        "    await _presentation.PlayAsync(execution);  // la vista, después",
        "    return outcome;",
        "}",
        "finally { SetPhase(TurnPhase.PlayerTurn); _queue.Release(); }",
    ], accent=BODY, size=8.8, line_h=0.16)
    write(s, 6.86, 2.28 + right_h + 0.16, 5.85, 0.7,
          "Validar y ejecutar es síncrono; lo único asíncrono es esperar la animación.",
          10, MUTED, spacing=1.15)

    takeaway(s, "El comando no conoce Unity: por eso el pipeline puede testearse sin Play Mode.", P2)
    notes(s, """
70 s. Arriba a la izquierda, la interfaz: cuatro miembros y ninguna referencia al motor.
MoveCommand solo devuelve un ShipModel nuevo; no anima nada.

A la derecha, el núcleo del procesador. El orden es el argumento: validate → execute → notifica
→ await. La regla ya está aplicada cuando la animación arranca, y el SemaphoreSlim impide que
dos comandos se intercalen.

OCP sin discurso: para agregar TractorBeamCommand se crea una clase que implementa ICommand.
CommandProcessor no cambia. El test Submit_NewCommandVariant_RequiresNoPipelineChange envía
ShieldCommand por el mismo SubmitAsync y verifica el efecto.

Cómo se testea el desacoplamiento: ControlledPresentation guarda un TaskCompletionSource por
llamada. El test asserta posición, PA y Phase con ese Task todavía pendiente, y recién después
lo completa. Si la lógica esperara la animación, las aserciones fallan.
""")

    # ---------------------------------------------------------- 10 P2 evidencia
    s = blank(deck, next(numbers), total)
    head(s, "P2 · Evidencia", "El estado cambia antes de que termine la animación", P2)

    checks = [("1", "Posición y PA ya cambiaron", "el efecto lógico está aplicado"),
              ("2", "Phase == Executing", "la animación está en curso"),
              ("3", "El Task sigue pendiente", "todavía no terminó")]
    for i, (n, title, detail) in enumerate(checks):
        cy = 1.95 + i * 0.72
        write(s, MARGIN, cy - 0.02, 0.45, 0.4, n, 17, P2, bold=True)
        write(s, MARGIN + 0.5, cy, 3.7, 0.3, title, 13, TEXT, bold=True)
        write(s, MARGIN + 0.5, cy + 0.3, 3.7, 0.28, detail, 10, MUTED)

    # La corrida real de la demo, no un ejemplo genérico.
    rect(s, MARGIN, 4.2, 4.2, 1.4, fill=PANEL_HI, line=BORDER, width=1.1)
    write(s, MARGIN + 0.24, 4.33, 3.7, 0.28, "CORRIDA REAL DE LA ESCENA", 10.5, P2, bold=True)
    write(s, MARGIN + 0.24, 4.63, 3.7, 0.55,
          "Mover 2 PA → Atacar 2 PA →\nMover 1 PA", 10.5, BODY, font=MONO, spacing=1.15)
    write(s, MARGIN + 0.24, 5.22, 3.7, 0.3, "PA 6 → 1   ·   hull 6 → 3", 11, GREEN, bold=True)

    write(s, MARGIN, 5.85, 4.2, 0.55,
          "Segundo test: la cola retiene el comando\nhasta que termina la animación.",
          10.5, MUTED, spacing=1.15)

    write(s, 5.1, 1.95, 7.6, 0.3, "Submit_AppliesLogicBeforeTheAnimationCompletes", 11.5, GREEN,
          bold=True, font=MONO)
    code(s, 5.1, 2.32, 7.6, [
        "[Test]",
        "public async Task Submit_AppliesLogicBeforeTheAnimationCompletes()",
        "{",
        "    var presentation = new ControlledPresentation();",
        "    var processor = new CommandProcessor(ShipModel.Start(), presentation);",
        "",
        "    var pending = processor.SubmitAsync(new MoveCommand(new HexCoordinates(2, 0), 2));",
        "",
        "    Assert.That(processor.State.Position, Is.EqualTo(new HexCoordinates(2, 0)));",
        "    Assert.That(processor.State.ActionPoints, Is.EqualTo(4));",
        "    Assert.That(processor.Phase, Is.EqualTo(TurnPhase.Executing));",
        "    Assert.That(pending.IsCompleted, Is.False,",
        '        "Sin animación terminada, el comando sigue en curso.");',
        "",
        "    presentation.CompleteNext();",
        "    await pending;",
        "    Assert.That(processor.Phase, Is.EqualTo(TurnPhase.PlayerTurn));",
        "}",
    ], accent=BODY, size=9, line_h=0.18)

    takeaway(s, "Si alguien espera la animación antes de mutar el estado, este test falla.", P2)
    notes(s, """
60 s. Este es el test que demuestra el desacoplamiento temporal.

Las tres aserciones del medio se hacen con el Task todavía pendiente: el estado ya cambió
aunque la vista no terminó. Al final, CompleteNext libera la animación y la fase vuelve sola.

Pregunta que invita: "¿qué error detectaría?"
→ acoplar la regla al tiempo de la animación, o intercalar dos comandos y corromper el estado.
""")

    # ---------------------------------------------------------- 10 P2 OCP
    s = blank(deck, next(numbers), total)
    head(s, "P2 · Extensibilidad", "Agregar una habilidad es crear una clase, no modificar el sistema", P2)
    diagram_ocp(s, MARGIN, 2.0, CONTENT_W)
    write(s, MARGIN, 4.05, CONTENT_W, 0.4, "✓  Submit_NewCommandVariant_RequiresNoPipelineChange", 13,
          GREEN, font=MONO)
    write(s, MARGIN, 4.5, CONTENT_W, 0.5,
          "CommandProcessor no tiene un switch por tipo: valida, ejecuta y notifica.",
          13, BODY, spacing=1.15)
    write(s, MARGIN, 5.1, CONTENT_W, 0.5,
          "Diferido: Undo/Redo, que el mismo pipeline habilita. TractorBeamCommand se escribe en vivo.",
          12, MUTED, spacing=1.15)
    takeaway(s, "ShieldCommand ya existe y el pipeline no se enteró.", P2)
    notes(s, """
50 s. En la escena, ShieldCommand cuesta 2 PA y suma 4 de escudo: el pipeline no cambió.

Demo D4: agregar TractorBeamCommand en vivo. Valida PA, mueve la nave enemiga,
y CommandProcessor no cambia una línea. Plan B: mostrar CommandProcessor.cs
(no hay switch por tipo) y el test Submit_NewCommandVariant_RequiresNoPipelineChange.
""")

    # ---------------------------------------------------------- 10 P3 DI
    s = blank(deck, next(numbers), total)
    head(s, "P3 · Patrón · DI por constructor", "El contenedor compone; el servicio no busca nada", P3)

    write(s, MARGIN, 1.95, 5.85, 0.28, "Composition root · EquipmentLifetimeScope", 12, P3, bold=True)
    left_h = code(s, MARGIN, 2.28, 5.85, [
        "protected override void Configure(IContainerBuilder builder)",
        "{",
        "    builder.RegisterInstance<IModuleCatalog>(moduleCatalog);",
        "    builder.RegisterInstance(shipConfig.ToConfig());",
        "    builder.Register<EquipmentService>(Lifetime.Singleton)",
        "           .As<IEquipmentService>();",
        "    builder.RegisterComponentInHierarchy<LoadoutDemo>();",
        "}",
    ], accent=BODY, size=8.8, line_h=0.16)
    write(s, MARGIN, 2.28 + left_h + 0.16, 5.85, 1.0,
          "El scope es el único punto que menciona tipos concretos. Vive en Presentación y no se ve desde el dominio.",
          10, MUTED, spacing=1.15)
    write(s, MARGIN, 2.28 + left_h + 0.9, 5.85, 0.55,
          "EquipmentService_WithInjectedFakeCatalog_ResolvesModulesById", 9.5, GREEN, font=MONO)

    write(s, 6.86, 1.95, 5.85, 0.28, "Consumidor · no busca: recibe", 12, GREEN, bold=True)
    injected_h = code(s, 6.86, 2.28, 5.85, [
        "[Inject]",
        "public void Construct(IEquipmentService service,",
        "                     IModuleCatalog catalog)",
        "{",
        "    _service = service;",
        "    _catalog = catalog;",
        "    Ready?.Invoke(service);    // evento, no un orden implícito",
        "}",
    ], accent=BODY, size=8.8, line_h=0.16)

    dip_y = 2.28 + injected_h + 0.28
    write(s, 6.86, dip_y, 5.85, 0.28, "DIP · el servicio depende de la interfaz", 12, GREEN, bold=True)
    dip_h = code(s, 6.86, dip_y + 0.32, 5.85, [
        "public EquipmentService(ShipConfig config, IModuleCatalog catalog)",
        "{",
        "    _config = config ?? throw new ArgumentNullException(nameof(config));",
        "    _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));",
        "}",
    ], accent=BODY, size=8.4, line_h=0.16)
    write(s, 6.86, dip_y + 0.32 + dip_h + 0.14, 5.85, 0.5,
          "El test pasa FakeCatalog; la escena pasa el asset. Mismo contrato.", 10, MUTED, spacing=1.15)

    takeaway(s, "Nadie hace GetComponent del servicio: la dependencia entra por parámetro.", P3)
    notes(s, """
60 s. Tres piezas, todas reales.

1) Composition root: el LifetimeScope registra la instancia del catálogo (el asset) como
IModuleCatalog, la config, y EquipmentService como IEquipmentService. Es el único archivo que
nombra tipos concretos.

2) Consumidor: LoadoutDemo recibe el servicio por un método marcado [Inject]. No hay
GameManager.Instance ni Service Locator: la clase no pide, recibe. El Ready es un evento porque
[Inject] puede llegar después de Start (fue un hallazgo real, está en la slide de ajustes).

3) DIP en el servicio: el constructor pide IModuleCatalog, no ModuleCatalogAsset. Por eso el
test puede pasarle un FakeCatalog que cuenta las consultas, y la escena le pasa el asset.

Cómo se testea: EquipmentService_WithInjectedFakeCatalog_ResolvesModulesById crea el servicio
con el fake, equipa laser-mk1 y asserta que el catálogo recibió exactamente una consulta.
Ese test corre en Edit Mode porque el servicio no es un MonoBehaviour.
""")

    # ---------------------------------------------------------- 14 P3 Flyweight
    s = blank(deck, next(numbers), total)
    head(s, "P3 · Patrón · Flyweight + inmutable", "Un asset por módulo; equipar devuelve un loadout nuevo", P3)

    write(s, MARGIN, 1.95, 5.85, 0.28, "Flyweight · el asset se traduce al dominio", 12, P3, bold=True)
    left_h = code(s, MARGIN, 2.28, 5.85, [
        "[CreateAssetMenu(menuName = \"VoidHex/Module\")]",
        "public sealed class ShipModuleAsset : ScriptableObject",
        "{",
        "    [SerializeField] private int bonusShields;   // datos, no reglas",
        "",
        "    public ModuleDefinition ToDefinition() => new ModuleDefinition(",
        "        moduleId, displayName, slot, tier,",
        "        bonusActionPoints, bonusShields, bonusMovement,",
        "        grantedAbilityId);   // el dominio nunca ve el asset",
        "}",
    ], accent=BODY, size=8.8, line_h=0.16)
    write(s, MARGIN, 2.28 + left_h + 0.16, 5.85, 0.9,
          "Una instancia por módulo, compartida por todas las naves que lo equipan: eso es el Flyweight.",
          10, MUTED, spacing=1.15)
    write(s, MARGIN, 5.15, 5.85, 0.3, "ShipModuleAsset_MapsToDefinition_AllFields", 9.5, GREEN, font=MONO)

    write(s, 6.86, 1.95, 5.81, 0.28, "Inmutable · equipar no muta", 12, GREEN, bold=True)
    right_h = code(s, 6.86, 2.28, 5.81, [
        "public EquipResult TryEquip(ModuleDefinition module)",
        "{",
        "    if (!IsKnownSlot(module.Slot))",
        "        return EquipResult.Failed(WrongSlotType, this);",
        "",
        "    var copy = CopySlots();          // nunca el arreglo propio",
        "    copy[(int)module.Slot] = module; // un slot por tipo ⇒ reemplaza",
        "    return EquipResult.Success(new Loadout(copy));",
        "}",
    ], accent=BODY, size=8.8, line_h=0.16)
    write(s, 6.86, 2.28 + right_h + 0.16, 5.81, 0.9,
          "Un slot por tipo hace imposible el estado “módulo en slot incompatible”: se elimina, no se testea.",
          10, MUTED, spacing=1.15)
    write(s, 6.86, 5.15, 5.81, 0.3, "TryEquip_ReturnsNewLoadout_WithoutMutatingTheOriginal", 9.5, GREEN, font=MONO)

    takeaway(s, "El asset es adaptador y Flyweight; el dominio recibe un ModuleDefinition inmutable.", P3)
    notes(s, """
60 s. Izquierda: el ScriptableObject guarda datos y sabe traducirse a ModuleDefinition, que es
un tipo de dominio sin UnityEngine. El catálogo indexa esos ModuleDefinition una sola vez, así
que todas las naves comparten la misma instancia: eso es Flyweight.

Derecha: TryEquip valida el slot y trabaja sobre una copia. El loadout original sigue intacto,
lo que hace comparables los estados (y habilita undo sin trucos). El slot índice es el enum
SlotType, así que no existe el estado inválido "dos armas en el mismo slot".

Cómo se testea: TryEquip_ReturnsNewLoadout_WithoutMutatingTheOriginal guarda la referencia
original y verifica que siga vacía después de equipar. ShipModuleAsset_MapsToDefinition_AllFields
instancia el asset, llama Initialize con ocho valores y compara los ocho contra la definición.
""")

    # ---------------------------------------------------------- 15 P3 evidencia
    s = blank(deck, next(numbers), total)
    head(s, "P3 · Evidencia", "Testeable sin Unity y ampliable sin recompilar", P3)

    write(s, MARGIN, 1.95, 5.85, 0.28, "DIP · catálogo inyectado", 12.5, P3, bold=True)
    write(s, MARGIN, 2.2, 5.85, 0.26,
          "EquipmentService_WithInjectedFakeCatalog_ResolvesModulesById", 9.5, MUTED, font=MONO)
    code(s, MARGIN, 2.5, 5.85, [
        "var catalog = new FakeCatalog(LaserMk1);",
        "var service = new EquipmentService(",
        "    new ShipConfig(BaseStats), catalog);",
        "",
        'var result = service.Equip("laser-mk1");',
        "",
        "Assert.That(result.IsSuccess, Is.True);",
        "Assert.That(service.CurrentStats.Abilities,",
        "    Has.Count.EqualTo(1));",
        "Assert.That(catalog.Queries, Is.EqualTo(1),",
        '    "El servicio consulta el catálogo inyectado.");',
    ], accent=BODY, size=9, line_h=0.18)

    write(s, 6.9, 1.95, 5.81, 0.28, "Data-driven · reemplazo en el slot", 12.5, GREEN, bold=True)
    write(s, 6.9, 2.2, 5.81, 0.26,
          "TryEquip_ReplacingModule_DoesNotDoubleCount", 9.5, MUTED, font=MONO)
    code(s, 6.9, 2.5, 5.81, [
        "var withMk1 = Loadout.Empty().TryEquip(LaserMk1).Loadout;",
        "var result = withMk1.TryEquip(LaserMk2);",
        "var stats = ShipStatsCalculator.Calculate(",
        "    BaseStats, result.Loadout.Modules);",
        "",
        "Assert.That(result.Loadout.Modules, Has.Count.EqualTo(1),",
        '    "El reemplazo no acumula módulos.");',
        "Assert.That(result.Loadout.TryGetModule(SlotType.Weapon,",
        "    out var weapon), Is.True);",
        'Assert.That(weapon.Id, Is.EqualTo("laser-mk2"));',
        "Assert.That(stats.Shields, Is.EqualTo(2));",
    ], accent=BODY, size=9, line_h=0.18)

    write(s, MARGIN, 5.45, CONTENT_W, 0.35,
          "MK II reemplaza a MK I en el mismo slot: escudos 2, movimiento 3, un módulo contado.",
          12.5, BODY)
    write(s, MARGIN, 5.9, CONTENT_W, 0.35,
          "Ambos corren en Edit Mode: sin cargar una escena y sin entrar en Play Mode.",
          11.5, MUTED)

    takeaway(s, "Un slot por tipo: el estado inválido “módulo en slot incompatible” no puede existir.", P3)
    notes(s, """
60 s. El código de la izquierda demuestra DIP: el servicio recibe el catálogo y el fake cuenta
las consultas. Si mañana el catálogo viene de un servidor, el servicio no cambia.

El de la derecha demuestra data-driven: el MK II ocupa el slot del MK I y no se suman los dos.

La escena equipa laser-mk1, lo reemplaza por laser-mk2, suma thruster-mk1 y shield-mk1.
Resultado real: PA 6, escudos 5, movimiento 4, habilidades [beam, barrier].

Demos: D5 (reemplazo MK I → MK II) y D6 (equipar el MK III).
""")

    # ---------------------------------------------------------- 13 dobles
    s = blank(deck, next(numbers), total)
    head(s, "Aislamiento", "Dobles escritos a mano: nada de frameworks de mock", GREEN)

    write(s, MARGIN, 1.95, 5.85, 0.28, "Doble de catálogo · cuenta lo que el real no expone", 12, P3, bold=True)
    left_h = code(s, MARGIN, 2.28, 5.85, [
        "private sealed class FakeCatalog : IModuleCatalog",
        "{",
        "    public int Queries { get; private set; }",
        "",
        "    public bool TryGet(string id, out ModuleDefinition module)",
        "    {",
        "        Queries++;   // el dato que el servicio no publica",
        "        return _modules.TryGetValue(id, out module);",
        "    }",
        "}",
    ], accent=BODY, size=8.8, line_h=0.16)
    write(s, MARGIN, 2.28 + left_h + 0.16, 5.85, 0.9,
          "Se inyecta por constructor: el servicio no distingue el asset del doble.",
          10, MUTED, spacing=1.15)

    write(s, 6.86, 1.95, 5.85, 0.28, "Presentación controlada · el tiempo lo maneja el test", 12, P2, bold=True)
    right_h = code(s, 6.86, 2.28, 5.85, [
        "private sealed class ControlledPresentation : ICommandPresentation",
        "{",
        "    private readonly Queue<TaskCompletionSource<bool>> _pending = new();",
        "",
        "    public Task PlayAsync(CommandExecution execution)",
        "    {",
        "        var tcs = new TaskCompletionSource<bool>();",
        "        _pending.Enqueue(tcs);",
        "        return tcs.Task;      // no termina hasta que el test lo suelte",
        "    }",
        "",
        "    public void CompleteNext() => _pending.Dequeue().SetResult(true);",
        "}",
    ], accent=BODY, size=8.8, line_h=0.16)
    write(s, 6.86, 2.28 + right_h + 0.16, 5.85, 1.1,
          "Reemplaza una animación de 300 ms por una decisión del test: sin esperas ni flakiness, "
          "y permite afirmar sobre un estado intermedio.",
          10, MUTED, spacing=1.15)

    takeaway(s, "El doble reemplaza al motor y al reloj; la regla queda sola en el test.", GREEN)
    notes(s, """
50 s. Los dos dobles del TP, escritos a mano y sin librería.

FakeCatalog implementa la interfaz que el servicio pide y **cuenta** las consultas. Ese contador
es la prueba de que el servicio usó el catálogo inyectado, no una copia interna.

ControlledPresentation devuelve un Task que el test libera cuando quiere. Eso convierte el tiempo
(el riesgo de flakiness) en una decisión determinista, y además permite assertar sobre un estado
intermedio: la animación no terminó, pero la regla ya se aplicó.

Por qué a mano: son diez líneas, no agregan dependencias y el framework de mock sería más código
que el doble.
""")

    # ---------------------------------------------------------- 14 dónde vive cada test
    s = blank(deck, next(numbers), total)
    head(s, "Dónde vive cada test", "Todo lo que se puede probar sin Play Mode, se prueba sin Play Mode", P2)
    grid(s, MARGIN, 1.95, CONTENT_W, [
        ["Modo", "Qué cubre", "Por qué ahí"],
        ["Edit Mode · 80", "Reglas, contratos y composición: grid, comandos, loadout, bucle", "No necesitan frames ni ciclo de vida"],
        ["Play Mode · 1", "Que Unity invoque al adaptador de la vista", "Solo un [UnityTest] espera frames"],
    ], [2.0, 4.5, 2.8], row_h=0.8, font=11.5, accent=P2)

    blocks = [("P1 · 5", "contrato + observación", P1),
              ("P2 · 7 + 1", "pipeline + adaptador", P2),
              ("P3 · 9", "dominio + DI", P3),
              ("bucle · 59", "escena jugable", TEXT)]
    for i, (name, detail, color) in enumerate(blocks):
        cx = MARGIN + i * 3.07
        rect(s, cx, 4.5, 2.86, 1.0, fill=PANEL, line=color, width=1.2)
        write(s, cx + 0.2, 4.63, 2.5, 0.28, name, 12.5, color, bold=True)
        write(s, cx + 0.2, 4.97, 2.5, 0.4, detail, 10, MUTED)

    write(s, MARGIN, 5.72, CONTENT_W, 0.5,
          "81 verdes, corridos el 04/11 con el editor abierto (docs/evidencia). Sin orden manual ni preparación previa.",
          12.5, BODY)
    takeaway(s, "Un test que necesita una escena para correr ya dice que la dependencia está mal puesta.", P2)
    notes(s, """
50 s. La regla que ordena la suite: si algo se puede probar sin entrar en Play Mode, se prueba así.

Edit Mode cubre 80: grid y pathfinding, comandos con su cola, cálculo de stats y loadout, y el
bucle jugable completo (59). Play Mode queda en uno: el driver de P2, porque ahí lo que se verifica
es que **Unity** invoque al adaptador y que las animaciones se completen en el ciclo de vida real.
Un Edit Mode no puede demostrar eso.

Los 81 se corrieron el 04/11 y la salida está en docs/evidencia.
""")

    # ---------------------------------------------------------- 15 mutación
    s = blank(deck, next(numbers), total)
    head(s, "La prueba de que sirven", "Rompo una línea y el test se pone rojo", RED)
    grid(s, MARGIN, 1.95, CONTENT_W, [
        ["Prototipo", "Línea que rompo", "Test que falla"],
        ["P1", "Emitir el evento aunque el valor no cambie", "SetCell_WithSameValue_DoesNotRaiseChange"],
        ["P2", "Esperar la animación antes de mutar el estado", "Submit_AppliesLogicBeforeTheAnimation…"],
        ["P3", "Sumar el módulo anterior y el nuevo", "TryEquip_ReplacingModule_DoesNotDoubleCount"],
    ], [1.2, 3.3, 3.6], row_h=0.72, font=11.5, accent=RED)
    write(s, MARGIN, 4.95, CONTENT_W, 0.45,
          "Una mutación por prototipo: el rojo aparece en el test que lo declara, no en otro.",
          12.5, BODY, spacing=1.15)
    code(s, MARGIN, 5.5, CONTENT_W, [
        "# git stash  →  editar una línea  →  correr el test  →  mostrar rojo  →  git checkout .",
    ], accent=MUTED, size=11)
    takeaway(s, "Un test que nunca falló no es evidencia: es otra afirmación.", RED)
    notes(s, """
35 s. Responde "¿los tests detectan defectos reales?".

Cada prototipo tiene una línea elegida: romperla produce exactamente el rojo esperado, en el test
que lo declara. Se revierte siempre con git checkout . antes de seguir.
""")

    # ---------------------------------------------------------- 15 exclusiones
    s = blank(deck, next(numbers), total)
    head(s, "Alcance", "Esto es lo que dejé afuera, y por qué", RED)
    grid(s, MARGIN, 1.95, CONTENT_W, [
        ["Excluido", "Justificación", "Ruta de extensión"],
        ["Undo / Redo", "Capacidad habilitada, no requisito", "Historial de estados en el pipeline"],
        ["IA enemiga", "El desafío es el flujo, no la búsqueda", "El pipeline acepta cualquier ICommand"],
        ["Animación con clips", "Los modelos se mueven por transform, sin Animator", "Animator sobre el modelo actual"],
        ["Modificadores multiplicativos", "Ningún módulo del alcance los usa", "StatModifier con operación"],
        ["Mapas 3D / niebla de guerra", "Otro sistema, no exposición de estado", "Capa sobre IGridQueryService"],
        ["Unidades con turnos", "P1 ya expone la ocupación; el turno vive en P2", "Capa de turnos sobre ICommand"],
    ], [2.4, 3.2, 3.3], row_h=0.6, font=11, accent=RED)
    takeaway(s, "No son las cosas que no llegué a hacer: son las que decidí no hacer.", RED)
    notes(s, """
45 s. Cada fila tiene por dónde crecer: no son las cosas que no llegué a hacer,
son las que decidí no hacer.

Ojo: sí hay arte 3D real (modelos del Kenney Space Kit). Lo excluido es la animación
con clips; el movimiento es por transform.
""")

    # ---------------------------------------------------------- 16 cambios
    s = blank(deck, next(numbers), total)
    head(s, "Ajustes y hallazgos", "Qué cambié, y qué descubrí al implementar", P2)
    grid(s, MARGIN, 1.85, CONTENT_W, [
        ["TP1", "TP2", "Motivo"],
        ["ExecuteAsync devuelve UniTask", "ICommand sync + Task en el puerto", "El PDF admite UniTask / Task"],
        ["ShipModuleSO entra al cálculo", "El dominio recibe ModuleDefinition", "Dominio libre de UnityEngine"],
        ["FSM con estados por acción", "Tres fases", "Evitar explosión de combinaciones"],
    ], [2.8, 3.1, 2.6], row_h=0.56, font=11.5, accent=P2)

    write(s, MARGIN, 4.30, CONTENT_W, 0.26, "QUÉ DESCUBRÍ AL IMPLEMENTAR", 10.5, GREEN, bold=True)

    findings = [
        ("Un FBX mal centrado parecía un bug de arquitectura",
         "la nave se dibujaba al lado de su anillo; era el pivote del export, no el código"),
        ("transform.Find por ruta cortó el panel con un NullReference",
         "la vista no debe buscar referencias: la fábrica devuelve tipos"),
        ("[Inject] llegaba después de Start",
         "la nave 3D nunca se construía; se resuelve con un evento Ready, no con un orden implícito"),
        ("El EventSystem del template no respondía",
         "con el Input System nuevo, StandaloneInputModule es inerte: hace falta InputSystemUIInputModule"),
    ]
    for i, (what, detail) in enumerate(findings):
        cy = 4.58 + i * 0.45
        write(s, MARGIN, cy, 0.28, 0.26, "×", 12, RED, bold=True)
        write(s, MARGIN + 0.30, cy, CONTENT_W - 0.4, 0.24, what, 11.5, TEXT, bold=True)
        write(s, MARGIN + 0.30, cy + 0.21, CONTENT_W - 0.4, 0.22, detail, 9.5, MUTED)

    takeaway(s, "Tres de los siete ajustes de docs/01 §5; estos cuatro hallazgos, en el código.", P2)
    notes(s, """
45 s. Cuatro hallazgos reales, del código y de los assets.

El más útil para contar: el del pivote. Perdí tiempo buscando en la arquitectura algo que
era un problema de importación de assets. Enseña a agotar la hipótesis barata antes.

El segundo es el más arquitectónico: una vista que busca referencias por nombre de ruta
no es una vista, es una dependencia oculta. Ahora la fábrica de UI devuelve tipos.

Un quinto caso, si preguntan por los tests: una aserción mía estaba mal escrita
(la ruta costaba 3, no 4). Lo detecté corriendo la lógica fuera del editor.
Los tests también pueden estar equivocados; por eso el auditor existe.
""")

    # ---------------------------------------------------------- 17 cierre
    s = blank(deck, next(numbers), total)
    write(s, MARGIN, 1.6, CONTENT_W, 0.3, "CIERRE", 11, GREEN, bold=True)
    write(s, MARGIN, 2.05, CONTENT_W, 1.2, "Tres problemas, tres soluciones,\ntres suites que las verifican",
          36, TEXT, bold=True, spacing=1.05)

    lines = [
        "Las propiedades se demuestran: ISP por reflexión, OCP con un comando nuevo, DIP con un fake.",
        "Lo que no está, está afuera por decisión y con ruta de extensión.",
    ]
    for i, line in enumerate(lines):
        write(s, MARGIN, 3.6 + i * 0.62, CONTENT_W - 0.2, 0.5, line, 14, BODY)

    for i, (name, color) in enumerate([("P1_GridState", P1), ("P2_CommandPipeline", P2), ("P3_Loadout", P3)]):
        cx = MARGIN + i * 4.06
        rect(s, cx, 5.15, 3.82, 0.72, fill=PANEL, line=color, width=1.2)
        write(s, cx, 5.15, 3.82, 0.72, name, 12.5, color, bold=True, font=MONO,
              align=PP_ALIGN.CENTER, anchor=MSO_ANCHOR.MIDDLE)

    write(s, MARGIN, 6.25, CONTENT_W, 0.4, "81 tests verdes · 80 Edit Mode + 1 Play Mode", 13, MUTED)
    notes(s, """
20 s. Con más tiempo, el siguiente paso sería Undo/Redo, porque el pipeline ya lo habilita.
Pero eso es otro prototipo, no una deuda escondida.
""")

    # ---------------------------------------------------------- 20 backup contratos
    s = blank(deck, 0, 0)
    head(s, "Respaldo · Si preguntan", "Contratos y decisiones de código", P1)
    grid(s, MARGIN, 1.95, CONTENT_W, [
        ["Pregunta", "Respuesta"],
        ["¿Cuándo usás excepciones?", "Solo precondiciones rotas (catálogo null). Un estado inválido es un resultado con motivo."],
        ["¿Por qué no null para ausencia?", "TryGetModule(out …) obliga a manejarla; null explota lejos del origen."],
        ["¿Quién puede mutar?", "GridModel el mapa, CommandProcessor la nave, EquipmentService el loadout. Nadie más."],
        ["¿Por qué estructuras inmutables?", "Permiten comparar antes/después y razonar sobre undo sin deriva."],
    ], [2.5, 6.0], row_h=0.82, font=11.5, accent=P1)
    takeaway(s, "Cada respuesta es una decisión de código verificable, no una preferencia de estilo.", P1)
    notes(s, """
Respaldo: contratos, null, mutabilidad, transparencia referencial y excepciones.
""")

    # ---------------------------------------------------------- 21 backup cómo correr
    s = blank(deck, 0, 0)
    head(s, "Respaldo · Si preguntan", "Cómo se reproduce y se verifica, sin abrir el editor", P2)
    code(s, MARGIN, 1.95, CONTENT_W, [
        "# Escenas: se generan solas si faltan ([InitializeOnLoad]) o por menú",
        'unity command --project-path . menu --path "VoidHex/Build Prototype Scenes"',
        "",
        "# Suite completa contra el editor en ejecución",
        "unity command --project-path . run_tests --mode EditMode",
        "unity command --project-path . run_tests --mode PlayMode --async_tests",
        "",
        "# Evidencia visual de las tres escenas",
        'unity command --project-path . capture_game_view --source screen --save_path captura.png',
    ], accent=GREEN, size=11.5, line_h=0.26)
    write(s, MARGIN, 5.05, CONTENT_W, 0.85,
          "Tres verificadores propios: referencias de asmdef, integridad de escenas y auditoría de la lógica pura sin Unity.",
          13, BODY, spacing=1.15)
    takeaway(s, "Unity 6000.5.9f1 trae el paquete com.unity.pipeline: el editor se controla por CLI.", GREEN)
    notes(s, """
El CLI (com.unity.pipeline) permite recompilar, ejecutar menús, correr tests y capturar
el game view con el editor abierto. Es lo que se usó para verificar cada cambio de esta entrega.

Si preguntan por batchmode: también existe, pero requiere el proyecto sin lock, es decir
el editor cerrado. El CLI no.
""")

    deck.save(PPTX_PATH)
    return deck


# ---------------------------------------------------------------- png assets


def build_png_assets():
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    from matplotlib.patches import FancyArrowPatch, FancyBboxPatch

    plt.rcParams["font.family"] = "DejaVu Sans"

    bg = "#0F172A"
    panel = "#1E293B"
    border = "#334155"
    text = "#F8FAFC"
    body = "#CBD5E1"
    muted = "#94A3B8"
    c1, c2, c3 = "#2DD4BF", "#A78BFA", "#FBBF24"
    green = "#4ADE80"
    red = "#F87171"

    def canvas(x_max, y_max):
        """Sistema de coordenadas explícito: 0..x_max por 0..y_max, sin clipping."""
        scale = 6.6 / x_max
        fig, ax = plt.subplots(figsize=(6.6, y_max * scale), dpi=200)
        fig.patch.set_facecolor(bg)
        ax.set_facecolor(bg)
        ax.set_xlim(0, x_max)
        ax.set_ylim(0, y_max)
        ax.axis("off")
        fig.subplots_adjust(left=0.01, right=0.99, top=0.99, bottom=0.01)
        return fig, ax

    def box(ax, x, y, w, h, title, detail, color, *, size=10, dsize=7.5, label_color=None):
        ax.add_patch(FancyBboxPatch((x, y), w, h, boxstyle="round,pad=0.02,rounding_size=0.08",
                                    facecolor=panel, edgecolor=color, linewidth=1.6))
        ax.text(x + w / 2, y + h * 0.63, title, ha="center", va="center",
                color=label_color or color, fontsize=size, fontweight="bold")
        if detail:
            ax.text(x + w / 2, y + h * 0.27, detail, ha="center", va="center",
                    color=muted, fontsize=dsize)

    def arr(ax, p1, p2, color=muted, lw=1.5, dash=False):
        ax.add_patch(FancyArrowPatch(p1, p2, arrowstyle="-|>", mutation_scale=13,
                                     color=color, linewidth=lw,
                                     linestyle="--" if dash else "-",
                                     shrinkA=0, shrinkB=0))

    # 1 P1  ·  x 0..10  y 0..7.2
    fig, ax = canvas(10, 7.2)
    box(ax, 3.4, 3.35, 3.2, 1.3, "GridModel", "único dueño del estado", c1, size=13, dsize=8.5)
    box(ax, 0.25, 5.6, 3.2, 1.3, "IGridQueryService", "lectura síncrona", border, size=10, dsize=8,
        label_color=text)
    box(ax, 6.55, 5.6, 3.2, 1.3, "IGridStatePublisher", "eventos C#", border, size=10, dsize=8,
        label_color=text)
    box(ax, 0.25, 0.95, 3.2, 1.3, "Pathfinding / IA", "no puede mutar", c1, size=10.5, dsize=8)
    box(ax, 6.55, 0.95, 3.2, 1.3, "GridPresenter", "no puede alterar reglas", c1, size=10.5, dsize=8)

    # GridModel implementa ambas interfaces (la flecha apunta a la interfaz)
    arr(ax, (4.15, 4.67), (2.35, 5.58), muted, 1.6)
    arr(ax, (5.85, 4.67), (7.65, 5.58), muted, 1.6)
    # los consumidores dependen de una sola interfaz cada uno
    arr(ax, (1.85, 2.27), (1.85, 5.58), c1, 1.8)
    arr(ax, (8.15, 2.27), (8.15, 5.58), c1, 1.8)

    ax.text(5.0, 0.4, "las mutaciones NO están en ninguna de las dos interfaces",
            ha="center", va="center", color=muted, fontsize=9, style="italic")
    fig.savefig(ASSET_DIR / "diagrama-p1-isp-observer.png", facecolor=bg)
    plt.close(fig)

    # 2 P2  ·  x 0..11  y 0..5
    fig, ax = canvas(11, 5.0)
    steps = [("ICommand", "inmutable", c2), ("Validate", "¿PA / objetivo?", c2),
             ("Execute", "instantáneo", green), ("Notifica", "ya cambiado", green),
             ("await vista", "animación", border)]
    bw, gap, y0, bh = 1.78, 0.29, 2.55, 1.3
    for i, (name, detail, color) in enumerate(steps):
        x = 0.45 + i * (bw + gap)
        label = text if color is border else None
        box(ax, x, y0, bw, bh, name, detail, color, size=10, dsize=7.5, label_color=label)
        if i < len(steps) - 1:
            arr(ax, (x + bw + 0.03, y0 + bh / 2), (x + bw + gap - 0.03, y0 + bh / 2), muted)

    vx = 0.45 + (bw + gap) + bw / 2          # centro de Validate
    arr(ax, (vx, y0 - 0.04), (vx, 1.78), red, 1.6)
    ax.add_patch(FancyBboxPatch((vx - 1.55, 1.02), 3.1, 0.72,
                                boxstyle="round,pad=0.02,rounding_size=0.08",
                                facecolor="#2E1515", edgecolor=red, linewidth=1.4))
    ax.text(vx, 1.38, "rechazo con motivo", ha="center", va="center",
            color=red, fontsize=9, fontweight="bold")
    ax.text(5.5, 0.42, "la regla se aplica al instante · la animación espera después",
            ha="center", va="center", color=muted, fontsize=9, style="italic")
    fig.savefig(ASSET_DIR / "diagrama-p2-pipeline.png", facecolor=bg)
    plt.close(fig)

    # 3 P3  ·  x 0..10  y 0..6.6
    fig, ax = canvas(10, 6.6)
    box(ax, 0.3, 3.6, 3.3, 1.45, "EquipmentLifetimeScope", "composición · 1 por escena",
        c3, size=9.5, dsize=7.5)
    box(ax, 5.85, 5.05, 3.85, 1.15, "ModuleCatalogAsset", "Flyweight: datos compartidos", c2,
        size=10, dsize=7.5)
    box(ax, 5.85, 3.6, 3.85, 1.15, "ShipConfig", "estadísticas base", border, size=10, dsize=7.5,
        label_color=text)
    box(ax, 5.85, 2.15, 3.85, 1.15, "IEquipmentService", "contrato inyectado", green, size=10, dsize=7.5)
    box(ax, 3.15, 0.45, 3.7, 1.15, "LoadoutDemo  [Inject]", "no busca: recibe", green,
        size=10, dsize=7.5)

    for cy in (5.62, 4.17, 2.72):
        arr(ax, (3.62, 4.32), (5.83, cy), muted, 1.5)
    arr(ax, (5.0, 1.62), (5.0, 2.13), green, 1.6, dash=True)
    ax.text(4.86, 1.88, "resuelve", ha="right", va="center", color=green, fontsize=8.5)

    fig.savefig(ASSET_DIR / "diagrama-p3-di.png", facecolor=bg)
    plt.close(fig)

    # 4 OCP  ·  x 0..10  y 0..4.2
    fig, ax = canvas(10, 4.2)
    names = [("MoveCommand", c2, "implementado"), ("AttackCommand", c2, "implementado"),
             ("ShieldCommand", c2, "implementado"), ("TractorBeam", green, "Command · en vivo")]
    bw, gap = 2.15, 0.3
    for i, (name, color, tag) in enumerate(names):
        x = 0.3 + i * (bw + gap)
        box(ax, x, 2.35, bw, 1.05, name, tag, color, size=8.8, dsize=7)
        arr(ax, (x + bw / 2, 2.33), (x + bw / 2, 1.82), color, 1.4)

    ax.add_patch(FancyBboxPatch((0.3, 0.85), 9.4, 0.95,
                                boxstyle="round,pad=0.02,rounding_size=0.08",
                                facecolor=panel, edgecolor=green, linewidth=2.0))
    ax.text(5.0, 1.32, "ICommand  →  CommandProcessor   (no cambia)",
            ha="center", va="center", color=green, fontsize=11.5, fontweight="bold")
    fig.savefig(ASSET_DIR / "diagrama-p2-ocp.png", facecolor=bg)
    plt.close(fig)


def main():
    deck = build()
    build_png_assets()

    print(f"PPTX : {PPTX_PATH}")
    print(f"Slides: {len(deck.slides)}")
    print(f"SIZE : {os.path.getsize(PPTX_PATH) / 1024:.0f} KB")
    for png in sorted(ASSET_DIR.glob("*.png")):
        print(f"PNG  : {png.name}  ({png.stat().st_size / 1024:.0f} KB)")


if __name__ == "__main__":
    main()
