using System.Threading.Tasks;

namespace VoidHex.P2.Domain
{
    /// <summary>
    /// Acción inmutable del juego. Declara su costo, valida contra el estado y ejecuta la parte
    /// lógica de forma instantánea en el Core. No conoce Unity, ni animaciones, ni frames.
    /// Agregar una habilidad nueva es crear otra implementación (OCP).
    /// </summary>
    public interface ICommand
    {
        int ActionPointCost { get; }

        /// <summary>Clave de la animación que la presentación debe reproducir. Dato, no comportamiento.</summary>
        string VisualKey { get; }

        CommandValidation Validate(ShipModel model);

        /// <summary>
        /// Aplica el efecto lógico. Solo debe invocarse cuando <see cref="Validate"/> devolvió Allowed
        /// (precondición del pipeline).
        /// </summary>
        ShipModel Execute(ShipModel model);
    }
}
