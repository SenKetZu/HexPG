using System.Collections.Generic;
using UnityEngine;

namespace VoidHex.UiKit
{
    /// <summary>
    /// Acceso a los modelos 3D del proyecto (Kenney Space Kit, CC0).
    ///
    /// Los modelos se referencian por nombre lógico y se cargan con <c>Resources.Load</c>: así la
    /// presentación no depende de referencias serializadas que se rompen al regenerar escenas.
    ///
    /// Si un modelo falta, se devuelve <c>null</c> y quien llama cae a una primitiva: la demo
    /// nunca queda vacía por un asset ausente.
    /// </summary>
    public static class ModelLibrary
    {
        public const string Root = "Models";

        private static readonly Dictionary<string, GameObject> Cache = new Dictionary<string, GameObject>();

        // Nombres lógicos: coinciden con los archivos en Assets/Resources/Models.
        public const string ShipSpeederA = "Ship_SpeederA";
        public const string ShipSpeederB = "Ship_SpeederB";
        public const string ShipRacer = "Ship_Racer";
        public const string EnemyTurret = "Enemy_Turret";
        public const string RockMeteor = "Rock_Meteor";
        public const string RockMeteorDetailed = "Rock_MeteorDetailed";
        public const string RockMeteorHalf = "Rock_MeteorHalf";
        public const string RockLargeA = "Rock_LargeA";
        public const string RockCrystals = "Rock_Crystals";
        public const string RockDebris = "Rock_Debris";
        public const string PlatformSmall = "Platform_Small";
        public const string PlatformCenter = "Platform_Center";
        public const string PropBarrel = "Prop_Barrel";

        public static GameObject Load(string modelName)
        {
            if (string.IsNullOrEmpty(modelName)) return null;
            if (Cache.TryGetValue(modelName, out var cached)) return cached;

            var asset = Resources.Load<GameObject>(Root + "/" + modelName);
            Cache[modelName] = asset;
            return asset;
        }

        /// <summary>
        /// Instancia un modelo normalizado: escala UNIFORME para que su eje mayor mida
        /// <paramref name="targetSize"/>, con el pivote apoyado en y = 0.
        ///
        /// Importante: se MULTIPLICA la escala existente en lugar de asignarla. Los FBX traen su
        /// propia escala de importación y pisarla convierte el modelo en una mota invisible.
        /// </summary>
        public static GameObject Spawn(string modelName, Transform parent, float targetSize = 1f,
            Color? accent = null, float emission = 0f)
        {
            var prefab = Load(modelName);
            if (prefab == null) return null;

            var instance = Object.Instantiate(prefab, parent, false);
            instance.name = modelName;

            var before = MeasureBounds(instance);
            var largest = Mathf.Max(before.size.x, Mathf.Max(before.size.y, before.size.z));

            if (largest > 0.0001f)
            {
                var factor = targetSize / largest;
                instance.transform.localScale *= factor;
            }

            // Recalcular tras escalar y compensar el pivote.
            // Kenney exporta con el pivote en una esquina: sin centrar, el modelo aparece corrido
            // respecto de la celda que representa (la nave se veía al lado de su propio anillo).
            //
            // La conversión se hace con InverseTransformPoint porque las bounds vienen en espacio
            // de mundo y el padre puede tener escala o rotación.
            var bounds = MeasureBounds(instance);
            var pivotParent = instance.transform.parent;

            if (pivotParent != null)
            {
                var center = bounds.center;
                var bottom = new Vector3(center.x, bounds.min.y, center.z);

                var localCenter = pivotParent.InverseTransformPoint(center);
                var localBottom = pivotParent.InverseTransformPoint(bottom);

                instance.transform.localPosition -= new Vector3(localCenter.x, localBottom.y, localCenter.z);
            }

            if (accent.HasValue) TintAccents(instance, accent.Value, emission);

            return instance;
        }

        /// <summary>Bounds en espacio de mundo del objeto instanciado (incluye su escala actual).</summary>
        public static Bounds MeasureBounds(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(instance.transform.position, Vector3.zero);

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        /// <summary>
        /// Repinta SOLO los submeshes de acento del modelo (trim, luces, cristales).
        ///
        /// Los FBX del Kenney Space Kit no traen texturas, pero sí traen sus materiales embebidos
        /// con la paleta del kit: <c>metal</c>, <c>metalDark</c>, <c>dark</c>, <c>metalRed</c>,
        /// <c>rock</c> y <c>crystal</c>. Repintar todo con un único material plano destruye ese
        /// desglose y deja las naves como siluetas de un solo color.
        ///
        /// Por eso se conserva el modelo tal cual viene y el color de equipo se aplica únicamente
        /// al acento: la nave se sigue leyendo como una nave de Kenney, pero se distingue a quién
        /// pertenece.
        /// </summary>
        public static void TintAccents(GameObject instance, Color color, float emission = 0f)
            => RepaintByMaterialName(instance, AccentKeywords, CreateLitMaterial(color, emission));

        /// <summary>
        /// Cambia el material de roca del kit por uno de asteroide.
        ///
        /// No es capricho: el kit pinta las rocas con <c>rockTrack</c> (arena, 0,99/0,79/0,70), que
        /// es el color del suelo de un planeta. Sobre el tablero espacial oscuro esas rocas se leían
        /// como manchas salmón, no como obstáculos. Se reemplazan SOLO los materiales de roca: los
        /// cristales conservan su teal, que sí funciona como acento.
        /// </summary>
        public static void RestyleRocks(GameObject instance, Color color, float emission = 0f)
            => RepaintByMaterialName(instance, RockKeywords, CreateLitMaterial(color, emission));

        /// <summary>
        /// Sustituye los materiales cuyo nombre contiene alguna de las claves.
        ///
        /// Los FBX del Kenney Space Kit no traen texturas, pero sí traen sus materiales embebidos
        /// con la paleta del kit: <c>metal</c>, <c>metalDark</c>, <c>dark</c>, <c>metalRed</c>,
        /// <c>rockTrack</c>, <c>rock</c> y <c>crystal</c>. Repintar todo con un único material plano
        /// destruye ese desglose y deja las naves como siluetas de un solo color; por eso se cambia
        /// slot por slot, por nombre, y lo que no matchea queda como vino del kit.
        /// </summary>
        private static void RepaintByMaterialName(GameObject instance, string[] keywords, Material replacement)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();

            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;

                if (materials.Length == 0)
                {
                    renderer.sharedMaterial = replacement;
                    continue;
                }

                var changed = false;
                for (var i = 0; i < materials.Length; i++)
                {
                    if (!NameContains(materials[i], keywords)) continue;

                    materials[i] = replacement;
                    changed = true;
                }

                // sharedMaterials devuelve una copia: asignar el array no toca el asset del FBX,
                // solo los renderers de esta instancia.
                if (changed) renderer.sharedMaterials = materials;
            }
        }

        /// <summary>
        /// Materiales de acento del kit. En la nave es el trim; en la torreta es la CÚPULA, que es
        /// la pieza que comunica a quién pertenece el arma.
        /// </summary>
        private static readonly string[] AccentKeywords = { "metalred", "accent" };

        /// <summary>Materiales de roca. <c>crystal</c> queda afuera a propósito.</summary>
        private static readonly string[] RockKeywords = { "rock" };

        private static bool NameContains(Material material, string[] keywords)
        {
            if (material == null) return false;

            var name = material.name;
            for (var i = 0; i < keywords.Length; i++)
            {
                if (name.IndexOf(keywords[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Repinta todas las mallas con un color plano.
        ///
        /// Se asignan TODOS los slots: en Unity 6 <c>renderer.sharedMaterial</c> solo escribe el
        /// slot 0 y deja los demás con el material original, así que el modelo quedaba a medio
        /// pintar (la plataforma del hangar mostraba un borde con el color del kit).
        /// </summary>
        public static void TintRenderers(GameObject instance, Color color, float emission = 0f)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            var material = CreateLitMaterial(color, emission);

            foreach (var renderer in renderers)
            {
                var slots = renderer.sharedMaterials.Length;

                if (slots <= 1)
                {
                    renderer.sharedMaterial = material;
                    continue;
                }

                var filled = new Material[slots];
                for (var i = 0; i < slots; i++) filled[i] = material;

                renderer.sharedMaterials = filled;
            }
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        /// <summary>
        /// Clave de caché de materiales. Incluye la emisión: sin ella, pedir el mismo color con dos
        /// emisiones distintas devolvía el primero y el brillo quedaba a merced del orden de llamadas.
        /// </summary>
        private readonly struct MaterialKey : System.IEquatable<MaterialKey>
        {
            private readonly Color _color;
            private readonly float _emission;

            public MaterialKey(Color color, float emission)
            {
                _color = color;
                _emission = emission;
            }

            public bool Equals(MaterialKey other)
            {
                return _color == other._color && Mathf.Approximately(_emission, other._emission);
            }

            public override bool Equals(object obj)
            {
                return obj is MaterialKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (_color.GetHashCode() * 397) ^ _emission.GetHashCode();
                }
            }
        }

        private static readonly Dictionary<MaterialKey, Material> Materials =
            new Dictionary<MaterialKey, Material>();

        public static Material CreateLitMaterial(Color color, float emission = 0f)
        {
            var key = new MaterialKey(color, emission);
            if (Materials.TryGetValue(key, out var cached) && cached != null) return cached;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var material = new Material(shader) { name = "Lit_" + ColorUtility.ToHtmlStringRGB(color) };
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);

            if (emission > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emission);
            }

            Materials[key] = material;
            return material;
        }
    }
}
