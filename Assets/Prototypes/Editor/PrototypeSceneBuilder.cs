using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using VoidHex.GameLoop.Logic;
using VoidHex.GameLoop.Unity;
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

        /// <summary>Escena de composición: el bucle completo usando los tres prototipos.</summary>
        public const string GameLoopScene = "Assets/GameLoop/Scenes/GameLoop.unity";

        private const string P3Assets = "Assets/Prototypes/P3_Loadout/Assets";
        private const string P2Materials = "Assets/Prototypes/P2_CommandPipeline/Scenes";

        public static readonly string[] AllScenes = { P1Scene, P2Scene, P3Scene, GameLoopScene };

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
            EnsureFolder("Assets/GameLoop/Scenes");

            if (force || !File.Exists(P1Scene)) BuildP1();
            if (force || !File.Exists(P2Scene)) BuildP2();
            if (force || !File.Exists(P3Scene)) BuildP3();
            if (force || !File.Exists(GameLoopScene)) BuildGameLoop();

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
            root.AddComponent<GridScreen>();

            // Encuadre a partir de la extensión REAL del mapa axial (es un rombo, no un rectángulo).
            // Una sola fuente de verdad para la separación: GridCellRenderer.Spacing.
            var spacing = GridCellRenderer.Spacing;
            const float hexZ = 1.7320508f;

            var extentX = 1.5f * spacing * (width - 1);
            var extentZ = hexZ * spacing * ((height - 1) + (width - 1) * 0.5f);
            var center = new Vector3(extentX * 0.5f, 0f, extentZ * 0.5f);

            FrameOrthoTopDown(center, halfExtentZ: extentZ * 0.5f, shiftRightFraction: 0.16f);

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
            root.AddComponent<TurnScreen>();

            CreateBoard(width: 5, height: 4);

            FrameCameraOnExtent(new Vector3(2.1f, 0.3f, 1.6f), halfExtentZ: 2.3f, fov: 42f, tiltDeg: 44f,
                shiftRightFraction: 0.16f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, P2Scene);
        }

        // ------------------------------------------------------------------ P3

        private static void BuildP3()
        {
            EnsureLoadoutAssets(out var config, out var catalog);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupEnvironment();

            var root = new GameObject("P3_Loadout");
            root.AddComponent<LoadoutDemo>();
            root.AddComponent<LoadoutScreen>();
            // Vista 3D en el mismo GameObject: comparte el LoadoutDemo sin buscarlo por la escena.
            root.AddComponent<ShipPreview>();
            var scope = root.AddComponent<EquipmentLifetimeScope>();

            // Asignación explícita: no depende de nombres de campos serializados.
            scope.EditorAssign(config, catalog);
            EditorUtility.SetDirty(scope);

            FrameCameraOnExtent(new Vector3(-0.6f, 0.15f, 0f), halfExtentZ: 1.05f, fov: 36f, tiltDeg: 32f,
                shiftRightFraction: 0.08f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, P3Scene);
        }

        /// <summary>
        /// Crea los assets de P3 si no existen y devuelve las referencias.
        /// No pisa un catálogo ya poblado: regenerar escenas no debe borrar contenido del diseñador.
        /// </summary>
        private static void EnsureLoadoutAssets(out ShipConfigAsset config, out ModuleCatalogAsset catalog)
        {
            config = CreateOrLoad<ShipConfigAsset>(P3Assets + "/ShipConfig.asset");
            if (config.ToBaseStats().ActionPoints <= 0)
            {
                config.Initialize(actionPoints: 6, shields: 0, movement: 3, hull: 10);
                EditorUtility.SetDirty(config);
            }

            catalog = CreateOrLoad<ModuleCatalogAsset>(P3Assets + "/ModuleCatalog.asset");

            if (catalog.All.Count == 0)
            {
                var laserMk1 = CreateModule("laser-mk1", "Láser MK I", SlotType.Weapon, 1, 0, 0, 0, "beam");
                var laserMk2 = CreateModule("laser-mk2", "Láser MK II", SlotType.Weapon, 2, 0, 0, 2, "beam");
                var laserMk3 = CreateModule("laser-mk3", "Láser MK III", SlotType.Weapon, 3, 1, 0, 4, "beam-iii");
                var thruster = CreateModule("thruster-mk1", "Propulsor MK I", SlotType.Propulsion, 1, 0, 0, 1,
                    string.Empty);
                var shield = CreateModule("shield-mk1", "Escudo MK I", SlotType.Shield, 1, 0, 3, 0, "barrier");

                catalog.Initialize(new[] { laserMk1, laserMk2, laserMk3, thruster, shield });
                EditorUtility.SetDirty(catalog);
            }

            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ game loop

        /// <summary>
        /// Escena de composición: no aporta reglas nuevas, une las tres.
        /// Hangar (P3) → despliegue → combate sobre la grilla (P1 + P2) → resultado → hangar.
        /// </summary>
        private static void BuildGameLoop()
        {
            EnsureLoadoutAssets(out var config, out var catalog);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupEnvironment();

            var root = new GameObject("GameLoop");
            var bootstrap = root.AddComponent<LoopBootstrap>();
            root.AddComponent<LoopGridView>();
            root.AddComponent<ShipView>();
            root.AddComponent<LoopDriver>();
            root.AddComponent<GameLoopScreen>();

            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("shipConfig").objectReferenceValue = config;
            serialized.FindProperty("moduleCatalog").objectReferenceValue = catalog;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // El encuadre usa la MISMA proyección que el clic, y deja el tablero en la franja
            // libre a la derecha del panel.
            const int width = 8;
            const int height = 6;
            var spacing = HexProjection.Spacing;
            var extentX = 1.5f * spacing * (width - 1);
            var extentZ = 1.7320508f * spacing * ((height - 1) + (width - 1) * 0.5f);
            var center = new Vector3(extentX * 0.5f, 0f, extentZ * 0.5f);

            FrameInFreeArea(center, halfExtentX: extentX * 0.5f, halfExtentZ: extentZ * 0.5f,
                panelFraction: 0.30f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, GameLoopScene);
        }

        // ------------------------------------------------------------------ entorno

        /// <summary>Cámara, luz direccional y ambiente. Sin esto las escenas se ven negras.</summary>
        /// <summary>
        /// Encuadre ortográfico cenital que deja el contenido DENTRO del área libre de UI.
        ///
        /// A diferencia de un simple desplazamiento, calcula el tamaño ortográfico para que el tablero
        /// entre completo en la franja derecha y centra el contenido en esa franja. Un desplazamiento
        /// fijo dependía del aspect ratio y cortaba la grilla en 16:9.
        /// </summary>
        private static void FrameInFreeArea(Vector3 center, float halfExtentX, float halfExtentZ,
            float panelFraction, float assumedAspect = 16f / 9f)
        {
            var camera = Camera.main;
            if (camera == null) return;

            var free = Mathf.Clamp(1f - panelFraction, 0.25f, 1f);

            // El tamaño vertical debe cubrir el alto del tablero y también su ancho dentro de `free`.
            var neededByHeight = halfExtentZ * 1.10f;
            var neededByWidth = halfExtentX * 1.10f / (assumedAspect * free);

            var orthoSize = Mathf.Max(neededByHeight, neededByWidth);

            camera.orthographic = true;
            camera.orthographicSize = orthoSize;
            camera.transform.SetPositionAndRotation(
                new Vector3(center.x, 40f, center.z),
                Quaternion.Euler(90f, 0f, 0f));

            // Centro de la franja libre en coordenadas normalizadas (-1..1).
            var freeCenterNormalized = -1f + free;
            var worldOffset = orthoSize * assumedAspect * freeCenterNormalized;

            camera.transform.position = new Vector3(center.x - worldOffset, 40f, center.z);
        }

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
        /// Encuadre ortográfico cenital: el tamaño depende solo de la extensión del mapa, así que
        /// entra completo sin importar el aspect ratio. Desplaza la cámara en -X para que el mapa
        /// quede a la derecha y el HUD (izquierda) no lo tape.
        /// </summary>
        private static void FrameOrthoTopDown(Vector3 center, float halfExtentZ, float shiftRightFraction = 0.30f)
        {
            var camera = Camera.main;
            if (camera == null) return;

            camera.orthographic = true;
            camera.orthographicSize = halfExtentZ * 1.12f;
            camera.transform.SetPositionAndRotation(
                center + new Vector3(0f, 40f, 0f),
                Quaternion.Euler(90f, 0f, 0f));

            var visibleHalfWidth = camera.orthographicSize * Mathf.Max(camera.aspect, 1.2f);
            camera.transform.position += Vector3.left * (visibleHalfWidth * shiftRightFraction);
        }

        /// <summary>
        /// Encuadre perspectiva: distancia derivada del FOV para que la extensión vertical entre
        /// con margen. El desplazamiento lateral corre el contenido a la derecha, lejos del HUD.
        /// </summary>
        private static void FrameCameraOnExtent(
            Vector3 center,
            float halfExtentZ,
            float fov,
            float tiltDeg,
            float shiftRightFraction = 0.22f)
        {
            var camera = Camera.main;
            if (camera == null) return;

            camera.orthographic = false;

            var halfFov = fov * 0.5f * Mathf.Deg2Rad;
            var distance = halfExtentZ / Mathf.Tan(halfFov) * 1.55f;

            var tilt = tiltDeg * Mathf.Deg2Rad;
            var offset = new Vector3(0f, Mathf.Sin(tilt) * distance, -Mathf.Cos(tilt) * distance);

            camera.fieldOfView = fov;
            camera.transform.position = center + offset;
            camera.transform.LookAt(center);

            // Corre el contenido a la derecha: la cámara se mueve a la izquierda en su espacio local.
            camera.transform.position -= camera.transform.right * (distance * shiftRightFraction * 0.45f);
        }

        /// <summary>
        /// Tablero de casillas hexagonales planas. Reemplaza la losa continua: da referencia
        /// espacial al movimiento y hace legible la posición de cada nave.
        /// </summary>
        private static void CreateBoard(int width = 5, int height = 4)
        {
            var baseMaterial = CreateOrLoadMaterial(P2Materials + "/P2_TileMaterial.mat",
                new Color(0.16f, 0.21f, 0.30f));
            var accentMaterial = CreateOrLoadMaterial(P2Materials + "/P2_TileAccentMaterial.mat",
                new Color(0.20f, 0.30f, 0.44f));

            var board = new GameObject("Board").transform;

            for (var q = 0; q < width; q++)
            {
                for (var r = 0; r < height; r++)
                {
                    var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = "tile_" + q + "_" + r;
                    tile.transform.SetParent(board, false);

                    // Misma conversión axial y separación que la lógica: el tablero coincide con las celdas.
                    const float spacing = 0.7f;
                    tile.transform.localPosition = new Vector3(
                        1.5f * q * spacing,
                        -0.03f,
                        1.7320508f * (r + q * 0.5f) * spacing);
                    tile.transform.localScale = new Vector3(0.66f, 0.06f, 0.66f);

                    RemoveCollider(tile);
                    tile.GetComponent<Renderer>().sharedMaterial =
                        (q + r) % 2 == 0 ? baseMaterial : accentMaterial;
                }
            }
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
                new EditorBuildSettingsScene(GameLoopScene, true),
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
