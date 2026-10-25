using VoidHex.P1.Domain;

namespace VoidHex.P1.Unity
{
    /// <summary>
    /// Lógica de presentación reactiva, independiente del motor.
    /// Cuenta redibujos para poder demostrar (en un test) que sin mutación real no hay trabajo:
    /// no existe ningún Update() que consulte el modelo.
    /// </summary>
    public sealed class GridPresenterLogic
    {
        public int RedrawCount { get; private set; }

        public GridChange LastChange { get; private set; }

        public void OnGridChanged(GridChange change)
        {
            RedrawCount++;
            LastChange = change;
        }
    }
}
