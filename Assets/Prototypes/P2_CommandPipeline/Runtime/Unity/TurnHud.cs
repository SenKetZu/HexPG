using UnityEngine;
using VoidHex.P2.Application;
using VoidHex.P2.Domain;

namespace VoidHex.P2.Unity
{
    /// <summary>
    /// Panel de control del prototipo P2 en Play Mode (IMGUI).
    /// Permite enviar comandos y ver en pantalla que el estado se aplica antes de que la animación termine.
    /// </summary>
    public sealed class TurnHud : MonoBehaviour
    {
        [SerializeField] private TurnDemoDriver driver;

        private void OnEnable()
        {
            if (driver == null) driver = GetComponent<TurnDemoDriver>();
        }

        private void OnGUI()
        {
            if (driver == null) return;

            var state = driver.State;

            GUILayout.BeginArea(new Rect(12, 12, 430, 470), GUI.skin.box);

            GUILayout.Label("<b>P2 · Pipeline ICommand + async</b>", Label(15));
            GUILayout.Label($"Fase: <b>{driver.Phase}</b>", Label(12));
            GUILayout.Space(4);

            GUILayout.Label($"PA: <b>{state.ActionPoints}</b> / {state.MaxActionPoints}", Label(13));
            GUILayout.Label($"Posición: {state.Position}   Escudos: {state.Shields}", Label(12));
            GUILayout.Label($"Enemigo en {state.EnemyPosition}   Hull: <b>{state.EnemyHull}</b>", Label(12));

            GUILayout.Space(8);
            if (driver.LastOutcome.Validation.IsAllowed || driver.LastOutcome.IsAccepted)
            {
                GUILayout.Label("Último comando: <b>aceptado</b>", Label(12));
            }
            else if (driver.LastOutcome.Validation.Rejection != CommandRejection.None)
            {
                GUILayout.Label($"Último comando: <b>rechazado</b> ({driver.LastOutcome.Validation.Rejection})",
                    Label(12));
            }

            GUILayout.Space(8);
            GUILayout.Label("Comandos intercalados", Label(11));

            if (GUILayout.Button("Mover a (2,0)  ·  2 PA"))
            {
                _ = driver.SubmitAsync(new MoveCommand(new HexCoordinates(2, 0), 2));
            }

            if (GUILayout.Button("Mover a (3,0)  ·  1 PA"))
            {
                _ = driver.SubmitAsync(new MoveCommand(new HexCoordinates(3, 0), 1));
            }

            if (GUILayout.Button("Atacar  ·  2 PA ·  daño 3 ·  rango 3"))
            {
                _ = driver.SubmitAsync(new AttackCommand(2, 3, 3));
            }

            if (GUILayout.Button("Escudo  ·  2 PA ·  +4"))
            {
                _ = driver.SubmitAsync(new ShieldCommand(2, 4));
            }

            GUILayout.Space(6);
            if (GUILayout.Button("Rechazo forzado: mover con 3 PA de costo"))
            {
                _ = driver.SubmitAsync(new MoveCommand(new HexCoordinates(5, 0), 3));
            }

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Terminar turno (recarga PA)")) driver.EndTurn();
            if (GUILayout.Button("Turno enemigo")) driver.BeginEnemyTurn();
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Reiniciar")) driver.Restart();

            GUILayout.EndArea();
        }

        private static GUIStyle Label(int size)
        {
            return new GUIStyle(GUI.skin.label) { richText = true, fontSize = size };
        }
    }
}
