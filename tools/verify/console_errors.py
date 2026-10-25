"""Filtra errores y excepciones del JSON de `unity command console`."""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

PAYLOAD = re.compile(r'\{"entries":.*', re.DOTALL)


def main(paths: list[str]) -> int:
    for raw in paths:
        path = Path(raw)
        if not path.exists():
            print(f"[FALTA] {raw}")
            continue

        bytes_ = path.read_bytes()
        text = ""
        for encoding in ("utf-8-sig", "utf-16", "utf-16-le", "latin-1"):
            try:
                candidate = bytes_.decode(encoding)
            except UnicodeDecodeError:
                continue
            if '{"entries"' in candidate:
                text = candidate
                break

        match = PAYLOAD.search(text)
        if not match:
            print(f"[?] {path.name}: sin bloque JSON")
            continue

        data, _ = json.JSONDecoder().raw_decode(match.group(0))
        entries = data.get("entries", [])

        bad = [e for e in entries
               if e.get("level") in ("error", "exception") or "Exception" in (e.get("message") or "")]

        print(f"=== {path.name}: {len(entries)} entradas, {len(bad)} con error ===")
        for entry in bad[-8:]:
            message = (entry.get("message") or "").strip().replace("\n", " | ")
            print(f"  [{entry.get('level')}] {message[:260]}")
            trace = (entry.get("stackTrace") or "").splitlines()
            if trace:
                print(f"      @ {trace[0][:150]}")

        # Últimas líneas de log del proyecto, útiles para confirmar que la demo corrió.
        relevant = [e for e in entries if (e.get("message") or "").startswith("[")]
        if relevant:
            print("  --- logs del proyecto (últimos 6) ---")
            for entry in relevant[-6:]:
                print(f"  · {(entry.get('message') or '')[:160]}")

    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
