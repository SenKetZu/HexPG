namespace VoidHex.P1.Domain
{
    /// <summary>
    /// Contrato de ESCRITURA. En el juego real lo implementa la capa de comandos; la presentación
    /// lo recibe para provocar mutaciones de demostración.
    ///
    /// Existe separado de <see cref="IGridQueryService"/> y de <see cref="IGridStatePublisher"/> por
    /// ISP: un consumidor que solo lee no puede escribir, y uno que solo escucha no puede alterar nada.
    /// </summary>
    public interface IGridCommands
    {
        /// <summary>Devuelve true solo si el estado cambió (y por lo tanto se notificó).</summary>
        bool SetCell(HexCell cell);

        /// <summary>Devuelve true solo si la ocupación cambió (y por lo tanto se notificó).</summary>
        bool SetOccupied(HexCoordinates coordinates, bool occupied);
    }
}
