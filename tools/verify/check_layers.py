"""
Verifica las separaciones de capas que el proyecto declara, sin abrir Unity.

Complementa a check_asmdef_refs.py: aquel mira las referencias entre assemblies, este mira
la forma del código.

Reglas comprobadas
------------------
R1  MonoBehaviours solo en la capa de presentación (carpetas Unity/, Editor/) o en tests.
    El dominio y la aplicación son C# puro: un MonoBehaviour ahí ata la regla al motor y
    rompe la posibilidad de testearla sinPlay Mode.

R2  Los assemblies con noEngineReferences: true no mencionan tipos del motor.

R3  La destrucción de objetos pasa por SafeObjects.Destroy.
    Object.Destroy lanza "Destroy may not be called from edit mode" cuando el mismo camino
    corre desde el editor, y el objeto NO se destruye: el bug se ve como objetos duplicados.
    Se exceptúan el propio helper, el editor y los tests, que sí pueden elegir a mano.

Uso:  python tools/verify/check_layers.py
"""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "Assets"

MONOBEHAVIOUR = re.compile(r"class\s+\w+\s*:\s*[^{]*\bMonoBehaviour\b")
ENGINE_TYPES = re.compile(
    r"\b(UnityEngine|MonoBehaviour|GameObject|Transform|Vector[23]|Quaternion|"
    r"ScriptableObject|Coroutine|Time\.|Debug\.Log)\b")
DIRECT_DESTROY = re.compile(r"(?<!SafeObjects\.)(?<![\w.])\bObject\.Destroy\s*\(|(?<![\w.])\bDestroy\s*\(")

# Carpetas donde un MonoBehaviour es legítimo: es la capa que dibuja.
PRESENTATION_PARTS = {"Unity", "Editor"}
TEST_PARTS = {"Tests"}

# Archivos que pueden tocar Destroy a mano, con motivo declarado.
DESTROY_EXEMPT = {"SafeObjects.cs"}


def load_asmdefs() -> list[dict]:
    result = []
    for path in ASSETS.rglob("*.asmdef"):
        data = json.loads(path.read_text(encoding="utf-8"))
        result.append({
            "path": path,
            "name": data.get("name", ""),
            "no_engine": data.get("noEngineReferences", False),
        })
    return result


def sources_under(folder: Path, all_asmdef_paths: list[Path]) -> list[Path]:
    """Archivos .cs de la carpeta, sin los que pertenecen a un asmdef más interno."""
    sources = []
    for candidate in folder.rglob("*.cs"):
        nested = [p for p in all_asmdef_paths
                  if p != folder / (folder.name + ".asmdef")
                  and folder in p.parents and p.parent in candidate.parents]
        if nested:
            deepest = max(nested, key=lambda p: len(p.parents))
            if deepest.parent != folder:
                continue
        sources.append(candidate)
    return sources


def relative(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8", errors="ignore")


def strip_comments(text: str) -> str:
    """Quita comentarios para no reportar las menciones que aparecen en la documentación."""
    text = re.sub(r"/\*.*?\*/", "", text, flags=re.DOTALL)
    return re.sub(r"//[^\n]*", "", text)


def check_monobehaviour_placement(sources: list[Path]) -> list[str]:
    problems = []
    for source in sources:
        parts = set(source.relative_to(ASSETS).parts)
        if parts & PRESENTATION_PARTS or parts & TEST_PARTS:
            continue
        if MONOBEHAVIOUR.search(strip_comments(read(source))):
            problems.append(
                f"R1 {relative(source)}: MonoBehaviour fuera de la capa de presentación")
    return problems


def check_no_engine(asmdefs: list[dict], all_paths: list[Path]) -> list[str]:
    problems = []
    for asmdef in asmdefs:
        if not asmdef["no_engine"]:
            continue
        for source in sources_under(asmdef["path"].parent, all_paths):
            code = strip_comments(read(source))
            if re.search(r"^\s*using\s+UnityEngine", code, re.MULTILINE):
                problems.append(
                    f"R2 {relative(source)}: usa UnityEngine y {asmdef['name']} declara "
                    f"noEngineReferences: true")
                continue
            hit = ENGINE_TYPES.search(code)
            if hit:
                problems.append(
                    f"R2 {relative(source)}: menciona '{hit.group(1)}' en {asmdef['name']}")
    return problems


def check_destroy(sources: list[Path]) -> list[str]:
    problems = []
    for source in sources:
        parts = set(source.relative_to(ASSETS).parts)
        if source.name in DESTROY_EXEMPT or parts & TEST_PARTS or "Editor" in parts:
            continue
        for number, line in enumerate(read(source).splitlines(), start=1):
            code = line.split("//")[0]
            if DIRECT_DESTROY.search(code):
                problems.append(
                    f"R3 {relative(source)}:{number}: Destroy directo; usar SafeObjects.Destroy")
    return problems


def main() -> int:
    asmdefs = load_asmdefs()
    all_paths = [a["path"] for a in asmdefs]
    sources = [p for p in ASSETS.rglob("*.cs")]
    sources = [p for p in sources if "Editor" not in p.parts or True]  # todos cuentan

    problems = []
    problems += check_monobehaviour_placement(sources)
    problems += check_no_engine(asmdefs, all_paths)
    problems += check_destroy(sources)

    print(f"archivos .cs analizados: {len(sources)}\n")

    if problems:
        for problem in problems:
            print(f"  [FAIL] {problem}")
        print(f"\nRESULTADO: {len(problems)} violación(es) de capas.")
        return 1

    print("  [OK  ] R1 MonoBehaviours solo en presentación")
    print("  [OK  ] R2 dominio y aplicación sin tipos del motor")
    print("  [OK  ] R3 destrucción de objetos vía SafeObjects")
    print("\nRESULTADO: las separaciones de capas se respetan.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
