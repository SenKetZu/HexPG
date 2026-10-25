"""
Verifica que las escenas de los prototipos estén bien formadas y completas.

Comprueba, sin abrir Unity:
  - que cada escena exista;
  - que todos los componentes esperados estén realmente adjuntos (por GUID del script);
  - que la cámara y la luz estén presentes;
  - que los assets de P3 existan y estén referenciados por la escena.

Uso:  python tools/verify/check_scenes.py
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "Assets" / "Prototypes"

EXPECTED = {
    "P1_GridState/Scenes/P1_GridState.unity": [
        "P1_GridState/Runtime/Unity/GridBootstrap.cs",
        "P1_GridState/Runtime/Unity/GridCellRenderer.cs",
        "P1_GridState/Runtime/Unity/GridPresenter.cs",
        "P1_GridState/Runtime/Unity/GridScreen.cs",
    ],
    "P2_CommandPipeline/Scenes/P2_CommandPipeline.unity": [
        "P2_CommandPipeline/Runtime/Unity/ShipView.cs",
        "P2_CommandPipeline/Runtime/Unity/TurnDemoDriver.cs",
        "P2_CommandPipeline/Runtime/Unity/TurnScreen.cs",
    ],
    "P3_Loadout/Scenes/P3_Loadout.unity": [
        "P3_Loadout/Runtime/Unity/LoadoutDemo.cs",
        "P3_Loadout/Runtime/Unity/LoadoutScreen.cs",
        "P3_Loadout/Runtime/Unity/EquipmentLifetimeScope.cs",
        "P3_Loadout/Runtime/Unity/ShipPreview.cs",
    ],
}

P3_ASSETS = [
    "P3_Loadout/Assets/ShipConfig.asset",
    "P3_Loadout/Assets/ModuleCatalog.asset",
    "P3_Loadout/Assets/Modules/laser-mk1.asset",
    "P3_Loadout/Assets/Modules/laser-mk2.asset",
    "P3_Loadout/Assets/Modules/laser-mk3.asset",
    "P3_Loadout/Assets/Modules/thruster-mk1.asset",
    "P3_Loadout/Assets/Modules/shield-mk1.asset",
]

GUID = re.compile(r"guid:\s*([0-9a-f]{32})")


def guid_of(meta_path: Path) -> str | None:
    if not meta_path.exists():
        return None
    match = GUID.search(meta_path.read_text(encoding="utf-8", errors="ignore"))
    return match.group(1) if match else None


def main() -> int:
    problems = 0

    for scene_rel, scripts in EXPECTED.items():
        scene_path = ASSETS / scene_rel
        print(f"=== {Path(scene_rel).name} ===")

        if not scene_path.exists():
            print(f"  [FALTA] la escena no existe: {scene_rel}")
            problems += 1
            continue

        text = scene_path.read_text(encoding="utf-8", errors="ignore")
        print(f"  [OK  ] escena presente ({len(text) // 1024} KB)")

        for script_rel in scripts:
            script_path = ASSETS / script_rel
            guid = guid_of(script_path.with_suffix(script_path.suffix + ".meta"))
            name = Path(script_rel).stem

            if guid is None:
                print(f"  [FALTA] {name}: sin .meta (Unity no lo importó)")
                problems += 1
            elif guid not in text:
                print(f"  [FALTA] {name}: el componente no está adjunto en la escena")
                problems += 1
            else:
                print(f"  [OK  ] {name} adjunto")

        for required in ("Main Camera", "Directional Light"):
            if required in text:
                print(f"  [OK  ] {required}")
            else:
                print(f"  [AVISO] no se encontró {required}")

    print()
    print("=== assets de P3 ===")
    for asset_rel in P3_ASSETS:
        asset_path = ASSETS / asset_rel
        meta = asset_path.with_suffix(asset_path.suffix + ".meta")
        if asset_path.exists() and meta.exists():
            print(f"  [OK  ] {asset_path.name}")
        else:
            print(f"  [FALTA] {asset_rel}")
            problems += 1

    scene_p3 = ASSETS / "P3_Loadout/Scenes/P3_Loadout.unity"
    if scene_p3.exists():
        text = scene_p3.read_text(encoding="utf-8", errors="ignore")
        for ref in ("ShipConfig.asset", "ModuleCatalog.asset"):
            guid = guid_of((ASSETS / "P3_Loadout/Assets" / ref).with_suffix(".asset.meta"))
            if guid and guid in text:
                print(f"  [OK  ] {ref} referenciado por la escena")
            else:
                print(f"  [FALTA] {ref} no está asignado en el LifetimeScope")
                problems += 1

    print()
    print("RESULTADO: escenas completas." if problems == 0
          else f"RESULTADO: {problems} problema(s).")
    return 0 if problems == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
