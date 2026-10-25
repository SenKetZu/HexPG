"""
Resume el resultado JSON de `unity command run_tests`.

Uso:
    unity command run_tests --mode EditMode > Logs/tests-editmode.json
    python tools/verify/summarize_tests.py Logs/tests-editmode.json
"""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

PAYLOAD = re.compile(r'\{"Summary":.*', re.DOTALL)


def read_any_encoding(path: Path) -> str:
    """PowerShell escribe redirecciones en UTF-16; el CLI en UTF-8. Se prueban ambas."""
    raw = path.read_bytes()
    for encoding in ("utf-8-sig", "utf-16", "utf-16-le", "latin-1"):
        try:
            text = raw.decode(encoding)
        except UnicodeDecodeError:
            continue
        if '{"Summary"' in text:
            return text
    return raw.decode("utf-8", errors="ignore")


def main(paths: list[str]) -> int:
    total_failed = 0

    for raw in paths:
        path = Path(raw)
        if not path.exists():
            print(f"[FALTA] {raw}")
            total_failed += 1
            continue

        text = read_any_encoding(path)
        match = PAYLOAD.search(text)
        if not match:
            print(f"[?] {path.name}: no se encontró el bloque JSON de resultados")
            total_failed += 1
            continue

        decoder = json.JSONDecoder()
        data, _ = decoder.raw_decode(match.group(0))

        summary = data["Summary"]
        mode = data.get("Mode", "?")
        print(f"=== {path.name} ({mode}) ===")
        print(f"  total {summary['Total']} · pasados {summary['Passed']} · "
              f"fallidos {summary['Failed']} · omitidos {summary['Skipped']}")

        for result in data["Results"]:
            if result["Status"] == "Passed":
                continue
            total_failed += 1
            message = (result.get("Message") or "").strip().replace("\n", " ")
            print(f"  [FAIL] {result['FullName']}")
            if message:
                print(f"         {message[:200]}")

        print()

    print("RESULTADO: suite verde." if total_failed == 0
          else f"RESULTADO: {total_failed} problema(s).")
    return 0 if total_failed == 0 else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:] or ["Logs/tests-editmode.json"]))
