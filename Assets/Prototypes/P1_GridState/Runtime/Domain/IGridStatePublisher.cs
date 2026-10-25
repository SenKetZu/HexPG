using System;

namespace VoidHex.P1.Domain
{
    /// <summary>
    /// Contrato de NOTIFICACIÓN REACTIVA. Lo consume únicamente la presentación.
    /// Intencionalmente expone solo un evento: quien lo recibe no puede mutar el mapa (ISP).
    /// </summary>
    public interface IGridStatePublisher
    {
        event Action<GridChange> Changed;
    }
}
