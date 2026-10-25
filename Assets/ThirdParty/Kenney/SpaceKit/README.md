# Kenney Space Kit (CC0)

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
