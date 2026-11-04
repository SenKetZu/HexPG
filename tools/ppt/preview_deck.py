"""Render de control del deck: PPTX -> PDF -> PNG (solo para revisar a ojo)."""

import os
import subprocess
import sys
import tempfile

sys.path.insert(0, os.path.join(os.environ["TEMP"], "pymupdf-lib"))

import pymupdf

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
PPTX = os.path.join(ROOT, "docs", "presentacion", "TP2-VoidHex-Tactics.pptx")
OUT = os.path.join(tempfile.gettempdir(), "deck-preview")

os.makedirs(OUT, exist_ok=True)
subprocess.run(
    [r"C:\Program Files\LibreOffice\program\soffice.exe", "--headless",
     "--convert-to", "pdf", "--outdir", OUT, PPTX],
    check=True, capture_output=True,
)

pdf = os.path.join(OUT, "TP2-VoidHex-Tactics.pdf")
doc = pymupdf.open(pdf)
png_dir = os.path.join(OUT, "png")
os.makedirs(png_dir, exist_ok=True)
for i, page in enumerate(doc, start=1):
    page.get_pixmap(dpi=110).save(os.path.join(png_dir, f"slide-{i:02d}.png"))

print(f"{len(doc)} slides -> {png_dir}")
