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
            Color? tint = null)
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

            if (tint.HasValue) TintRenderers(instance, tint.Value);

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
        /// Aplica un color a todas las mallas. Los FBX de Kenney no traen texturas, así que
        /// el color es justamente lo que comunica el estado (asteroide, nave, enemigo).
        /// </summary>
        public static void TintRenderers(GameObject instance, Color color)
        {
            var block = new MaterialPropertyBlock();
            var renderers = instance.GetComponentsInChildren<Renderer>();
            var material = CreateLitMaterial(color);

            foreach (var renderer in renderers)
            {
                renderer.sharedMaterial = material;

                block.Clear();
                block.SetColor(BaseColorId, color);
                block.SetColor(ColorId, color);
                renderer.SetPropertyBlock(block);
            }
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();

        public static Material CreateLitMaterial(Color color, float emission = 0f)
        {
            if (emission <= 0f && Materials.TryGetValue(color, out var cached) && cached != null) return cached;

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
            else
            {
                Materials[color] = material;
            }

            return material;
        }
    }
}
