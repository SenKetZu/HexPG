using VoidHex.P3.Domain;

namespace VoidHex.P3.Application
{
    /// <summary>
    /// Configuración de la nave que recibe el servicio. Es una clase para no depender de
    /// cómo cada contenedor trata los tipos de valor; expone las stats base del dominio.
    /// </summary>
    public sealed class ShipConfig
    {
        public ShipBaseStats BaseStats { get; }

        public ShipConfig(ShipBaseStats baseStats)
        {
            BaseStats = baseStats;
        }
    }
}
