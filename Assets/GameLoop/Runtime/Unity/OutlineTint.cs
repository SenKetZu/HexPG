using UnityEngine;

namespace VoidHex.GameLoop.Unity
{
    /// <summary>
    /// Tiñe los segmentos de un contorno hexagonal en un solo paso.
    ///
    /// Existe para que el contorno de hover pueda cambiar entre "alcanzable" y "bloqueado" sin que
    /// quien lo pide conozca su estructura interna (cuántos segmentos tiene, cómo se llaman).
    /// </summary>
    public sealed class OutlineTint : MonoBehaviour
    {
        private MeshRenderer[] _renderers;
        private MaterialPropertyBlock _block;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        public void Apply(Color color)
        {
            if (_renderers == null) _renderers = GetComponentsInChildren<MeshRenderer>(true);
            _block ??= new MaterialPropertyBlock();

            foreach (var renderer in _renderers)
            {
                if (renderer == null) continue;

                _block.Clear();
                _block.SetColor(BaseColorId, color);
                _block.SetColor(ColorId, color);
                _block.SetColor(EmissionId, color * 1.4f);
                renderer.SetPropertyBlock(_block);
            }
        }
    }
}
