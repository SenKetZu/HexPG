using System.Threading.Tasks;
using UnityEngine;
using VoidHex.P2.Domain;
using VoidHex.UiKit;

namespace VoidHex.P2.Unity
{
    /// <summary>
    /// Adaptador de presentación con modelos reales (Kenney Space Kit).
    ///
    /// La tarea de animación se completa desde el ciclo de vida de Unity, nunca desde el dominio:
    /// cuando el <see cref="Application.CommandProcessor"/> vuelve de <c>Execute</c>, el estado ya
    /// cambió y esta animación recién empieza. Esa es la propiedad que defiende el prototipo.
    /// </summary>
    public sealed class ShipView : MonoBehaviour, ICommandPresentation
    {
        [SerializeField] private int animationFrames = 14;
        [SerializeField] private float tileSpacing = 0.7f;
        [SerializeField] private float hoverHeight = 0.22f;

        private TaskCompletionSource<bool> _completion;
        private int _framesTotal;
        private int _framesLeft;

        private Transform _player;
        private Transform _enemy;
        private Transform _playerRing;
        private Transform _enemyRing;
        private Material _ringMaterial;

        private Vector3 _playerFrom;
        private Vector3 _playerTo;
        private float _enemyFrom;
        private float _enemyTo;

        public int CompletedAnimations { get; private set; }

        private void Update()
        {
            if (_completion == null) return;

            _framesLeft--;
            var t = 1f - Mathf.Clamp01(_framesLeft / (float)_framesTotal);
            var eased = Mathf.SmoothStep(0f, 1f, t);

            if (_player != null) _player.position = Vector3.Lerp(_playerFrom, _playerTo, eased);

            if (_enemy != null)
            {
                var scale = Mathf.Lerp(_enemyFrom, _enemyTo, eased);
                _enemy.localScale = _enemyBaseScale * scale;
            }

            if (_framesLeft > 0) return;

            if (_player != null) _player.position = _playerTo;

            var completion = _completion;
            _completion = null;
            CompletedAnimations++;
            completion.SetResult(true);
        }

        public Task PlayAsync(CommandExecution execution)
        {
            Debug.Log($"[P2][View] Animación '{execution.Command.VisualKey}' " +
                      $"{execution.Before.Position} → {execution.After.Position}");

            EnsureVisuals();

            _framesTotal = Mathf.Max(1, animationFrames);
            _framesLeft = _framesTotal;

            _playerFrom = _player != null ? _player.position : Vector3.zero;
            _playerTo = ToWorld(execution.After.Position);

            _enemyFrom = 1f;
            _enemyTo = Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(execution.After.EnemyHull / 6f));

            _completion = new TaskCompletionSource<bool>();
            return _completion.Task;
        }

        public void ResetVisuals()
        {
            EnsureVisuals();

            var start = ShipModel.Start();

            if (_player != null) _player.position = ToWorld(start.Position);
            if (_enemy != null)
            {
                _enemy.position = ToWorld(start.EnemyPosition);
                _enemy.localScale = _enemyBaseScale;
            }
        }

        /// <summary>
        /// Coloca las naves en las posiciones de un estado concreto, sin animar.
        ///
        /// Lo usa el game loop al desplegar: la vista arranca sincronizada con el estado de la sesión
        /// en lugar de asumir las posiciones iniciales de la demo de P2.
        /// </summary>
        public void SnapTo(ShipModel state, int enemyHullMax)
        {
            EnsureVisuals();

            if (_player != null) _player.position = ToWorld(state.Position);

            if (_enemy != null)
            {
                _enemy.position = ToWorld(state.EnemyPosition);

                var normalized = enemyHullMax <= 0 ? 0f : Mathf.Clamp01(state.EnemyHull / (float)enemyHullMax);
                _enemy.localScale = _enemyBaseScale * Mathf.Lerp(0.45f, 1f, normalized );
            }
        }

        // ------------------------------------------------------------------ construcción

        private Vector3 _enemyBaseScale = Vector3.one;

        private void EnsureVisuals()
        {
            if (_player != null) return;

            var start = ShipModel.Start();

            _player = SpawnModel(ModelLibrary.ShipSpeederA, "PlayerShip", 1.02f,
                new Color(0.30f, 0.85f, 0.95f));
            _player.position = ToWorld(start.Position);

            // El enemigo es más grande que la nave y más grande que una casilla: es el objetivo del
            // juego, no un detalle del decorado. Con 0.55 se confundía con una roca.
            //
            // El modelo se deja con la paleta del kit. El color de equipo vive en el aro del piso:
            // el material de acento de la torreta (metalRed) es la CÚPULA, la pieza que más se ve
            // desde arriba; pintarla de rojo la convierte en un óvalo de color sin forma.
            _enemy = SpawnModel(ModelLibrary.EnemyTurret, "EnemyTurret", 1.12f,
                new Color(0.70f, 0.16f, 0.16f));
            _enemy.position = ToWorld(start.EnemyPosition);
            _enemyBaseScale = _enemy.localScale;

            _ringMaterial = ModelLibrary.CreateLitMaterial(new Color(0.25f, 0.85f, 0.95f), emission: 1.1f);
            _playerRing = BuildRing("PlayerRing", _ringMaterial, 0.42f, _player);
            _enemyRing = BuildRing("EnemyRing", ModelLibrary.CreateLitMaterial(
                new Color(0.98f, 0.30f, 0.26f), emission: 0.90f), 0.58f, _enemy);
        }

        private Transform SpawnModel(string modelName, string name, float size, Color fallbackColor)
        {
            var holder = new GameObject(name).transform;
            holder.SetParent(transform, false);

            // El modelo se deja tal como viene de Kenney (acero, paneles oscuros y cristales). El
            // color de equipo no se aplica sobre la malla: vive en el aro del piso, donde no compite
            // con la lectura del modelo. Solo la primitiva de respaldo se tiñe, porque no tiene una
            // paleta que preservar.
            var instance = ModelLibrary.Spawn(modelName, holder, size);
            if (instance == null)
            {
                // Respaldo: si el modelo falta, una primitiva mantiene la demo funcionando.
                instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
                instance.transform.SetParent(holder, false);
                instance.transform.localScale = Vector3.one * size;

                SafeObjects.Destroy(instance.GetComponent<Collider>());

                ModelLibrary.TintRenderers(instance, fallbackColor);
            }

            holder.localPosition = new Vector3(0f, hoverHeight, 0f);
            return holder;
        }

        /// <summary>
        /// Anillo de 14 segmentos, HIJO de la nave: marca la casilla ocupada y no puede
        /// desincronizarse con ella. Se compensa la altura de vuelo para quedar sobre el tablero.
        /// </summary>
        private Transform BuildRing(string name, Material material, float radius, Transform owner)
        {
            var root = new GameObject(name).transform;
            root.SetParent(owner, false);
            root.localPosition = new Vector3(0f, -hoverHeight + 0.012f, 0f);

            const int segments = 14;

            for (var i = 0; i < segments; i++)
            {
                var angle = i * Mathf.PI * 2f / segments;
                var segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                segment.name = "seg" + i;
                segment.transform.SetParent(root, false);
                segment.transform.localPosition =
                    new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                segment.transform.localScale = new Vector3(radius * 0.42f, 0.02f, 0.035f);
                segment.transform.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);

                SafeObjects.Destroy(segment.GetComponent<Collider>());

                segment.GetComponent<Renderer>().sharedMaterial = material;
            }

            return root;
        }

        /// <summary>Axial a mundo, solo para dibujar.</summary>
        private Vector3 ToWorld(HexCoordinates cell)
        {
            return new Vector3(1.5f * cell.Q * tileSpacing, hoverHeight, 1.7320508f * (cell.R + cell.Q * 0.5f) * tileSpacing);
        }
    }
}
