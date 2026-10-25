using System.Threading.Tasks;
using UnityEngine;
using VoidHex.P2.Domain;

namespace VoidHex.P2.Unity
{
    /// <summary>
    /// Adaptador de presentación: traduce un <see cref="CommandExecution"/> en una animación visual.
    /// La tarea se completa desde el ciclo de vida de Unity, nunca desde el dominio: cuando el
    /// <see cref="Application.CommandProcessor"/> vuelve de <c>Execute</c>, el estado ya cambió y esta
    /// animación recién empieza.
    /// </summary>
    public sealed class ShipView : MonoBehaviour, ICommandPresentation
    {
        [SerializeField] private int animationFrames = 8;
        [SerializeField] private bool showVisuals = true;

        private TaskCompletionSource<bool> _completion;
        private CommandExecution _execution;
        private int _framesTotal;
        private int _framesLeft;

        private Transform _player;
        private Transform _enemy;
        private Vector3 _playerFrom;
        private Vector3 _playerTo;
        private float _enemyFrom;
        private float _enemyTo;

        public int CompletedAnimations { get; private set; }

        public Task PlayAsync(CommandExecution execution)
        {
            Debug.Log($"[P2][View] Animación '{execution.Command.VisualKey}' " +
                      $"{execution.Before.Position} → {execution.After.Position}");

            _execution = execution;
            _framesTotal = Mathf.Max(1, animationFrames);
            _framesLeft = _framesTotal;

            if (showVisuals)
            {
                EnsureVisuals();
                BeginVisualTransition(execution);
            }

            _completion = new TaskCompletionSource<bool>();
            return _completion.Task;
        }

        private void BeginVisualTransition(CommandExecution execution)
        {
            if (_player != null)
            {
                _playerFrom = _player.position;
                _playerTo = ToWorld(execution.After.Position, 0.2f);
            }

            if (_enemy != null)
            {
                _enemyFrom = _enemy.localScale.y;
                _enemyTo = Mathf.Lerp(0.08f, 0.6f, Mathf.Clamp01(execution.After.EnemyHull / 6f));
                _enemy.position = ToWorld(execution.After.EnemyPosition, 0.2f);
            }
        }

        private void Update()
        {
            if (_completion == null) return;

            _framesLeft--;
            var t = 1f - Mathf.Clamp01(_framesLeft / (float)_framesTotal);

            if (showVisuals)
            {
                if (_player != null) _player.position = Vector3.Lerp(_playerFrom, _playerTo, t);

                if (_enemy != null)
                {
                    var hull = Mathf.Lerp(_enemyFrom, _enemyTo, t);
                    _enemy.localScale = new Vector3(0.8f, hull, 0.8f);
                }
            }

            if (_framesLeft > 0) return;

            if (showVisuals && _player != null) _player.position = _playerTo;

            var completion = _completion;
            _completion = null;
            CompletedAnimations++;
            completion.SetResult(true);
        }

        public void ResetVisuals()
        {
            if (!showVisuals) return;

            EnsureVisuals();
            if (_player != null) _player.position = ToWorld(ShipModel.Start().Position, 0.2f);
            if (_enemy != null) _enemy.localScale = new Vector3(0.8f, 0.6f, 0.8f);
        }

        private void EnsureVisuals()
        {
            if (_player == null)
            {
                _player = CreateShip("PlayerShip", new Color(0.18f, 0.83f, 0.75f));
                _player.position = ToWorld(ShipModel.Start().Position, 0.2f);
            }

            if (_enemy == null)
            {
                _enemy = CreateShip("EnemyShip", new Color(0.97f, 0.44f, 0.44f));
                _enemy.position = ToWorld(ShipModel.Start().EnemyPosition, 0.2f);
            }
        }

        private Transform CreateShip(string name, Color color)
        {
            var ship = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ship.name = name;
            ship.transform.SetParent(transform, false);
            ship.transform.localScale = new Vector3(0.8f, 0.6f, 0.8f);

            var collider = ship.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var material = new Material(shader) { name = name + "_Material" };
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);

            var renderer = ship.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return ship.transform;
        }

        /// <summary>Axial a mundo, solo para dibujar.</summary>
        private static Vector3 ToWorld(HexCoordinates cell, float y)
        {
            return new Vector3(1.5f * cell.Q, y, 1.7320508f * (cell.R + cell.Q * 0.5f));
        }
    }
}
