using UnityEngine;
using VoidHex.P1.Domain;

namespace VoidHex.P1.Unity
{
    /// <summary>
    /// Panel de control del prototipo P1 en Play Mode (IMGUI, sin prefabs ni Canvas).
    /// Permite provocar mutaciones reales y comprobar en pantalla cuántos eventos se emitieron.
    /// Es herramienta de demostración, no UI de producto.
    /// </summary>
    public sealed class GridHud : MonoBehaviour
    {
        [SerializeField] private GridBootstrap bootstrap;

        private Vector2 _scroll;

        private void OnEnable()
        {
            if (bootstrap == null) bootstrap = GetComponent<GridBootstrap>();
        }

        private void OnGUI()
        {
            if (bootstrap == null) return;

            GUILayout.BeginArea(new Rect(12, 12, 430, 470), GUI.skin.box);
            _scroll = GUILayout.BeginScrollView(_scroll);

            GUILayout.Label("<b>P1 · ISP + Observer</b>", RichLabel(15));
            GUILayout.Label($"Mapa {bootstrap.Width}×{bootstrap.Height}   ·   eventos recibidos: <b>{bootstrap.ChangeCount}</b>",
                RichLabel(12));

            GUILayout.Space(6);
            GUILayout.Label($"Origen {bootstrap.StartCell}  →  Destino {bootstrap.GoalCell}", RichLabel(12));

            if (bootstrap.LastPathFound)
            {
                GUILayout.Label($"Ruta: <b>{bootstrap.LastPath.Count} celdas</b>, costo <b>{bootstrap.LastPathCost}</b>",
                    RichLabel(12));
            }
            else
            {
                GUILayout.Label($"Sin ruta: <b>{bootstrap.LastFailure}</b>", RichLabel(12));
            }

            GUILayout.Space(8);
            GUILayout.Label("Mutaciones reales (deben sumar exactamente 1 evento cada una)", RichLabel(11));

            var probe = new HexCoordinates(6, 6);

            if (GUILayout.Button("Bloquear celda (6,6)"))
            {
                bootstrap.DemoBlock(probe);
                bootstrap.RecomputePath();
            }

            if (GUILayout.Button("Liberar celda (6,6)"))
            {
                bootstrap.DemoOpen(probe);
                bootstrap.RecomputePath();
            }

            if (GUILayout.Button("Ocupar / liberar celda (2,2)"))
            {
                bootstrap.DemoToggleOccupied(new HexCoordinates(2, 2));
            }

            GUILayout.Space(8);
            GUILayout.Label("Escritura SIN cambio (no debe sumar ningún evento)", RichLabel(11));

            if (GUILayout.Button("Reescribir (6,6) con el mismo valor"))
            {
                var mutated = bootstrap.DemoRewriteSame(probe);
                Debug.Log($"[P1] Escritura idéntica → mutó: {mutated} (se espera False).");
            }

            GUILayout.Space(8);
            if (GUILayout.Button("Recalcular ruta")) bootstrap.RecomputePath();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private static GUIStyle RichLabel(int size)
        {
            var style = new GUIStyle(GUI.skin.label) { richText = true, fontSize = size };
            return style;
        }
    }
}
