using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoidHex.P1.Unity;
using VoidHex.P2.Unity;
using VoidHex.P3.Application;
using VoidHex.P3.Domain;
using VoidHex.P3.Unity;

namespace VoidHex.Prototypes.Editor
{
    /// <summary>
    /// Genera las escenas de demostración y los assets de P3 desde código, para que el repositorio
    /// no dependa de wiring manual en el editor. Las escenas se pueden regenerar con el menú
    /// <c>VoidHex → Build Prototype Scenes</c> en cualquier momento.
    /// </summary>
    public static class PrototypeSceneBuilder
    {
        public const string P1Scene = "Assets/Prototypes/P1_GridState/Scenes/P1_GridState.unity";
        public const string P2Scene = "Assets/Prototypes/P2_CommandPipeline/Scenes/P2_CommandPipeline.unity";
        public const string P3Scene = "Assets/Prototypes/P3_Loadout/Scenes/P3_Loadout.unity";

        private const string P3Assets = "Assets/Prototypes/P3_Loadout/Assets";

        public static readonly string[] AllScenes = { P1Scene, P2Scene, P3Scene };

        public static bool ScenesExist()
        {
            foreach (var path in AllScenes)
            {
                if (!File.Exists(path)) return false;
            }

            return true;
        }

        /// <summary>Genera solo las escenas que falten. Seguro para llamar desde un hook automático.</summary>
        public static bool EnsureAll()
        {
            if (ScenesExist()) return false;

            BuildInternal(force: false);
            return true;
        }

        [MenuItem("VoidHex/Build Prototype Scenes")]
        public static void BuildAll() => BuildInternal(force: true);

        private static void BuildInternal(bool force)
        {
            var previousScene = EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.SaveOpenScenes();

            EnsureFolder("Assets/Prototypes/P1_GridState/Scenes");
            EnsureFolder("Assets/Prototypes/P2_CommandPipeline/Scenes");
            EnsureFolder("Assets/Prototypes/P3_Loadout/Scenes");
            EnsureFolder(P3Assets + "/Modules");

            if (force || !File.Exists(P1Scene)) BuildP1();
            if (force || !File.Exists(P2Scene)) BuildP2();
            if (force || !File.Exists(P3Scene)) BuildP3();

            RegisterScenesInBuildSettings();
            AssetDatabase.SaveAssets();

            if (!string.IsNullOrEmpty(previousScene)) EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);

            Debug.Log("[VoidHex] Escenas de prototipos listas: P1_GridState, P2_CommandPipeline, P3_Loadout.");
        }

        // ------------------------------------------------------------------ P1

        private static void BuildP1()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var root = new GameObject("P1_Grid");
            root.AddComponent<GridBootstrap>();
            root.AddComponent<GridCellRenderer>();
            root.AddComponent<GridPresenter>();
            root.AddComponent<GridHud>();

            FrameCamera(new Vector3(8.25f, 24f, -14f), new Vector3(8.25f, 0f, 14f));

            EditorSceneManager.SaveScene(scene, P1Scene);
        }

        // ------------------------------------------------------------------ P2

        private static void BuildP2()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var root = new GameObject("P2_TurnDemo");
            root.AddComponent<ShipView>();
            root.AddComponent<TurnDemoDriver>();
            root.AddComponent<TurnHud>();

            AddHexFloor(3, 0, 5);

            FrameCamera(new Vector3(3f, 8f, -8f), new Vector3(2.5f, 0f, 1.5f));

            EditorSceneManager.SaveScene(scene, P2Scene);
        }

        // ------------------------------------------------------------------ P3

        private static void BuildP3()
        {
            var config = CreateOrLoad<ShipConfigAsset>(P3Assets + "/ShipConfig.asset");
            config.Initialize(actionPoints: 6, shields: 0, movement: 3, hull: 10);
            EditorUtility.SetDirty(config);

            var laserMk1 = CreateModule("laser-mk1", "Láser MK I", SlotType.Weapon, 1, 0, 0, 0, "beam");
            var laserMk2 = CreateModule("laser-mk2", "Láser MK II", SlotType.Weapon, 2, 0, 0, 2, "beam");
            var laserMk3 = CreateModule("laser-mk3", "Láser MK III", SlotType.Weapon, 3, 1, 0, 4, "beam-iii");
            var thrusterMk1 = CreateModule("thruster-mk1", "Propulsor MK I", SlotType.Propulsion, 1, 0, 0, 1, string.Empty);
            var shieldMk1 = CreateModule("shield-mk1", "Escudo MK I", SlotType.Shield, 1, 0, 3, 0, "barrier");

            var catalog = CreateOrLoad<ModuleCatalogAsset>(P3Assets + "/ModuleCatalog.asset");
            catalog.Initialize(new[] { laserMk1, laserMk2, laserMk3, thrusterMk1, shieldMk1 });
            EditorUtility.SetDirty(catalog);

            AssetDatabase.SaveAssets();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var root = new GameObject("P3_Loadout");
            root.AddComponent<LoadoutDemo>();
            root.AddComponent<LoadoutHud>();
            var scope = root.AddComponent<EquipmentLifetimeScope>();

            var serialized = new SerializedObject(scope);
            serialized.FindProperty("shipConfig").objectReferenceValue = config;
            serialized.FindProperty("moduleCatalog").objectReferenceValue = catalog;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            FrameCamera(new Vector3(0f, 3f, -6f), Vector3.zero);

            EditorSceneManager.SaveScene(scene, P3Scene);
        }

        // ------------------------------------------------------------------ helpers

        private static void FrameCamera(Vector3 position, Vector3 lookAt)
        {
            var camera = Camera.main;
            if (camera == null) return;

            camera.transform.position = position;
            camera.transform.LookAt(lookAt);
        }

        /// <summary>Suelo de referencia para que el movimiento de las naves sea legible en Play Mode.</summary>
        private static void AddHexFloor(int fromQ, int toQ, int radius)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var material = new Material(shader) { name = "P2_FloorMaterial" };
            material.SetColor("_BaseColor", new Color(0.13f, 0.17f, 0.24f));
            material.SetColor("_Color", new Color(0.13f, 0.17f, 0.24f));

            var path = "Assets/Prototypes/P2_CommandPipeline/Scenes/P2_FloorMaterial.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.shader = material.shader;
                existing.SetColor("_BaseColor", new Color(0.13f, 0.17f, 0.24f));
                existing.SetColor("_Color", new Color(0.13f, 0.17f, 0.24f));
                material = existing;
            }
            else
            {
                AssetDatabase.CreateAsset(material, path);
            }

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            var width = (toQ - fromQ + 2) * 1.5f;
            floor.transform.position = new Vector3((fromQ + toQ) * 0.75f, -0.06f, radius * 0.9f);
            floor.transform.localScale = new Vector3(width, 0.1f, radius * 3.2f);

            var collider = floor.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);

            floor.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static ShipModuleAsset CreateModule(
            string id,
            string label,
            SlotType slot,
            int tier,
            int actionPoints,
            int shields,
            int movement,
            string ability)
        {
            var asset = CreateOrLoad<ShipModuleAsset>(P3Assets + "/Modules/" + id + ".asset");
            asset.Initialize(id, label, slot, tier, actionPoints, shields, movement, ability);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        private static void RegisterScenesInBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(P1Scene, true),
                new EditorBuildSettingsScene(P2Scene, true),
                new EditorBuildSettingsScene(P3Scene, true),
            };
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            var parts = path.Split('/');
            var current = parts[0];

            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }

            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }
    }
}
