using UnityEngine;

namespace VoidHex.UiKit
{
    /// <summary>
    /// Malla de un prisma hexagonal unitario, generada en código.
    ///
    /// Por qué generar en vez de usar un asset: es reproducible (no hay binarios ni importadores de
    /// por medio), tiene la forma exacta que necesita la proyección axial y no depende de que alguien
    /// no borre un FBX.
    ///
    /// Convención (la misma que <c>HexProjection</c>): hexágono "flat-top" de radio 1 en el plano XZ.
    ///   · ancho  = 2
    ///   · fondo  = √3
    ///   · altura = 1 (de y=0 a y=1; el llamador la escala)
    ///
    /// Con esa convención, dos casillas vecinas quedan a 1.5 en X y √3 en Z, exactamente lo que
    /// produce la proyección axial. Al escalar por la separación, los hexágonos encajan sin huecos.
    /// </summary>
    public static class HexMesh
    {
        private static Mesh _mesh;

        public static Mesh Get()
        {
            if (_mesh != null) return _mesh;

            const int sides = 6;

            // Conteo explícito: 1 centro + 6 borde por tapa (2 tapas) y 4 vértices por lado.
            // Un array corto lanzaba IndexOutOfRange dentro de la construcción y dejaba el
            // tablero sin malla: el síntoma era un mapa invisible, no un error visible.
            const int capVertices = 1 + sides;
            const int sideVertices = sides * 4;
            const int vertexCount = (capVertices * 2) + sideVertices;

            // 6 triángulos por tapa (abanico) y 2 por lado.
            const int capTriangles = sides * 3;
            const int sideTriangles = sides * 6;
            const int indexCount = (capTriangles * 2) + sideTriangles;

            var vertices = new Vector3[vertexCount];
            var triangles = new int[indexCount];

            var v = 0;
            var t = 0;

            // Rims: 0..5 = tapa superior, 6..11 = base, 12.. = lados (normales planas por cara).
            var top = new Vector3[sides];
            var bottom = new Vector3[sides];

            for (var i = 0; i < sides; i++)
            {
                // Vértices en 0°, 60°, … 300°: hexágono "flat-top" con ancho 2R en X y √3·R en Z.
                // Es la orientación que exige la proyección axial del juego (columnas a 1.5R,
                // filas a √3·R con desfase de √3/2·R entre columnas). Con otro desfase, los
                // hexágonos quedan girados y no encajan entre sí.
                var angle = Mathf.Deg2Rad * (60f * i);
                var x = Mathf.Cos(angle);
                var z = Mathf.Sin(angle);

                top[i] = new Vector3(x, 1f, z);
                bottom[i] = new Vector3(x, 0f, z);
            }

            // ---- tapa superior (normal +Y) ----
            var topCenter = v;
            vertices[v++] = new Vector3(0f, 1f, 0f);

            var topStart = v;
            for (var i = 0; i < sides; i++) vertices[v++] = top[i];

            for (var i = 0; i < sides; i++)
            {
                var next = (i + 1) % sides;
                triangles[t++] = topCenter;
                triangles[t++] = topStart + next;
                triangles[t++] = topStart + i;
            }

            // ---- base (normal -Y) ----
            var bottomCenter = v;
            vertices[v++] = new Vector3(0f, 0f, 0f);

            var bottomStart = v;
            for (var i = 0; i < sides; i++) vertices[v++] = bottom[i];

            for (var i = 0; i < sides; i++)
            {
                var next = (i + 1) % sides;
                triangles[t++] = bottomCenter;
                triangles[t++] = bottomStart + i;
                triangles[t++] = bottomStart + next;
            }

            // ---- lados: un quad por arista, con normales planas ----
            for (var i = 0; i < sides; i++)
            {
                var next = (i + 1) % sides;
                var start = v;

                vertices[v++] = bottom[i];
                vertices[v++] = top[i];
                vertices[v++] = top[next];
                vertices[v++] = bottom[next];

                triangles[t++] = start;
                triangles[t++] = start + 1;
                triangles[t++] = start + 2;

                triangles[t++] = start;
                triangles[t++] = start + 2;
                triangles[t++] = start + 3;
            }

            _mesh = new Mesh
            {
                name = "HexPrism",
                vertices = vertices,
                triangles = triangles,
                hideFlags = HideFlags.HideAndDontSave,
            };

            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();

            return _mesh;
        }

        /// <summary>
        /// Crea un GameObject con la malla hexagonal.
        ///
        /// <paramref name="radius"/> es el circunradio: para que las casillas se toquen debe ser
        /// igual a la separación entre centros (Spacing). Se usa algo menor para dejar una junta.
        /// </summary>
        public static GameObject CreateTile(string name, Transform parent, float radius, float height,
            Material material, Mesh mesh = null)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);

            // Una vista que arma el tablero en el editor necesita pasar una malla serializada como
            // asset: la de <see cref="Get"/> vive en memoria y se pierde al recargar la escena.
            go.GetComponent<MeshFilter>().sharedMesh = mesh != null ? mesh : Get();
            go.GetComponent<MeshRenderer>().sharedMaterial = material;

            // La malla es unitaria: escala X/Z por el radio y Y por la altura.
            go.transform.localScale = new Vector3(radius, height, radius);

            return go;
        }

        /// <summary>
        /// Contorno hexagonal hecho con 6 segmentos finos.
        ///
        /// Se construye con cubos en lugar de una malla extra: es más simple, no necesita materiales
        /// con máscara de recorte y se ve igual desde cualquier ángulo de la cámara cenital.
        /// El hexágono regular tiene lado = radio, y la apotema (distancia al centro de cada lado)
        /// es radio · cos(30°).
        /// </summary>
        public static Transform CreateOutline(string name, Transform parent, float radius, float thickness,
            float height, Material material)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);

            const int sides = 6;
            var apothem = radius * 0.8660254f;

            for (var i = 0; i < sides; i++)
            {
                // Centro de cada lado: a mitad de camino entre dos vértices (vértices en 60°·k).
                var angleDeg = (60f * i) + 30f;
                var angleRad = angleDeg * Mathf.Deg2Rad;

                var segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                segment.name = name + "_" + i;
                segment.transform.SetParent(root, false);

                segment.transform.localPosition = new Vector3(
                    Mathf.Cos(angleRad) * apothem,
                    height,
                    Mathf.Sin(angleRad) * apothem);

                // El lado de un hexágono regular mide lo mismo que su radio.
                segment.transform.localScale = new Vector3(radius, thickness, thickness);

                // El eje local X del cubo se alinea con la tangente del lado.
                segment.transform.localRotation = Quaternion.Euler(0f, -(angleDeg + 90f), 0f);

                var collider = segment.GetComponent<Collider>();
                SafeObjects.Destroy(collider);

                segment.GetComponent<MeshRenderer>().sharedMaterial = material;
            }

            return root;
        }
    }
}
