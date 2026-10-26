# Assessment: por qué los materiales del kit no se ven bien

Diagnóstico medido (no inferido) sobre la torreta, la nave y los obstáculos.
Todo lo que sigue sale de leer los materiales en el editor en ejecución y de renderizar
los modelos crudos en una vista 3/4 canónica para comparar.

## Resumen

El problema **no es que los materiales no se carguen**: se cargan bien. Lo que falla es que
nuestro código y nuestra presentación esperan un modelo con textura PBR, y el Kenney Space Kit
usa otra técnica: **materiales planos con color sólido**. Nuestro pipeline de color asume
lo contrario y por eso el resultado se ve mal.

## Evidencia 1: los materiales del kit son planos, sin textura

Leído del FBX `Enemy_Turret.fbx` (importado con `materialImportMode: 2`):

| Material | Color base | Textura |
| --- | --- | --- |
| `dark` | `0.56, 0.58, 0.62` | ninguna |
| `metal` | `0.93, 0.94, 0.96` | ninguna |
| `metalDark` | `0.84, 0.86, 0.89` | ninguna |
| `metalRed` | `1.00, 0.81, 0.49` | ninguna |
| `rockTrack` / `rock` | `0.99, 0.79, 0.70` | ninguna |
| `crystal` | `0.47, 0.94, 0.79` | ninguna |

Los cuatro materiales de la torreta tienen `_BaseMap = no`, `_Metallic = 0`, `_Smoothness = 0`.
**No hay texturas en ningún lado del kit.** El color vive en `_BaseColor` y punto. Es un kit
*low-poly palette-based*: cada pieza del modelo es un color plano y el volumen lo da
exclusivamente el sombreado de la luz.

## Evidencia 2: nuestro material generado es casi idéntico

`ModelLibrary.CreateLitMaterial` produce un `Lit_B22929` con `_Metallic = 0`,
`_BaseMap = no`, `_Smoothness = 0.50` (el único valor distinto: el default del shader URP).

Es decir: **no estamos rompiendo el material por propiedades**. Lo que rompe el resultado es
*qué piezas repintamos*, no *cómo*.

## Causa raíz 1: repintar el submesh equivocado destruye la lectura del modelo

Cada renderer del FBX tiene varios submeshes, y el material de acento **no está donde parece**:

```
Enemy_Turret(Clone)  [0] dark      [1] metal     [2] metalDark
turret               [0] metalRed  [1] dark      [2] metal      ← el acento es la CÚPULA
Group                [0] metal     [1] dark
Group 1              [0] metal     [1] dark
```

En la torreta, `metalRed` es **la cúpula**: la pieza más grande y más visible desde arriba.
Repintarla de rojo saturado no "marca al enemigo", borra el modelo: queda un óvalo rojo.
Lo mismo en la nave, donde `metalRed` es el trim de las alas: pintarlo de teal deja una
mancha de color sobre un casco blanco.

Conclusión: **el acento no sirve como canal de color de equipo** en este kit. Solo hay tres
materiales neutros (`metal`, `metalDark`, `dark`) y un acento que ya tiene un rol visual.

Decisión aplicada: **no se repinta ningún submesh del kit**. El color de equipo vive en el
aro del piso (P2 y bucle) y en el color de la losa ocupada (P1). El modelo se lee completo y
el color sigue siendo inequívoco.

## Causa raíz 2: las rocas usan el color del suelo de un planeta

`rockTrack` y `rock` son `0.99, 0.79, 0.70`: un **salmón muy claro**. En el kit ese material
está pensado para pistas y superficies de planeta, no para asteroides en el vacío. Sobre un
tablero espacial oscuro (`#0F1A2B`) esas rocas se leen como manchas pastel, no como obstáculos.

Mitigación aplicada: `ModelLibrary.RestyleRocks` reemplaza solo los materiales `rock*`
por un gris pizarra (`ModelLibrary.AsteroidColor`), y deja `crystal` en su teal original (que
sí funciona como acento). Se aplica en las **dos** vistas con props de terreno: P1
(`GridCellRenderer.SpawnProp`) y el bucle (`LoopGridView`). Sigue abierta, como mejora
opcional, la idea de elegir otro modelo del kit para asteroides.

## Causa raíz 3 (la más importante): la cámara cenital aplana el kit

Los modelos están modelados para mirarse en **3/4**. La escena del bucle usaba
`FrameInFreeArea` con cámara ortográfica a **90° (cenital pura)**. A 90°:

- la nave es una mancha blanca plana (no se ve la cabina ni el motor),
- la torreta es un rectángulo (no se ve el cañón ni las patas),
- el asteroide es un hexágono beige.

Comparación medida, misma escena y misma luz, cambiando solo el ángulo:

| Vista | Nave | Torreta | Roca |
| --- | --- | --- | --- |
| 90° cenital (antes) | mancha blanca | rectángulo rojo | hexágono beige |
| 54–58° inclinada | cabina, alas, motor | cañón y patas | facetas con volumen |

Aplicado: `FrameInFreeArea(..., pitchDeg: 54f)` en el bucle **y en P1**, que era la última
escena que seguía con `FrameOrthoTopDown` a 90°. P2 y P3 ya usaban cámara perspectiva inclinada
(`44°` y `32°`).

## Causa raíz 4: el ambiente tapaba el sombreado

`Ambient` estaba en `0.34, 0.38, 0.46`. Con un kit que no tiene textura, **el volumen sale
solo del contraste entre caras iluminadas y no iluminadas**. Un ambiente tan alto le da a
todas las caras casi el mismo relleno y el modelo se aplana.

Aplicado: ambiente `0.16, 0.19, 0.26`, luz direccional `1.45` a `38°` de elevación.

## Lo que NO es el problema

- No es un fallo de carga: los 13 FBX se instancian y tienen sus 4 materiales correctos.
- No es un shader faltante: todo renderiza con `Universal Render Pipeline/Lit`.
- No es `sharedMaterials` vs `materials`: la asignación por índice funciona (verificado).
- No es escala: las piezas están al tamaño correcto.

## Bugs de código encontrados en el camino (ya corregidos)

1. **`Spawn` seguía llamando a `TintRenderers`** en lugar de `TintAccents`. La edición no se
   había aplicado y el síntoma era exactamente el medido: todo el modelo de un color. El
   problema de fondo era otro: incluso repintando el submesh "correcto", ese submesh es una
   pieza estructural. Se eliminó el tintado de malla (`TintAccents` ya no existe).
2. **`renderer.sharedMaterial = m` solo escribe el slot 0** en Unity 6. Los demás submeshes
   conservaban el material del kit, así que los modelos quedaban a medio pintar.
3. **`Object.Destroy` en edit mode** no destruye y tira un error: los props se duplicaban al
   regenerar la escena. Corregido con `SafeObjects.Destroy`.
4. **Materiales sin inicializar**: los aros de peligro se creaban antes que su material y
   salían con el magenta de shader faltante.
5. **Rango de ataque duplicado como literal `3`** en la UI, en vez de leerse del dominio.
6. **Tablero de P2 sin malla**: `HexMesh.Get` genera con `HideAndDontSave`, así que el tablero
   armado en el editor guardaba `m_Mesh: {fileID: 0}` y desaparecía al recargar la escena (las
   naves flotaban sobre el fondo). Se serializa una copia como `P2_TileMesh.asset`.

## Decisiones tomadas (antes "qué queda abierto")

1. **Color de equipo: canal externo, no materiales.** Se eliminó `TintAccents`; `Spawn` ya no
   tiñe ningún submesh. Jugador y torreta se distinguen por el **aro del piso** (cian/rojo) en
   P2 y el bucle, y por el color de la losa ocupada en P1. Solo la primitiva de respaldo (si
   falta un FBX) se tiñe entera, porque no tiene paleta que preservar.
2. **Rocas: gris pizarra compartido.** `ModelLibrary.AsteroidColor` es la única fuente del
   color; P1 y el bucle llaman a `RestyleRocks`. `crystal` conserva su teal.
3. **P1 y P3 verificados con la misma lupa.** P1 pasó a 54° y reestila sus rocas; en P3 el
   `Pad` se repinta entero a propósito (es decorado, no un modelo de nave) y la nave conserva
   su paleta. La emisión de PA en `ShipPreview` es feedback de energía, no color de equipo.
4. **Iluminación unificada.** Las cuatro escenas usan `SetupEnvironment`: ambiente
   `0.16, 0.19, 0.26` y direccional `1.45` a `38°`, sin luces propias por escena.

Queda como mejora opcional, no como defecto: elegir modelos de asteroide que no usen
`rockTrack` en lugar de reestilarlos.

## Herramientas usadas para medir

Estas sondas se ejecutan con `unity command ... eval_file --file <ruta>.cs` en play mode
y devuelven un string con el resultado:

- inventario de materiales por renderer y por slot (`sharedMaterials[i].name`, `_BaseColor`);
- comparación de propiedades entre material del kit y material generado;
- render de los modelos crudos en fila con cámara 3/4 canónica para tener la referencia visual.

**Ojo**: `eval_file` compila y recarga el dominio, lo que **saca del play mode**. Para medir
en vivo hay que volver a entrar con `editor_play` después de cada `eval_file`.
