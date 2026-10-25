using System.Threading.Tasks;
using UnityEngine;
using VoidHex.P2.Domain;

namespace VoidHex.P2.Unity
{
    /// <summary>
    /// Adaptador de presentación: traduce un <see cref="CommandExecution"/> en una animación.
    /// La tarea se completa desde el ciclo de vida de Unity, nunca desde el dominio.
    /// </summary>
    public sealed class ShipView : MonoBehaviour, ICommandPresentation
    {
        [SerializeField] private int animationFrames = 2;

        private TaskCompletionSource<bool> _completion;
        private int _framesRemaining;

        public int CompletedAnimations { get; private set; }

        public Task PlayAsync(CommandExecution execution)
        {
            Debug.Log($"[P2][View] Animación '{execution.Command.VisualKey}' " +
                      $"{execution.Before.Position} -> {execution.After.Position}");

            _completion = new TaskCompletionSource<bool>();
            _framesRemaining = Mathf.Max(1, animationFrames);
            return _completion.Task;
        }

        private void Update()
        {
            if (_completion == null) return;

            _framesRemaining--;
            if (_framesRemaining > 0) return;

            var completion = _completion;
            _completion = null;
            CompletedAnimations++;
            completion.SetResult(true);
        }
    }
}
