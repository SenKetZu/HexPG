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
    /// Genera las tres escenas de demostración y los assets de P3 desde código, para que el
    /// repositorio no dependa de wiring manual hecho a mano en el editor.
    /// </summary>
    public static class PrototypeSceneBuilder
    {
        private const string P1Scene = "Assets/Prototypes/P1_GridState/Scenes/P1_GridState.unity";
        private const string P2Scene = "Assets/Prototypes/P2_CommandPipeline/Scenes/P2_CommandPipeline.unity";
        private const string P3Scene = "Assets/Prototypes/P3_Loadout/Scenes/P3_Loadout.unity";

        private const string P3Assets = "Assets/Prototypes/P3_Loadout/Assets";

        [MenuItem("VoidHex/Build Prototype Scenes")]
        public static void BuildAll()
        {
            EnsureFolder("Assets/Prototypes/P1_GridState/Scenes");
            EnsureFolder("Assets/Prototypes/P2_CommandPipeline/Scenes");
            EnsureFolder("Assets/Prototypes/P3_Loadout/Scenes");
            EnsureFolder(P3Assets + "/Modules");

            BuildP1();
            BuildP2();
            BuildP3();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(P1Scene, true),
                new EditorBuildSettingsScene(P2Scene, true),
                new EditorBuildSettingsScene(P3Scene, true),
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VoidHex] Escenas de prototipos generadas.");
        }

        private static void BuildP1()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var grid = new GameObject("P1_Grid");
            grid.AddComponent<GridBootstrap>();
            grid.AddComponent<GridPresenter>();

            EditorSceneManager.SaveScene(scene, P1Scene);
        }

        private static void BuildP2()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var driver = new GameObject("P2_TurnDemo");
            driver.AddComponent<TurnDemoDriver>();

            EditorSceneManager.SaveScene(scene, P2Scene);
        }

        private static void BuildP3()
        {
            var config = CreateOrLoad<ShipConfigAsset>(P3Assets + "/ShipConfig.asset");
            config.Initialize(actionPoints: 6, shields: 0, movement: 3, hull: 10);
            EditorUtility.SetDirty(config);

            var laserMk1 = CreateModule("laser-mk1", "Láser MK I", SlotType.Weapon, 1, 0, 0, 0, "beam");
            var laserMk2 = CreateModule("laser-mk2", "Láser MK II", SlotType.Weapon, 2, 0, 2, 0, "beam");
            var thrusterMk1 = CreateModule("thruster-mk1", "Propulsor MK I", SlotType.Propulsion, 1, 0, 0, 1, string.Empty);
            var shieldMk1 = CreateModule("shield-mk1", "Escudo MK I", SlotType.Shield, 1, 0, 3, 0, "barrier");

            var catalog = CreateOrLoad<ModuleCatalogAsset>(P3Assets + "/ModuleCatalog.asset");
            catalog.Initialize(new[] { laserMk1, laserMk2, thrusterMk1, shieldMk1 });
            EditorUtility.SetDirty(catalog);

            AssetDatabase.SaveAssets();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var root = new GameObject("P3_Loadout");
            root.AddComponent<LoadoutDemo>();
            var scope = root.AddComponent<EquipmentLifetimeScope>();

            var serialized = new SerializedObject(scope);
            serialized.FindProperty("shipConfig").objectReferenceValue = config;
            serialized.FindProperty("moduleCatalog").objectReferenceValue = catalog;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, P3Scene);
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

        private static void EnsureFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }
    }
}
