"""
Verifica las condiciones de entrega del TP2 (§2 de la consigna).

La consigna pide un repositorio de GitHub que incluya:
  1. La entrega del TP1, como referencia para comparar diseño con implementación.
  2. El código de los prototipos y sus tests automatizados.
  3. Instrucciones para abrir, ejecutar los prototipos y correr los tests, con la versión de Unity.
  4. Documentación breve del alcance, exclusiones y decisiones de cada prototipo.

Y que se entregue por tag. Este script comprueba lo comprobable desde el repositorio local.
Lo que NO puede comprobar (y por eso lo avisa): si el repositorio es público y si el mail se envió.

Uso:  python tools/verify/check_delivery.py
"""

from __future__ import annotations

import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

REQUIRED_ROOT = ["GDD.md", "DIAGRAMS.md", "README.md"]
FORBIDDEN_DIRS = ["Library/", "Temp/", "Logs/", "UserSettings/", "obj/", "bin/"]
PROTOTYPES = ["P1_GridState", "P2_CommandPipeline", "P3_Loadout"]
SCENES = [
    "Assets/Prototypes/P1_GridState/Scenes/P1_GridState.unity",
    "Assets/Prototypes/P2_CommandPipeline/Scenes/P2_CommandPipeline.unity",
    "Assets/Prototypes/P3_Loadout/Scenes/P3_Loadout.unity",
    "Assets/GameLoop/Scenes/GameLoop.unity",
]


def git(*args: str) -> str:
    result = subprocess.run(["git", "-C", str(ROOT), *args],
                            capture_output=True, text=True, encoding="utf-8", errors="ignore")
    return result.stdout.strip()


def tracked_files() -> list[str]:
    return [line for line in git("ls-files").splitlines() if line]


def check(name: str, ok: bool, detail: str = "") -> tuple[str, bool, str]:
    return (name, ok, detail)


def main() -> int:
    files = tracked_files()
    checks = []

    # 1. TP1 como referencia.
    missing_tp1 = [f for f in REQUIRED_ROOT if f not in files]
    checks.append(check("TP1 + README versionados", not missing_tp1,
                        "faltan: " + ", ".join(missing_tp1) if missing_tp1 else "GDD.md, DIAGRAMS.md, README.md"))

    # 2. Repositorio clonable: nada de carpetas generadas.
    dirty = []
    for prefix in FORBIDDEN_DIRS:
        count = sum(1 for f in files if f.startswith(prefix))
        if count:
            dirty.append(f"{prefix} ({count})")
    checks.append(check("sin carpetas generadas", not dirty,
                        "versionadas: " + ", ".join(dirty) if dirty else "0 archivos"))

    # 3. Código de los prototipos, con dominio y tests.
    problems = []
    for proto in PROTOTYPES:
        base = f"Assets/Prototypes/{proto}"
        if not any(f.startswith(base + "/Runtime/Domain/") for f in files):
            problems.append(f"{proto} sin Runtime/Domain")
        if not any("/Tests/" in f and f.endswith(".cs") for f in files if f.startswith(base)):
            problems.append(f"{proto} sin tests")
    checks.append(check("3 prototipos con dominio y tests", not problems,
                        "; ".join(problems) if problems else ", ".join(PROTOTYPES)))

    # 4. Una escena por prototipo, generada y versionada.
    missing_scenes = [s for s in SCENES if not (ROOT / s).exists()]
    checks.append(check("escenas presentes", not missing_scenes,
                        "faltan: " + ", ".join(missing_scenes) if missing_scenes else f"{len(SCENES)} escenas"))

    # 5. Instrucciones: versión de Unity y cómo correr los tests.
    readme = (ROOT / "README.md").read_text(encoding="utf-8", errors="ignore")
    has_version = re.search(r"6000\.5\.9f1", readme) is not None
    has_tests = "Cómo correr los tests" in readme
    has_run = "Cómo ejecutar cada prototipo" in readme
    checks.append(check("README: versión de Unity", has_version, "6000.5.9f1" if has_version else "no aparece"))
    checks.append(check("README: ejecutar y testear", has_run and has_tests,
                        "secciones presentes" if (has_run and has_tests) else "falta una sección"))

    # 6. Documentación de alcance, exclusiones y decisiones.
    doc = ROOT / "docs" / "03-prototipos.md"
    text = doc.read_text(encoding="utf-8", errors="ignore") if doc.exists() else ""
    per_prototype = sum(1 for p in PROTOTYPES if p.split("_")[0] in text)
    has_scope = "Alcance comprometido" in text
    has_exclusions = "Exclusiones" in text
    checks.append(check("docs/03: alcance y exclusiones", has_scope and has_exclusions,
                        f"{per_prototype} prototipos documentados"))
    checks.append(check("docs/03: GameLoop documentado", "capa de composición" in text,
                        "sí" if "capa de composición" in text else "no"))

    # 7. Tag de entrega.
    tags = [t for t in git("tag", "-l").splitlines() if t]
    checks.append(check("tag de entrega", bool(tags), ", ".join(tags) if tags else "no hay tags locale"))

    # 8. Sincronía con el remoto.
    branch_sync = git("log", "--oneline", "origin/main..HEAD")
    checks.append(check("todo pusheado", not branch_sync,
                        "hay commits sin push" if branch_sync else "main == origin/main"))

    print("Condiciones de entrega (consigna TP2 §2)\n")
    failed = 0
    for name, ok, detail in checks:
        if not ok:
            failed += 1
        print(f"  [{'OK  ' if ok else 'FAIL'}] {name:<34} {detail}")

    print("\nRequiere verificación manual (no es observable desde el repo):")
    print("  · El repositorio debe ser PÚBLICO: GitHub devuelve 404 si es privado.")
    print("  · El enlace enviado debe ser el del TAG, no el de la rama.")
    print("  · El mail a diego.freijo@udelaciudad.edu.ar antes del límite.")

    print()
    if failed:
        print(f"RESULTADO: {failed} condición(es) sin cumplir.")
        return 1

    print("RESULTADO: condiciones locales cumplidas.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
