using System.Threading.Tasks;

namespace VoidHex.P2.Domain
{
    /// <summary>
    /// Puerto de salida hacia la representación visual. El Core valida y ejecuta al instante;
    /// la animación ocurre después, de forma asíncrona, sin bloquear ni condicionar la regla.
    /// </summary>
    public interface ICommandPresentation
    {
        Task PlayAsync(CommandExecution execution);
    }
}
