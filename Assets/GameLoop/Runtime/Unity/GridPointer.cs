using UnityEngine;
using UnityEngine.EventSystems;
using VoidHex.GameLoop.Logic;
using P1Coords = VoidHex.P1.Domain.HexCoordinates;

namespace VoidHex.GameLoop.Unity
{
    /// <summary>
    /// Traduce el clic a una casilla usando la proyección pura de <see cref="HexProjection"/>.
    ///
    /// No usa colliders: interseca el rayo con el plano del tablero y convierte el punto a coordenadas
    /// axiales. Así el tablero no necesita física y la matemática queda testeable sin escena.
    /// </summary>
    public static class GridPointer
    {
        public static bool TryPickCell(Camera camera, Vector2 screenPosition, int width, int height,
            out P1Coords cell)
        {
            cell = default;

            if (camera == null) return false;
            if (IsOverUi()) return false;

            var ray = camera.ScreenPointToRay(screenPosition);

            // Plano y = 0: el tablero. Si el rayo es paralelo o apunta hacia arriba, no hay clic válido.
            if (Mathf.Abs(ray.direction.y) < 0.0001f) return false;

            var distance = -ray.origin.y / ray.direction.y;
            if (distance < 0f) return false;

            var point = ray.origin + ray.direction * distance;

            if (!HexProjection.TryToCell(point.x, point.z, width, height, out var q, out var r)) return false;

            cell = new P1Coords(q, r);
            return true;
        }

        /// <summary>Un clic sobre un panel no debe mover la nave.</summary>
        private static bool IsOverUi()
            => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
