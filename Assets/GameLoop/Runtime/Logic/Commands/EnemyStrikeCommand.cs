using System;
using VoidHex.P2.Domain;

namespace VoidHex.GameLoop.Logic
{
    /// <summary>
    /// Disparo de la torreta: no consume PA del jugador y solo daña la nave.
    ///
    /// El daño se aplica primero a los escudos y el excedente al casco. Esa regla vive acá, en el
    /// comando, y no en <c>ShipModel</c>: el modelo sigue siendo datos inmutables, y el bucle aporta
    /// su propia regla de combate sin modificar P2.
    /// </summary>
    public sealed class EnemyStrikeCommand : ICommand
    {
        private readonly int _damage;

        public int ActionPointCost => 0;

        public string VisualKey => "enemy-strike";

        public EnemyStrikeCommand(int damage)
        {
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));

            _damage = damage;
        }

        /// <summary>La torreta siempre puede disparar; el turno ya decidió que corresponde.</summary>
        public CommandValidation Validate(ShipModel model) => CommandValidation.Allowed();

        public ShipModel Execute(ShipModel model)
        {
            var absorbed = Math.Min(model.Shields, _damage);
            var toHull = _damage - absorbed;

            var shields = model.Shields - absorbed;
            var hull = Math.Max(0, model.Hull - toHull);

            // Se construye el estado nuevo con el constructor público: el bucle no necesita
            // agregarle setters a P2 para expresar su propia regla de daño.
            return new ShipModel(
                model.Position,
                model.ActionPoints,
                model.MaxActionPoints,
                shields,
                hull,
                model.EnemyPosition,
                model.EnemyHull);
        }
    }
}
