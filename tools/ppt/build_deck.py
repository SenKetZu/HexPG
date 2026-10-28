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


def card(slide, x, y, w, h, title, lines, accent, *, body_size=12.5, title_size=13):
    rect(slide, x, y, w, h)
    write(slide, x + 0.26, y + 0.2, w - 0.5, 0.3, title, title_size, accent, bold=True)
    box = textbox(slide, x + 0.26, y + 0.62, w - 0.5, h - 0.85)
    tf = box.text_frame
    for i, line in enumerate(lines):
        p = tf.paragraphs[0] if i == 0 else tf.add_paragraph()
        p.space_after = Pt(7)
        p.line_spacing = 1.05
        run = p.add_run()
        run.text = f"·  {line}"
        style_run(run, body_size, BODY)


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


def diagram_layers(slide, x, y, w):
    labels = [
        ("Presentación", "MonoBehaviour · VContainer · Assets SO", P1),
        ("Aplicación", "Pipeline · Servicio de equipamiento", P2),
        ("Dominio", "Reglas · Contratos · Cálculo puro", P3),
    ]
    bw, bh = w * 0.62, 0.92
    cx = x + (w - bw) / 2
    for i, (name, detail, color) in enumerate(labels):
        cy = y + i * (bh + 0.42)
        rect(slide, cx, cy, bw, bh, fill=PANEL, line=color, radius=0.08, width=1.5)
        write(slide, cx, cy + 0.16, bw, 0.3, name, 14.5, color, bold=True, align=PP_ALIGN.CENTER)
        write(slide, cx, cy + 0.5, bw, 0.28, detail, 10.5, MUTED, align=PP_ALIGN.CENTER)
        if i < len(labels) - 1:
            arrow(slide, x + w / 2, cy + bh + 0.05, x + w / 2, cy + bh + 0.37, MUTED, width=1.5)


def diagram_p1(slide, x, y, w, h):
    mid = x + w / 2
    rect(slide, mid - 1.15, y + h / 2 - 0.44, 2.3, 0.88, fill=PANEL_HI, line=P1, width=1.5, radius=0.1)
    write(slide, mid - 1.15, y + h / 2 - 0.36, 2.3, 0.32, "GridModel", 14, P1, bold=True, align=PP_ALIGN.CENTER)
    write(slide, mid - 1.15, y + h / 2 + 0.0, 2.3, 0.3, "único dueño del estado", 9.5, MUTED,
          align=PP_ALIGN.CENTER)

    left_x, right_x = x + 0.05, x + w - 2.55
    rect(slide, left_x, y + 0.05, 2.5, 0.95, fill=PANEL, line=BORDER)
    write(slide, left_x + 0.14, y + 0.17, 2.22, 0.3, "IGridQueryService", 11.5, TEXT, bold=True)
    write(slide, left_x + 0.14, y + 0.5, 2.22, 0.4, "lectura síncrona", 10, MUTED)

    rect(slide, right_x, y + 0.05, 2.5, 0.95, fill=PANEL, line=BORDER)
    write(slide, right_x + 0.14, y + 0.17, 2.22, 0.3, "IGridStatePublisher", 11.5, TEXT, bold=True)
    write(slide, right_x + 0.14, y + 0.5, 2.22, 0.4, "eventos C#", 10, MUTED)

    ia_y = y + h - 0.98
    rect(slide, left_x, ia_y, 2.5, 0.85, fill=RGBColor(0x14, 0x2B, 0x2E), line=P1, width=1.2)
    write(slide, left_x + 0.14, ia_y + 0.12, 2.22, 0.3, "Pathfinding / IA", 11.5, P1, bold=True)
    write(slide, left_x + 0.14, ia_y + 0.44, 2.22, 0.3, "no puede mutar", 9.5, MUTED)

    ui_y = y + h - 0.98
    rect(slide, right_x, ui_y, 2.5, 0.85, fill=RGBColor(0x14, 0x2B, 0x2E), line=P1, width=1.2)
    write(slide, right_x + 0.14, ui_y + 0.12, 2.22, 0.3, "GridPresenter", 11.5, P1, bold=True)
    write(slide, right_x + 0.14, ui_y + 0.44, 2.22, 0.3, "no puede alterar reglas", 9.5, MUTED)

    arrow(slide, left_x + 1.25, ia_y - 0.02, left_x + 1.25, y + 1.06, P1)
    arrow(slide, right_x + 1.25, ia_y - 0.02, right_x + 1.25, y + 1.06, P1)
    arrow(slide, mid - 1.15, y + h / 2, left_x + 2.52, y + h / 2, MUTED)
    arrow(slide, mid + 1.15, y + h / 2, right_x - 0.02, y + h / 2, MUTED)


def diagram_p2(slide, x, y, w):
    steps = [
        ("ICommand", "inmutable", P2),
        ("Validate", "¿PA / objetivo?", P2),
        ("Execute", "instantáneo", GREEN),
        ("Notifica", "estado ya cambiado", GREEN),
        ("await vista", "animación", MUTED),
    ]
    bw = (w - 0.34 * (len(steps) - 1)) / len(steps)
    for i, (name, detail, color) in enumerate(steps):
        cx = x + i * (bw + 0.34)
        rect(slide, cx, y, bw, 0.92, fill=PANEL, line=color, width=1.4)
        write(slide, cx, y + 0.16, bw, 0.3, name, 12.5, color, bold=True, align=PP_ALIGN.CENTER)
        write(slide, cx, y + 0.5, bw, 0.28, detail, 9.5, MUTED, align=PP_ALIGN.CENTER)
        if i < len(steps) - 1:
            arrow(slide, cx + bw + 0.03, y + 0.46, cx + bw + 0.31, y + 0.46, MUTED)

    # rechazo
    rx = x + bw + 0.17 - 1.35
    arrow(slide, x + bw * 1.5 + 0.34, y + 0.94, x + bw * 1.5 + 0.34, y + 1.34, RED)
    rect(slide, x + bw + 0.05, y + 1.36, 2.25, 0.55, fill=RGBColor(0x2E, 0x15, 0x15), line=RED, width=1.0)
    write(slide, x + bw + 0.05, y + 1.46, 2.25, 0.3, "rechazo con motivo", 10.5, RED,
          align=PP_ALIGN.CENTER)


def diagram_p3(slide, x, y, w, h):
    sc = (x + 0.1, y + 0.05, 2.55, 1.0)
    rect(slide, *sc, fill=PANEL_HI, line=P3, width=1.5, radius=0.08)
    write(slide, sc[0] + 0.16, sc[1] + 0.14, sc[2] - 0.3, 0.3, "EquipmentLifetimeScope", 11, P3, bold=True)
    write(slide, sc[0] + 0.16, sc[1] + 0.48, sc[2] - 0.3, 0.28, "composición · 1 por escena", 9, MUTED)

    targets = [
        ("ModuleCatalogAsset", "Flyweight", P2),
        ("ShipConfig", "stats base", MUTED),
        ("IEquipmentService", "contrato", GREEN),
    ]
    tx = x + w - 3.0
    for i, (name, detail, color) in enumerate(targets):
        ty = y + 0.05 + i * 0.86
        rect(slide, tx, ty, 2.95, 0.72, fill=PANEL, line=color, width=1.2)
        write(slide, tx + 0.16, ty + 0.1, 2.6, 0.28, name, 10.5, color, bold=True)
        write(slide, tx + 0.16, ty + 0.4, 2.6, 0.26, detail, 9, MUTED)
        arrow(slide, sc[0] + sc[2] + 0.05, sc[1] + 0.5, tx - 0.05, ty + 0.36, BORDER)

    dy = y + h - 0.92
    rect(slide, x + w / 2 - 1.5, dy, 3.0, 0.78, fill=RGBColor(0x1A, 0x24, 0x18), line=GREEN, width=1.2)
    write(slide, x + w / 2 - 1.5, dy + 0.12, 3.0, 0.28, "LoadoutDemo  [Inject]", 10.5, GREEN,
          bold=True, align=PP_ALIGN.CENTER)
    write(slide, x + w / 2 - 1.5, dy + 0.44, 3.0, 0.26, "no busca: recibe", 9, MUTED, align=PP_ALIGN.CENTER)


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

    total = 21
    deck = new_deck()

    # ---------------------------------------------------------- 1 portada
    s = blank(deck, 1, total)
    write(s, MARGIN, 1.55, CONTENT_W, 0.3, "PROGRAMACIÓN DE VIDEOJUEGOS III · 2026", 12, P1, bold=True)
    write(s, MARGIN, 2.0, CONTENT_W, 1.3, "Tres decisiones de arquitectura,\nverificadas con tests", 40,
          TEXT, bold=True, spacing=1.05)
    write(s, MARGIN, 3.55, CONTENT_W, 0.4, "Void Hex Tactics · TP2", 20, P2, bold=True)

    chips = [("P1_GridState", "ISP + Observer · 5 tests", P1),
             ("P2_CommandPipeline", "Pipeline async · 8 tests", P2),
             ("P3_Loadout", "DI + Data-driven · 9 tests", P3)]
    for i, (folder, tag, color) in enumerate(chips):
        cx = MARGIN + i * 4.06
        rect(s, cx, 4.55, 3.82, 1.05, fill=PANEL, line=color, width=1.3)
        write(s, cx + 0.22, 4.7, 3.4, 0.3, folder, 12, TEXT, bold=True, font=MONO)
        write(s, cx + 0.22, 5.05, 3.4, 0.3, tag, 10.5, color, bold=True)

    write(s, MARGIN, 6.15, CONTENT_W, 0.6,
          "Matias Andres Fahler   ·   Unity 6000.5.9f1   ·   tag tp2   ·   81 tests verdes", 13, MUTED)
    notes(s, """
15 s. No voy a mostrar un juego. Voy a mostrar tres problemas técnicos del TP1,
la solución declarada para cada uno, y la evidencia de que funciona.

Tiempo total objetivo: 18-19 minutos, dejando margen para preguntas.
""")

    # ---------------------------------------------------------- 2 mapa
    s = blank(deck, 2, total)
    head(s, "Qué entrego", "No es un juego: son tres decisiones verificables", P1)
    grid(s, MARGIN, 1.95, CONTENT_W, [
        ["Desafío declarado en el TP1", "Solución declarada", "Prototipo"],
        ["Exponer el mapa sin God Object ni polling en Update()", "ISP + Observer", "P1_GridState"],
        ["Alternar PA con animaciones asíncronas", "Pipeline ICommand + async", "P2_CommandPipeline"],
        ["Servicios sin singletons + módulos MK I–III", "VContainer + ScriptableObjects", "P3_Loadout"],
    ], [3.4, 2.6, 2.0], row_h=0.78, font=13, accent=P1)
    write(s, MARGIN, 5.2, CONTENT_W, 0.7,
          "Una escena real por prototipo, con modelos 3D (Kenney CC0) y UI nativa: se puede jugar la prueba.",
          13, MUTED)
    takeaway(s, "La consigna pide 3 pruebas de concepto pulidas, no un juego completo.", P1)
    notes(s, """
45 s. El TP2 no cambia el diagnóstico del TP1: cambia que ahora está implementado y testeado.

Si preguntan "¿por qué no un juego completo?": la consigna (§4.1) pide 3 pruebas de concepto
técnicamente pulidas. Agregar contenido no demuestra ninguna propiedad de diseño.
""")

    # ---------------------------------------------------------- 3 capas
    s = blank(deck, 3, total)
    head(s, "Regla de dependencia", "La arquitectura la impone el compilador, no la disciplina", P2)
    diagram_layers(s, MARGIN, 2.0, 4.6)
    code(s, 6.05, 2.0, 5.6, [
        "{",
        '  "name": "VoidHex.P2.Domain",',
        '  "references": [],',
        '  "noEngineReferences": true',
        "}",
        "",
        "// using UnityEngine;  →  no compila",
    ], accent=P2, size=13)
    write(s, 6.05, 4.5, 5.6, 1.1,
          "Podría prometer que el dominio es independiente del motor.\nMejor: lo demuestro rompiendo una línea en vivo.",
          13, BODY, spacing=1.15)
    takeaway(s, "Si alguien agrega using UnityEngine en el dominio, el proyecto no compila. Sin excepciones.", P2)
    notes(s, """
60 s. Esta regla es la base de los tres prototipos.

Demo D2 (opcional): escribir using UnityEngine; en un archivo del dominio y mostrar el error.
Plan B: captura del error ya tomada.
""")

    # ---------------------------------------------------------- 4 P1 problema
    s = blank(deck, 4, total)
    head(s, "P1 · Desafío 1", "Exponer el mapa sin God Object ni polling en Update()", P1)
    grid(s, MARGIN, 1.95, CONTENT_W, [
        ["Lo que NO quiero", "Por qué es un problema"],
        ["Un GridManager que sabe todo", "God Object: nadie sabe quién muta el estado"],
        ["Update() consultando el mapa", "CPU por frame y acoplamiento al motor"],
        ["Un Event Bus global", "Dependencias ocultas, imposible de depurar"],
    ], [2.3, 3.9], row_h=0.72, font=13.5, accent=RED)
    write(s, MARGIN, 4.9, CONTENT_W, 1.2,
          "Tres consumidores, tres necesidades:\nla IA lee · los comandos validan · la UI dibuja.",
          15, BODY, spacing=1.2)
    notes(s, """
45 s. Si les doy el mismo objeto a los tres, la UI puede cambiar reglas
y la IA termina escuchando eventos visuales. Ese es el problema real.
""")

    # ---------------------------------------------------------- 5 P1 solución
    s = blank(deck, 5, total)
    head(s, "P1 · Solución", "Dos contratos: ningún consumidor puede hacer lo del otro", P1)
    diagram_p1(s, MARGIN, 1.95, CONTENT_W, 3.35)
    write(s, MARGIN, 5.55, CONTENT_W, 0.9,
          "Solo GridModel muta, y esos métodos no están en ningún contrato: es el tipo del constructor, no una convención.",
          13, BODY)
    takeaway(s, "El Pathfinder recibe IGridQueryService. Aunque quisiera, no puede escribir el mapa.", P1)
    notes(s, """
60 s. Insistir: no es una convención de estilo. Es el tipo del constructor.

Si preguntan "¿y si alguien castea?": tendría que romper una capa explícitamente,
y el asmdef lo impide hacia el dominio.
""")

    # ---------------------------------------------------------- 6 P1 evidencia
    s = blank(deck, 6, total)
    head(s, "P1 · Evidencia", "Reactivo de verdad: sin mutación real, no hay trabajo", P1)

    stats = [("1", "evento", "por cada mutación real", GREEN),
             ("0", "eventos", "al reescribir el mismo valor", TEXT),
             ("4", "redibujos", "en un mapa 12×12 con 4 obstáculos", MUTED)]
    for i, (big, label, detail, color) in enumerate(stats):
        cx = MARGIN + i * 4.06
        rect(s, cx, 1.95, 3.82, 1.62, fill=PANEL, line=BORDER)
        write(s, cx + 0.26, 2.02, 3.3, 0.7, big, 40, color, bold=True)
        write(s, cx + 0.26, 2.72, 3.3, 0.3, label, 13, TEXT, bold=True)
        write(s, cx + 0.26, 3.05, 3.3, 0.3, detail, 10.5, MUTED)

    tests = ["IGridStatePublisher_ExposesOnlyEvents",
             "IGridQueryService_ExposesNoMutatingMembers",
             "SetCell_WithSameValue_DoesNotRaiseChange"]
    for i, name in enumerate(tests):
        ty = 3.95 + i * 0.44
        write(s, MARGIN, ty, 0.3, 0.3, "✓", 14, GREEN, bold=True)
        write(s, MARGIN + 0.32, ty, CONTENT_W - 0.4, 0.32, name, 12.5, BODY, font=MONO)

    takeaway(s, "Los dos primeros no prueban comportamiento: prueban forma, por reflexión.", P1)
    notes(s, """
60 s. Escena real: mapa 12×12 con asteroides y cristales del Kenney Space Kit.
La ruta al destino es de 21 celdas y costo 20; el contador de eventos queda en 4,
que es exactamente la cantidad de obstáculos colocados.

Los dos primeros tests usan reflexión: verifican que el contrato de publicación solo
tenga eventos y que el de lectura no tenga mutaciones. Si mañana alguien agrega SetCell
a la interfaz de lectura, el test se pone rojo antes de que llegue a la UI.
""")

    # ---------------------------------------------------------- 7 P1 alcance
    s = blank(deck, 7, total)
    head(s, "P1 · Alcance", "El A* es el sustrato, no la tesis", P1)
    write(s, MARGIN, 1.95, CONTENT_W, 0.95,
          "El informe largo del TP1 desarrolla grilla y pathfinding; el desafío declarado es la exposición del estado.",
          15, BODY, spacing=1.2)
    grid(s, MARGIN, 3.05, CONTENT_W, [
        ["Fuera de alcance", "Justificación"],
        ["Mapas 3D multicapa", "El desafío es de exposición de estado, no de geometría"],
        ["Niebla de guerra", "Es un sistema de conocimiento, ajeno a la exposición"],
        ["Unidades como bloqueos", "Necesita el modelo de turnos, que es P2"],
        ["Event Bus global", "Descartado en el TP1: dependencias ocultas"],
    ], [2.2, 5.4], row_h=0.62, font=12.5, accent=RED)
    takeaway(s, "Cada exclusión tiene una ruta de extensión documentada, no es un olvido.", P1)
    notes(s, """
45 s. ESTA SLIDE ES LA QUE CORRIGE EL MALENTENDIDO DEL TP1. No la saltees.

Si el jurado espera "problema 1 = matemática hexagonal" y ve ISP,
sin esta slide parece que me fui del tema. Con esta slide, es una decisión fundamentada.
""")

    # ---------------------------------------------------------- 8 P2 problema
    s = blank(deck, 8, total)
    head(s, "P2 · Desafío 2", "Mover → Atacar → Mover, sin que la animación decida las reglas", P2)
    grid(s, MARGIN, 1.95, CONTENT_W, [
        ["Lo que NO quiero", "Por qué es un problema"],
        ["TurnManager con corrutinas", "WaitForSeconds mete el tiempo del arte dentro de la regla"],
        ["FSM con un estado por acción", "Explota en combinaciones al agregar módulos"],
        ["Un comando por cada orden", "Obliga a tocar el controlador central cada vez"],
    ], [2.3, 4.6], row_h=0.72, font=13, accent=RED)
    write(s, MARGIN, 4.9, CONTENT_W, 1.1,
          "La pregunta difícil no es “¿puede moverse?”.\nEs cuándo se aplica el efecto: antes o después de la animación.",
          15, BODY, spacing=1.2)
    notes(s, """
40 s. El jugador puede intercalar acciones. El riesgo real es que el tiempo del arte
termine gobernando la regla del juego.
""")

    # ---------------------------------------------------------- 9 P2 solución
    s = blank(deck, 9, total)
    head(s, "P2 · Solución", "La regla se aplica al instante, la animación espera después", P2)
    diagram_p2(s, MARGIN, 2.05, CONTENT_W)
    items = [
        ("Único dueño del estado", "CommandProcessor. Nadie más lo reemplaza."),
        ("Cola serializada", "Dos envíos seguidos no se intercalan."),
        ("Solo tres fases", "PlayerTurn · Executing · EnemyTurn."),
    ]
    for i, (title, detail) in enumerate(items):
        cx = MARGIN + i * 4.06
        rect(s, cx, 4.3, 3.82, 1.15, fill=PANEL, line=BORDER)
        write(s, cx + 0.24, 4.44, 3.4, 0.3, title, 12.5, P2, bold=True)
        write(s, cx + 0.24, 4.8, 3.4, 0.5, detail, 10.5, BODY)
    takeaway(s, "Cuando el pipeline vuelve de Execute, el estado ya cambió. La animación no puede condicionarlo.", P2)
    notes(s, """
70 s. Mirar el orden del diagrama: Validate → Execute → notifica → await.

Si el jugador pulsa dos veces, el segundo comando espera en la cola:
no hay estado a medio aplicar.
""")

    # ---------------------------------------------------------- 10 P2 evidencia
    s = blank(deck, 10, total)
    head(s, "P2 · Evidencia", "El estado cambia antes de que termine la animación", P2)

    rect(s, MARGIN, 1.95, CONTENT_W, 0.82, fill=PANEL_HI, line=P2, width=1.4, radius=0.1)
    write(s, MARGIN + 0.3, 1.95, CONTENT_W - 0.6, 0.82,
          "Submit_AppliesLogicBeforeTheAnimationCompletes", 15, P2, bold=True, font=MONO,
          anchor=MSO_ANCHOR.MIDDLE)

    checks = [("1", "Posición y PA ya cambiaron", "el efecto lógico está aplicado"),
              ("2", "Phase == Executing", "la animación está en curso"),
              ("3", "El Task sigue pendiente", "todavía no terminó")]
    for i, (n, title, detail) in enumerate(checks):
        cy = 3.05 + i * 0.86
        write(s, MARGIN, cy, 0.45, 0.4, n, 18, P2, bold=True)
        write(s, MARGIN + 0.5, cy + 0.02, 5.0, 0.32, title, 14, TEXT, bold=True)
        write(s, MARGIN + 0.5, cy + 0.34, 5.0, 0.3, detail, 10.5, MUTED)

    rect(s, 7.1, 3.0, 5.6, 1.05, fill=PANEL, line=GREEN, width=1.2)
    write(s, 7.32, 3.14, 5.2, 0.3, "Segundo test", 10.5, GREEN, bold=True)
    write(s, 7.32, 3.42, 5.2, 0.6, "Submit_SecondCommand_QueuesUntil\nTheFirstAnimationCompletes",
          11, BODY, font=MONO)

    # La corrida real de la demo, no un ejemplo genérico.
    rect(s, 7.1, 4.25, 5.6, 1.62, fill=PANEL_HI, line=BORDER, width=1.1)
    write(s, 7.32, 4.38, 5.2, 0.28, "CORRIDA REAL DE LA ESCENA", 10.5, P2, bold=True)
    write(s, 7.32, 4.68, 5.2, 0.9,
          "Mover (2,0) 2 PA → Atacar 2 PA → Mover (3,0) 1 PA", 11, BODY, font=MONO)
    write(s, 7.32, 5.02, 5.2, 0.3, "PA 6 → 1   ·   hull enemigo 6 → 3", 11, GREEN, bold=True)

    takeaway(s, "Si alguien espera la animación antes de mutar el estado, este test falla.", P2)
    notes(s, """
60 s. Este es el test que demuestra el desacoplamiento temporal.

Pregunta que invita: "¿qué error detectaría?"
→ acoplar la regla al tiempo de la animación, o intercalar dos comandos y corromper el estado.
""")

    # ---------------------------------------------------------- 11 P2 OCP
    s = blank(deck, 11, total)
    head(s, "P2 · Extensibilidad", "Agregar una habilidad es crear una clase, no modificar el sistema", P2)
    diagram_ocp(s, MARGIN, 2.0, CONTENT_W)
    write(s, MARGIN, 4.15, CONTENT_W, 0.4, "✓  Submit_NewCommandVariant_RequiresNoPipelineChange", 13,
          GREEN, font=MONO)
    write(s, MARGIN, 4.75, CONTENT_W, 0.95,
          "Diferido: Undo/Redo. El TP1 lo menciona como capacidad que el patrón habilita, no como requisito del prototipo.",
          13, BODY, spacing=1.15)
    takeaway(s, "ShieldCommand ya existe y el pipeline no se enteró.", P2)
    notes(s, """
50 s. En la escena, ShieldCommand cuesta 2 PA y suma 4 de escudo: el pipeline no cambió.

Demo D4: agregar TractorBeamCommand en vivo. Valida PA, mueve la nave enemiga,
y CommandProcessor no cambia una línea.
""")

    # ---------------------------------------------------------- 12 P3 problema
    s = blank(deck, 12, total)
    head(s, "P3 · Desafío 3", "Sin GameManager.Instance y sin Service Locator", P3)
    grid(s, MARGIN, 1.95, CONTENT_W, [
        ["Lo que NO quiero", "Por qué es un problema"],
        ["GameManager.Instance", "Dependencias ocultas y estado global mutable"],
        ["Service Locator", "Mismo problema con otro nombre: la clase pide, no recibe"],
        ["Stats calculadas dentro del SO", "El cálculo queda atado al motor y no se puede testear"],
    ], [2.5, 4.7], row_h=0.72, font=13, accent=RED)
    write(s, MARGIN, 4.9, CONTENT_W, 1.0,
          "El síntoma es fácil de ver: si para testear tengo que abrir una escena, la dependencia está mal puesta.",
          15, BODY, spacing=1.2)
    notes(s, """
40 s. Ese es el diagnóstico en una frase: si el test necesita una escena,
la dependencia está mal puesta.
""")

    # ---------------------------------------------------------- 13 P3 solución
    s = blank(deck, 13, total)
    head(s, "P3 · Solución", "Dependencias por constructor, datos como assets", P3)
    diagram_p3(s, MARGIN, 1.95, CONTENT_W, 3.35)
    write(s, MARGIN, 5.55, CONTENT_W, 0.95,
          "El contenedor vive en Presentación. El servicio ni sabe que existe VContainer: recibe una interfaz.",
          13, BODY, spacing=1.15)
    takeaway(s, "ScriptableObject es adaptador, no modelo: el dominio recibe ModuleDefinition.", P3)
    notes(s, """
60 s. El LifetimeScope es composición: registra catálogo, config y servicio.
LoadoutDemo no busca nada, recibe IEquipmentService por [Inject].

Eso es lo que permite testear el servicio sin escena.
""")

    # ---------------------------------------------------------- 14 P3 evidencia
    s = blank(deck, 14, total)
    head(s, "P3 · Evidencia", "Testeable sin Unity y ampliable sin recompilar", P3)

    card(s, MARGIN, 1.95, 5.95, 2.15, "DIP: catálogo inyectado", [
        "EquipmentService recibe IModuleCatalog",
        "El test inyecta FakeCatalog y cuenta consultas",
        "Corre en Edit Mode, sin escena",
    ], P3)
    card(s, MARGIN + 6.18, 1.95, 5.95, 2.15, "Data-driven: sin código", [
        "laser-mk1 → laser-mk2 reemplaza en el slot",
        "laser-mk3 se agregó como asset, no como clase",
        "Stats: PA 6 · escudos 5 · movimiento 4",
    ], GREEN)

    for i, name in enumerate(["TryEquip_ReplacingModule_DoesNotDoubleCount",
                              "EquipmentService_WithInjectedFakeCatalog_ResolvesModulesById"]):
        ty = 4.35 + i * 0.46
        write(s, MARGIN, ty, 0.3, 0.3, "✓", 13, GREEN, bold=True)
        write(s, MARGIN + 0.32, ty, CONTENT_W - 0.4, 0.32, name, 11.5, BODY, font=MONO)

    takeaway(s, "Un slot por tipo: el estado inválido “módulo en slot incompatible” no puede existir.", P3)
    notes(s, """
60 s. La escena equipa laser-mk1, lo reemplaza por laser-mk2, suma thruster-mk1 y shield-mk1.
Resultado real: PA 6, escudos 5, movimiento 4, habilidades [beam, barrier].

Columna izquierda demuestra DIP: si el catálogo viene de un servidor, el servicio no cambia.
Columna derecha demuestra data-driven: el MK III existe como asset, sin una clase nueva.

Demos: D5 (reemplazo MK I → MK II) y D6 (equipar el MK III).
""")

    # ---------------------------------------------------------- 15 trazabilidad
    s = blank(deck, 15, total)
    head(s, "Trazabilidad", "Cada comportamiento tiene un test y un defecto declarado", TEXT)
    grid(s, MARGIN, 1.95, CONTENT_W, [
        ["Comportamiento", "Test", "Error que detectaría"],
        ["El evento solo se emite si hay mutación real", "SetCell_WithSameValue_…", "Polling disfrazado de reactividad"],
        ["El estado cambia antes de la animación", "Submit_AppliesLogicBefore…", "Regla acoplada al tiempo del arte"],
        ["Reemplazar un módulo no duplica stats", "TryEquip_ReplacingModule_…", "Sumar el módulo viejo y el nuevo"],
    ], [3.1, 2.8, 3.0], row_h=0.86, font=12, accent=BORDER)
    write(s, MARGIN, 5.35, CONTENT_W, 0.9,
          "80 tests en Edit Mode (P1 5 · P2 7 · P3 9 · bucle 59)  ·  1 en Play Mode  ·  el único Play Mode es P2, porque es el único donde importa que Unity invoque al adaptador.",
          13, BODY, spacing=1.15)
    takeaway(s, "No hay un test por método: hay un test por regla, y cada uno responde qué error detectaría.", TEXT)
    notes(s, """
50 s. 80 en Edit Mode y 1 en Play Mode: 81, corridos y verdes dos veces (antes y después
 del rediseño de UI y de los assets). Reparto: P1 5, P2 7, P3 9 y 59 del bucle jugable.

El único Play Mode es P2 porque es el único donde importa que Unity invoque al adaptador.

Si preguntan qué detectaría cada uno, la matriz de docs/04 tiene la columna completa.
""")

    # ---------------------------------------------------------- 16 exclusiones
    s = blank(deck, 16, total)
    head(s, "Alcance", "Esto es lo que dejé afuera, y por qué", RED)
    grid(s, MARGIN, 1.95, CONTENT_W, [
        ["Excluido", "Justificación", "Ruta de extensión"],
        ["Undo / Redo", "Capacidad habilitada, no requisito", "Historial de estados en el pipeline"],
        ["IA enemiga", "El desafío es el flujo, no la búsqueda", "El pipeline acepta cualquier ICommand"],
        ["Animación con clips", "Los modelos se mueven por transform, sin Animator", "Animator sobre el modelo actual"],
        ["Modificadores multiplicativos", "Ningún módulo del alcance los usa", "StatModifier con operación"],
        ["Mapas 3D / niebla de guerra", "Otro sistema, no exposición de estado", "Capa sobre IGridQueryService"],
    ], [2.4, 3.2, 3.3], row_h=0.68, font=11.5, accent=RED)
    takeaway(s, "No son las cosas que no llegué a hacer: son las que decidí no hacer.", RED)
    notes(s, """
45 s. La consigna dice que excluir un caso necesita justificación (§4.1).
Cada fila tiene por dónde crecer.

Ojo: sí hay arte 3D real (modelos del Kenney Space Kit). Lo excluido es la animación
con clips; el movimiento es por transform.
""")

    # ---------------------------------------------------------- 17 cambios
    s = blank(deck, 17, total)
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

    takeaway(s, "Los cuatro cambios del TP1 están en docs/01 §5; estos cuatro hallazgos, en el código.", P2)
    notes(s, """
45 s. Esto responde "qué descubriste al implementar", que la consigna pide explícitamente (§4.1).

El más útil para contar: el del pivote. Perdí tiempo buscando en la arquitectura algo que
era un problema de importación de assets. Enseña a agotar la hipótesis barata antes.

El segundo es el más arquitectónico: una vista que busca referencias por nombre de ruta
no es una vista, es una dependencia oculta. Ahora la fábrica de UI devuelve tipos.

Un quinto caso, si preguntan por los tests: una aserción mía estaba mal escrita
(la ruta costaba 3, no 4). Lo detecté corriendo la lógica fuera del editor.
Los tests también pueden estar equivocados; por eso el auditor existe.
""")

    # ---------------------------------------------------------- 18 cierre
    s = blank(deck, 18, total)
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

    write(s, MARGIN, 6.25, CONTENT_W, 0.4, "81 tests verdes · tag tp2", 13, MUTED)
    notes(s, """
20 s. Con más tiempo, el siguiente paso sería Undo/Redo, porque el pipeline ya lo habilita.
Pero eso es otro prototipo, no una deuda escondida.
""")

    # ---------------------------------------------------------- 19 backup mutation
    s = blank(deck, 0, 0)
    head(s, "Respaldo · Si preguntan", "Mutation demo: rompo una línea y el test se pone rojo", RED)
    grid(s, MARGIN, 1.95, CONTENT_W, [
        ["Prototipo", "Línea que rompo", "Test que falla"],
        ["P1", "Emitir el evento aunque el valor no cambie", "SetCell_WithSameValue_DoesNotRaiseChange"],
        ["P2", "Esperar la animación antes de mutar el estado", "Submit_AppliesLogicBeforeTheAnimation…"],
        ["P3", "Sumar el módulo anterior y el nuevo", "TryEquip_ReplacingModule_DoesNotDoubleCount"],
    ], [1.2, 3.3, 3.6], row_h=0.9, font=11.5, accent=RED)
    code(s, MARGIN, 5.35, CONTENT_W, [
        "# git stash  →  editar una línea  →  correr el test  →  mostrar rojo  →  git checkout .",
    ], accent=MUTED, size=12)
    notes(s, """
Se usa solo si preguntan "¿los tests detectan defectos reales?".
Revertir siempre con git checkout . antes de seguir.
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
    takeaway(s, "La consigna evalúa decisiones, no cantidad de patrones.", P1)
    notes(s, """
Esta slide responde el punto §4.4 de la consigna (contratos, null, mutabilidad,
transparencia referencial, excepciones).
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
    ], accent=GREEN, size=11.5, line_h=0.32)
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

    # 1 capas  ·  x 0..10  y 0..7
    fig, ax = canvas(10, 7.0)
    layers = [
        ("Presentación", "MonoBehaviour · VContainer · Assets SO", c1),
        ("Aplicación", "Pipeline de comandos · Servicio de equipamiento", c2),
        ("Dominio", "Reglas · Contratos · Cálculo puro (sin UnityEngine)", c3),
    ]
    for i, (name, detail, color) in enumerate(layers):
        y = 5.35 - i * 1.95
        box(ax, 1.5, y, 7.0, 1.4, name, detail, color, size=13, dsize=8.5)
        if i < len(layers) - 1:
            arr(ax, (5.0, y - 0.04), (5.0, y - 0.47), muted, 1.8)
    ax.text(5.0, 0.45, "using UnityEngine;  →  no compila en el dominio",
            ha="center", va="center", color=c3, fontsize=9.5, style="italic")
    fig.savefig(ASSET_DIR / "diagrama-capas.png", facecolor=bg)
    plt.close(fig)

    # 2 P1  ·  x 0..10  y 0..7.2
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

    # 3 P2  ·  x 0..11  y 0..5
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

    # 4 P3  ·  x 0..10  y 0..6.6
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

    # 5 OCP  ·  x 0..10  y 0..4.2
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
