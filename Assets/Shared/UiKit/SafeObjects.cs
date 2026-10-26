using UnityEngine;

namespace VoidHex.UiKit
{
    /// <summary>
    /// Destrucción de objetos de Unity con el método válido para el contexto actual.
    ///
    /// <c>Object.Destroy</c> solo está permitido en Play Mode: en el editor deja el error
    /// "Destroy may not be called from edit mode" y además el objeto sigue existiendo, así que el
    /// efecto buscado tampoco ocurre. Este código corre en los dos contextos —las escenas y la demo
    /// se pueden construir y poblar desde el editor, sin entrar en play mode— por eso la elección
    /// tiene que ser en tiempo de ejecución y no en tiempo de compilación.
    /// </summary>
    public static class SafeObjects
    {
        public static void Destroy(Object target)
        {
            if (target == null) return;

            // En Play Mode se usa Destroy: es diferido, respeta el orden de actualización y es la
            // forma correcta de hacerlo desde un MonoBehaviour. En el editor no hay frame que
            // consuma esa cola, así que hace falta DestroyImmediate.
            if (Application.isPlaying) Object.Destroy(target);
            else Object.DestroyImmediate(target);
        }
    }
}
