"""
Verifica que cada .asmdef declare las referencias que sus archivos .cs realmente usan.

Detecta la clase de error que ya apareció dos veces en este proyecto:
  - CS0234: 'VoidHex.P1.Domain' no existe  -> using sin referencia declarada.
  - CS0012: falta la referencia a un assembly cuya clase base se usa (p. ej. VContainer).

No reemplaza al compilador: es un chequeo estático rápido que corre sin abrir Unity.

Uso:  python tools/verify/check_asmdef_refs.py
"""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "Assets"

# Namespaces que aporta el propio motor o los paquetes de test: no se declaran en "references".
IMPLICIT = (
    "UnityEngine", "UnityEditor", "NUnit", "System", "Microsoft",
    "TMPro", "Unity", "VContainer.Unity",
)

USING = re.compile(r"^\s*using\s+(?:static\s+)?([A-Za-z_][\w.]*)\s*;", re.MULTILINE)


def is_implicit(namespace: str) -> bool:
    return any(namespace == prefix or namespace.startswith(prefix + ".")
               for prefix in IMPLICIT)


def load_asmdefs() -> list[dict]:
    result = []
    for path in ASSETS.rglob("*.asmdef"):
        data = json.loads(path.read_text(encoding="utf-8"))
        result.append({"path": path, "name": data.get("name", ""),
                       "refs": data.get("references", []),
                       "no_engine": data.get("noEngineReferences", False)})
    return result


def sources_for(asmdef_path: Path, all_asmdef_paths: list[Path]) -> list[Path]:
    """Archivos .cs bajo la carpeta del asmdef, sin entrar en subcarpetas con su propio asmdef."""
    folder = asmdef_path.parent
    sources = []
    for candidate in folder.rglob("*.cs"):
        nested = [p for p in all_asmdef_paths
                  if p != asmdef_path and folder in p.parents and p.parent in candidate.parents]
        if nested:
            # pertenece a un asmdef más interno
            deepest = max(nested, key=lambda p: len(p.parents))
            if deepest.parent != folder:
                continue
        sources.append(candidate)
    return sources


def main() -> int:
    asmdefs = load_asmdefs()
    paths = [a["path"] for a in asmdefs]
    known = {a["name"] for a in asmdefs}
    problems = 0

    print(f"asmdefs encontrados: {len(asmdefs)}\n")

    for asmdef in asmdefs:
        refs = set(asmdef["refs"])
        missing: dict[str, list[str]] = {}

        for source in sources_for(asmdef["path"], paths):
            text = source.read_text(encoding="utf-8", errors="ignore")
            for namespace in USING.findall(text):
                if is_implicit(namespace):
                    continue
                if namespace in ("VContainer",):
                    if "VContainer" not in refs:
                        missing.setdefault("VContainer", []).append(source.name)
                    continue

                # VoidHex.P1.Domain(.X) -> assembly VoidHex.P1.Domain
                candidate = None
                for name in known:
                    if namespace == name or namespace.startswith(name + "."):
                        if candidate is None or len(name) > len(candidate):
                            candidate = name
                if candidate is None:
                    if namespace.startswith("VoidHex"):
                        missing.setdefault(namespace + " (asmdef desconocido)", []).append(source.name)
                    continue
                if candidate not in refs:
                    missing.setdefault(candidate, []).append(source.name)

        status = "OK  " if not missing else "FAIL"
        if missing:
            problems += len(missing)
        print(f"  [{status}] {asmdef['name']}")
        for target, files in missing.items():
            unique = sorted(set(files))
            print(f"        · falta referencia a '{target}' (usado en: {', '.join(unique)})")

        # El dominio no puede referenciar el motor.
        if asmdef["no_engine"]:
            for source in sources_for(asmdef["path"], paths):
                text = source.read_text(encoding="utf-8", errors="ignore")
                if re.search(r"^\s*using\s+UnityEngine", text, re.MULTILINE):
                    problems += 1
                    print(f"  [FAIL] {asmdef['name']}: {source.name} usa UnityEngine "
                          f"pero el dominio declara noEngineReferences: true")

    print()
    print("RESULTADO: todas las referencias declaradas cubren los usings."
          if problems == 0 else f"RESULTADO: {problems} referencia(s) faltante(s).")
    return 0 if problems == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
