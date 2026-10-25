"""
Instala los modelos 3D de Kenney Space Kit en el proyecto.

Qué hace
--------
1. Verifica que el kit esté descargado y descomprimido en `_downloads/space-kit`.
2. Copia SOLO los modelos usados por los prototipos (no los 153) a
   `Assets/ThirdParty/Kenney/SpaceKit/`, junto con la licencia CC0.
3. Escribe el `README.md` de atribución.

Por qué solo un subconjunto: el repo debe quedar liviano y legible. Si un prototipo
necesita otro modelo, se agrega a WANTED y se vuelve a correr.

Uso:  python tools/assets/install_kenney_spacekit.py
"""

from __future__ import annotations

import shutil
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
KIT = ROOT / "_downloads" / "space-kit"
DEST = ROOT / "Assets" / "ThirdParty" / "Kenney" / "SpaceKit"

# Los modelos viven en un "Resources" para poder cargarse por nombre en runtime:
# Resources.Load("Models/Ship_SpeederA"). Una sola copia, sin rutas duplicadas.
MODELS_DIR = DEST / "Resources" / "Models"

# Modelos usados, con el nombre que tendrán en Unity.
WANTED = {
    # Naves del jugador (P2, P3)
    "craft_speederA": "Ship_SpeederA",
    "craft_speederB": "Ship_SpeederB",
    "craft_racer": "Ship_Racer",
    # Enemigo (P2): cañón orbital, se lee distinto a una nave
    "turret_single": "Enemy_Turret",
    # Rocas (P1): asteroides y campo de escombros
    "meteor": "Rock_Meteor",
    "meteor_detailed": "Rock_MeteorDetailed",
    "meteor_half": "Rock_MeteorHalf",
    "rock_largeA": "Rock_LargeA",
    "rock_crystals": "Rock_Crystals",
    "rocks_smallA": "Rock_Debris",
    # Props (P3): plataforma de hangar
    "platform_small": "Platform_Small",
    "platform_center": "Platform_Center",
    "barrel": "Prop_Barrel",
}

ATTRIBUTION = """# Kenney Space Kit (CC0)

Modelos 3D usados por los prototipos del TP2.

- **Autor:** Kenney (https://kenney.nl)
- **Kit:** Space Kit — https://kenney.nl/assets/space-kit
- **Licencia:** Creative Commons CC0 1.0 (dominio público)
  https://creativecommons.org/publicdomain/zero/1.0/
- **Formato:** FBX (importado nativamente por Unity)

Se incluye un subconjunto de modelos, no el kit completo. Los nombres fueron
normalizados (`craft_speederA` → `Ship_SpeederA`) para que el código de presentación
no dependa del nombre original del asset.

Los materiales NO vienen del kit: se generan con el shader URP del proyecto y la paleta
de `VoidHex.UiKit`, para que la escena y la UI compartan el mismo lenguaje visual.

Los modelos se cargan por nombre en runtime (`Resources/Models/<Nombre>.fbx`), por eso
están dentro de una carpeta `Resources`: es el único lugar duplicado del proyecto y
evita referencias serializadas que se rompen al regenerar las escenas.
"""


def main() -> int:
    fbx_dir = KIT / "Models" / "FBX format"

    if not fbx_dir.exists():
        print(f"[ERROR] No se encontró el kit descomprimido en: {KIT}")
        print("        Esperado: _downloads/space-kit/Models/FBX format/*.fbx")
        return 1

    DEST.mkdir(parents=True, exist_ok=True)
    MODELS_DIR.mkdir(parents=True, exist_ok=True)

    copied, missing = 0, []

    for source_name, target_name in WANTED.items():
        source = fbx_dir / f"{source_name}.fbx"
        if not source.exists():
            missing.append(source_name)
            continue

        shutil.copy2(source, MODELS_DIR / f"{target_name}.fbx")
        copied += 1

    license_file = KIT / "License.txt"
    if license_file.exists():
        shutil.copy2(license_file, DEST / "Kenney_SpaceKit_License.txt")

    (DEST / "README.md").write_text(ATTRIBUTION, encoding="utf-8")

    print(f"Modelos copiados: {copied}/{len(WANTED)}")
    if missing:
        print("No encontrados en el kit: " + ", ".join(missing))

    print(f"Destino: {MODELS_DIR.relative_to(ROOT)}")
    return 0 if not missing else 2


if __name__ == "__main__":
    sys.exit(main())
