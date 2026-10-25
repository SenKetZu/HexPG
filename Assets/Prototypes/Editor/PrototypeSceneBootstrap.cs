using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VoidHex.Prototypes.Editor
{
    /// <summary>
    /// Genera las escenas automáticamente la primera vez que Unity compila el proyecto, para que
    /// clonar el repositorio y abrirlo alcance sin pasos manuales (requisito de reproducibilidad).
    ///
    /// Solo actúa si falta alguna escena: nunca sobrescribe escenas existentes ni pisa cambios.
    /// El menú <c>VoidHex → Build Prototype Scenes</c> sigue disponible para forzar la regeneración.
    /// </summary>
    [InitializeOnLoad]
    public static class PrototypeSceneBootstrap
    {
        private const string SessionKey = "VoidHex.PrototypeScenes.Checked.v2";

        private static bool _running;

        static PrototypeSceneBootstrap()
        {
            // Una sola comprobación por sesión del editor: evita bucles con cada domain reload.
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);

            // Diferido: crear escenas durante el domain reload es inseguro.
            EditorApplication.delayCall += TryEnsureScenes;
        }

        private static void TryEnsureScenes()
        {
            if (_running) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (PrototypeSceneBuilder.ScenesExist()) return;

            _running = true;
            try
            {
                Debug.Log("[VoidHex] Faltan escenas de prototipos: generándolas automáticamente…");
                PrototypeSceneBuilder.EnsureAll();
            }
            catch (Exception exception)
            {
                Debug.LogError("[VoidHex] No se pudieron generar las escenas automáticamente. " +
                               "Usá VoidHex → Build Prototype Scenes.\n" + exception);
            }
            finally
            {
                _running = false;
            }
        }

        [MenuItem("VoidHex/Open P1 GridState Scene")]
        private static void OpenP1() => OpenScene(PrototypeSceneBuilder.P1Scene);

        [MenuItem("VoidHex/Open P2 CommandPipeline Scene")]
        private static void OpenP2() => OpenScene(PrototypeSceneBuilder.P2Scene);

        [MenuItem("VoidHex/Open P3 Loadout Scene")]
        private static void OpenP3() => OpenScene(PrototypeSceneBuilder.P3Scene);

        [MenuItem("VoidHex/Open Game Loop Scene")]
        private static void OpenGameLoop() => OpenScene(PrototypeSceneBuilder.GameLoopScene);

        private static void OpenScene(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[VoidHex] La escena {path} no existe todavía. " +
                                 "Usá VoidHex → Build Prototype Scenes.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }
    }
}
