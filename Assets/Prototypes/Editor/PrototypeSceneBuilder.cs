using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using VoidHex.P1.Unity;
using VoidHex.P2.Unity;
using VoidHex.P3.Domain;
using VoidHex.P3.Unity;

namespace VoidHex.Prototypes.Editor
{
    /// <summary>
    /// Maqueta las tres escenas de demostración desde código, para que el repositorio no dependa
    /// de wiring manual en el editor.
    ///
    /// Cada escena queda con: cámara encuadrada, luz direccional, ambiente y fondo, y los componentes
    /// con sus referencias ya asignadas. Es idempotente: regenerar produce la misma escena.
    ///
    /// Menú: VoidHex → Build Prototype Scenes
    /// CLI : Unity -batchmode -quit -projectPath . -executeMethod VoidHex.Prototypes.Editor.PrototypeSceneBuilder.BuildAllFromCli
    /// </summary>
    public static class PrototypeSceneBuilder
    {
        public const string P1Scene = "Assets/Prototypes/P1_GridState/Scenes/P1_GridState.unity";
        public const string P2Scene = "Assets/Prototypes/P2_CommandPipeline/Scenes/P2_CommandPipeline.unity";
        public const string P3Scene = "Assets/Prototypes/P3_Loadout/Scenes/P3_Loadout.unity";

        private const string P3Assets = "Assets/Prototypes/P3_Loadout/Assets";
        private const string P2Materials = "Assets/Prototypes/P2_CommandPipeline/Scenes";

        public static readonly string[] AllScenes = { P1Scene, P2Scene, P3Scene };

        // Fondo y ambiente comunes: las tres escenas comparten estética con el HUD.
        private static readonly Color Background = new Color(0.06f, 0.09f, 0.16f);
        private static readonly Color Ambient = new Color(0.34f, 0.38f, 0.46f);

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

            Build(force: false);
            return true;
        }

        [MenuItem("VoidHex/Build Prototype Scenes")]
        public static void BuildAll() => Build(force: true);

        /// <summary>Entrada para batchmode: no toca escenas abiertas ni pide interacción.</summary>
        public static void BuildAllFromCli()
        {
            var ok = Build(force: true, verbose: true);
            if (!ok) EditorApplication.Exit(1);
        }

        private static bool Build(bool force, bool verbose = false)
        {
            var previousScene = EditorSceneManager.GetActiveScene().path;

            EnsureFolder("Assets/Prototypes/P1_GridState/Scenes");
            EnsureFolder("Assets/Prototypes/P2_CommandPipeline/Scenes");
            EnsureFolder("Assets/Prototypes/P3_Loadout/Scenes");
            EnsureFolder(P3Assets + "/Modules");

            if (force || !File.Exists(P1Scene)) BuildP1();
            if (force || !File.Exists(P2Scene)) BuildP2();
            if (force || !File.Exists(P3Scene)) BuildP3();

            RegisterScenesInBuildSettings();
            AssetDatabase.SaveAssets();

            if (!string.IsNullOrEmpty(previousScene) && File.Exists(previousScene))
            {
                EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
            }

            Debug.Log("[VoidHex] Escenas listas: P1_GridState, P2_CommandPipeline, P3_Loadout.");
            if (verbose) VerifyGeneratedScenes();
            return true;
        }

        // ------------------------------------------------------------------ P1

        private static void BuildP1()
        {
            const int width = 12;
            const int height = 12;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupEnvironment();

            var root = new GameObject("P1_Grid");
            var bootstrap = root.AddComponent<GridBootstrap>();

            // El HUD y el renderer toman la referencia del mismo GameObject (auto-wiring en OnEnable).
            root.AddComponent<GridCellRenderer>();
            root.AddComponent<GridPresenter>();
            root.AddComponent<GridHud>();

            // Encuadre calculado a partir de la extensión real del mapa.
            var maxX = 1.5f * (width - 1);
            var maxZ = 1.7320508f * (height - 1);
            var center = new Vector3(maxX * 0.5f, 0f, maxZ * 0.5f);

            FrameCameraOnExtent(center, halfExtentZ: maxZ * 0.5f, fov: 48f, tiltDeg: 58f);

            EditorUtility.SetDirty(bootstrap);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, P1Scene);
        }

        // ------------------------------------------------------------------ P2

        private static void BuildP2()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupEnvironment();

            var root = new GameObject("P2_TurnDemo");
            root.AddComponent<ShipView>();
            root.AddComponent<TurnDemoDriver>();
            root.AddComponent<TurnHud>();

            CreateHexFloor(fromQ: 0, toQ: 4, radius: 2);

            FrameCameraOnExtent(new Vector3(2.8f, 0.25f, 2.2f), halfExtentZ: 3.2f, fov: 50f, tiltDeg: 52f);

            EditorSceneManager.MarkSceneDirty(scene);
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
            var thruster = CreateModule("thruster-mk1", "Propulsor MK I", SlotType.Propulsion, 1, 0, 0, 1, string.Empty);
            var shield = CreateModule("shield-mk1", "Escudo MK I", SlotType.Shield, 1, 0, 3, 0, "barrier");

            var catalog = CreateOrLoad<ModuleCatalogAsset>(P3Assets + "/ModuleCatalog.asset");
            catalog.Initialize(new[] { laserMk1, laserMk2, laserMk3, thruster, shield });
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupEnvironment();

            var root = new GameObject("P3_Loadout");
            root.AddComponent<LoadoutDemo>();
            root.AddComponent<LoadoutHud>();
            var scope = root.AddComponent<EquipmentLifetimeScope>();

            // Asignación explícita: no depende de nombres de campos serializados.
            scope.EditorAssign(config, catalog);
            EditorUtility.SetDirty(scope);

            FrameCameraOnExtent(new Vector3(0f, 0.6f, 0f), halfExtentZ: 2.4f, fov: 55f, tiltDeg: 18f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, P3Scene);
        }

        // ------------------------------------------------------------------ entorno

        /// <summary>Cámara, luz direccional y ambiente. Sin esto las escenas se ven negras.</summary>
        private static void SetupEnvironment()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            cameraObject.AddComponent<AudioListener>();

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.97f, 0.92f);
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Ambient;
            RenderSettings.fog = false;
        }

        private static void PlaceCamera(Vector3 position, Vector3 lookAt, float fieldOfView)
        {
            var camera = Camera.main;
            if (camera == null) return;

            camera.transform.position = position;
            camera.transform.LookAt(lookAt);
            camera.fieldOfView = fieldOfView;
        }

        /// <summary>
        /// Coloca la cámara a la distancia justa para que la extensión vertical dada entre en cuadro,
        /// con una inclinación fija, y desplaza el encuadre a la derecha para que el HUD no tape el mapa.
        /// </summary>
        private static void FrameCameraOnExtent(
            Vector3 center,
            float halfExtentZ,
            float fov,
            float tiltDeg,
            float horizontalShift = 0.22f)
        {
            var camera = Camera.main;
            if (camera == null) return;

            var halfFov = fov * 0.5f * Mathf.Deg2Rad;
            var distance = halfExtentZ / Mathf.Tan(halfFov) * 1.18f;

            var tilt = tiltDeg * Mathf.Deg2Rad;
            var offset = new Vector3(0f, Mathf.Sin(tilt) * distance, -Mathf.Cos(tilt) * distance);

            camera.fieldOfView = fov;
            camera.transform.position = center + offset;
            camera.transform.LookAt(center);

            // Mueve la cámara a la derecha en su espacio local: el mapa se corre a la derecha en pantalla.
            camera.transform.position += camera.transform.right * (distance * horizontalShift * 0.45f);
        }

        /// <summary>Suelo de referencia con la misma geometría hexagonal que la lógica.</summary>
        private static void CreateHexFloor(int fromQ, int toQ, int radius)
        {
            var material = CreateOrLoadMaterial(P2Materials + "/P2_FloorMaterial.mat",
                new Color(0.13f, 0.17f, 0.24f));

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";

            var width = (toQ - fromQ + 2) * 1.5f;
            floor.transform.position = new Vector3((fromQ + toQ) * 0.75f, -0.08f, radius * 0.9f);
            floor.transform.localScale = new Vector3(width, 0.12f, radius * 3.4f);

            RemoveCollider(floor);
            floor.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void RemoveCollider(GameObject target)
        {
            var collider = target.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
        }

        // ------------------------------------------------------------------ assets

        private static Material CreateOrLoadMaterial(string path, Color color)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
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

        // ------------------------------------------------------------------ verificación

        /// <summary>
        /// Comprueba que las escenas recién generadas tengan lo mínimo para correr la demo.
        /// Corre en CLI para que el proceso falle con código 1 si algo quedó sin asignar.
        /// </summary>
        private static void VerifyGeneratedScenes()
        {
            var failures = 0;

            failures += VerifyScene(P1Scene, "P1_Grid", typeof(GridBootstrap));
            failures += VerifyScene(P2Scene, "P2_TurnDemo", typeof(TurnDemoDriver));
            failures += VerifyScene(P3Scene, "P3_Loadout", typeof(LoadoutDemo));

            Debug.Log(failures == 0
                ? "[VoidHex] Verificación de escenas: OK."
                : $"[VoidHex] Verificación de escenas: {failures} problema(s).");
        }

        private static int VerifyScene(string path, string rootName, System.Type expectedComponent)
        {
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var problems = 0;

            var root = GameObject.Find(rootName);
            if (root == null)
            {
                Debug.LogError($"[VoidHex] {path}: falta el GameObject '{rootName}'.");
                problems++;
            }
            else if (root.GetComponent(expectedComponent) == null)
            {
                Debug.LogError($"[VoidHex] {path}: '{rootName}' no tiene {expectedComponent.Name}.");
                problems++;
            }

            if (Camera.main == null)
            {
                Debug.LogError($"[VoidHex] {path}: falta la cámara principal.");
                problems++;
            }

            if (Object.FindAnyObjectByType<Light>() == null)
            {
                Debug.LogError($"[VoidHex] {path}: falta la luz direccional.");
                problems++;
            }

            if (problems == 0)
            {
                Debug.Log($"[VoidHex] {Path.GetFileName(path)}: OK (raíz, cámara y luz presentes).");
            }

            return problems;
        }
    }
}
