"""Muestra el detalle completo de los tests fallidos del JSON de run_tests."""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

PAYLOAD = re.compile(r'\{"Summary":.*', re.DOTALL)


def main(path: str) -> int:
    raw = Path(path).read_bytes()

    text = ""
    for encoding in ("utf-8-sig", "utf-16", "utf-16-le", "latin-1"):
        try:
            candidate = raw.decode(encoding)
        except UnicodeDecodeError:
            continue
        if '{"Summary"' in candidate:
            text = candidate
            break

    match = PAYLOAD.search(text)
    if not match:
        print("sin bloque JSON")
        return 1

    data, _ = json.JSONDecoder().raw_decode(match.group(0))

    for result in data["Results"]:
        if result["Status"] == "Passed":
            continue

        print(f"=== {result['FullName']}")
        print(result.get("Message") or "(sin mensaje)")
        trace = result.get("StackTrace")
        if trace:
            print(trace)
        print()

    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1] if len(sys.argv) > 1 else "Logs/tests-editmode.json"))
