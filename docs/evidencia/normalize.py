"""Normaliza la salida del CLI de Unity a JSON legible para docs/evidencia."""

import json
import pathlib

for name in ["tests-editmode.json", "tests-playmode.json"]:
    path = pathlib.Path("docs/evidencia") / name
    raw = path.read_bytes()
    for encoding in ("utf-8-sig", "utf-16", "utf-8"):
        try:
            text = raw.decode(encoding)
            break
        except UnicodeDecodeError:
            continue

    start = text.find("{")
    payload, _ = json.JSONDecoder().raw_decode(text[start:])
    summary = payload.get("summary") or payload.get("Summary")
    results = payload.get("results") or payload.get("Results") or []
    clean = {
        "source": "unity command run_tests",
        "summary": summary,
        "results": [
            {
                "test": item.get("FullName"),
                "status": item.get("Status"),
                "duration": round(item.get("Duration") or 0, 4),
            }
            for item in results
        ],
    }
    path.write_text(json.dumps(clean, indent=2, ensure_ascii=False), encoding="utf-8")
    print(name, summary)
