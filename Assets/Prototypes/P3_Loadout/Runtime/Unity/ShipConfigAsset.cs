using UnityEngine;
using VoidHex.P3.Application;
using VoidHex.P3.Domain;

namespace VoidHex.P3.Unity
{
    /// <summary>Asset de estadísticas base. Solo datos; el cálculo vive en el dominio.</summary>
    [CreateAssetMenu(menuName = "VoidHex/ShipConfig", fileName = "ShipConfig")]
    public sealed class ShipConfigAsset : ScriptableObject
    {
        [SerializeField] private int baseActionPoints = 6;
        [SerializeField] private int baseShields;
        [SerializeField] private int baseMovement = 3;
        [SerializeField] private int baseHull = 10;

        public void Initialize(int actionPoints, int shields, int movement, int hull)
        {
            baseActionPoints = actionPoints;
            baseShields = shields;
            baseMovement = movement;
            baseHull = hull;
        }

        public ShipBaseStats ToBaseStats()
            => new ShipBaseStats(baseActionPoints, baseShields, baseMovement, baseHull);

        public ShipConfig ToConfig() => new ShipConfig(ToBaseStats());
    }
}
