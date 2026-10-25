namespace VoidHex.P2.Domain
{
    /// <summary>Par antes/después que se entrega a la presentación para animar el cambio.</summary>
    public readonly struct CommandExecution
    {
        public ICommand Command { get; }
        public ShipModel Before { get; }
        public ShipModel After { get; }

        public CommandExecution(ICommand command, ShipModel before, ShipModel after)
        {
            Command = command;
            Before = before;
            After = after;
        }
    }
}
