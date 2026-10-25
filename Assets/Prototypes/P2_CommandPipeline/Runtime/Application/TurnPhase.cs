namespace VoidHex.P2.Application
{
    /// <summary>
    /// Fase mínima del flujo. No hay un estado por combinación de acciones: eso es justamente
    /// la FSM monolítica que el TP1 descartó. Solo distingue quién puede actuar y si hay
    /// una animación en curso.
    /// </summary>
    public enum TurnPhase
    {
        PlayerTurn = 0,
        Executing = 1,
        EnemyTurn = 2,
    }
}
