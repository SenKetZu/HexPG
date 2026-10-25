using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using VoidHex.P2.Application;
using VoidHex.P2.Domain;

namespace VoidHex.P2.Tests
{
    /// <summary>
    /// Tests del pipeline de comandos. Verifican la propiedad central del diseño:
    /// la lógica se valida y aplica de forma instantánea, y la animación ocurre después,
    /// sin condicionar la regla ni permitir que dos comandos se intercalen.
    /// </summary>
    public sealed class CommandPipelineTests
    {
        [Test]
        public async Task Submit_RejectedCommand_KeepsStateAndDoesNotAnimate()
        {
            var presentation = new ImmediatePresentation();
            var processor = new CommandProcessor(ShipModel.Start().WithActionPoints(1), presentation);

            var outcome = await processor.SubmitAsync(new MoveCommand(new HexCoordinates(3, 0), 2));

            Assert.That(outcome.IsAccepted, Is.False);
            Assert.That(outcome.Validation.Rejection, Is.EqualTo(CommandRejection.NotEnoughActionPoints));
            Assert.That(processor.State.ActionPoints, Is.EqualTo(1));
            Assert.That(processor.State.Position, Is.EqualTo(HexCoordinates.Origin));
            Assert.That(presentation.Calls, Is.Zero, "Un comando rechazado no debe pedir animación.");
        }

        [Test]
        public async Task Submit_AppliesLogicBeforeTheAnimationCompletes()
        {
            var presentation = new ControlledPresentation();
            var processor = new CommandProcessor(ShipModel.Start(), presentation);

            var pending = processor.SubmitAsync(new MoveCommand(new HexCoordinates(2, 0), 2));

            Assert.That(presentation.Calls, Is.EqualTo(1));
            Assert.That(processor.State.Position, Is.EqualTo(new HexCoordinates(2, 0)));
            Assert.That(processor.State.ActionPoints, Is.EqualTo(4));
            Assert.That(processor.Phase, Is.EqualTo(TurnPhase.Executing));
            Assert.That(pending.IsCompleted, Is.False, "Sin animación terminada, el comando sigue en curso.");

            presentation.CompleteNext();
            await pending;

            Assert.That(processor.Phase, Is.EqualTo(TurnPhase.PlayerTurn));
        }

        [Test]
        public async Task Submit_SecondCommand_QueuesUntilTheFirstAnimationCompletes()
        {
            var presentation = new ControlledPresentation();
            var processor = new CommandProcessor(ShipModel.Start(), presentation);

            var first = processor.SubmitAsync(new MoveCommand(new HexCoordinates(1, 0), 2));
            var second = processor.SubmitAsync(new MoveCommand(new HexCoordinates(2, 0), 2));

            Assert.That(presentation.Calls, Is.EqualTo(1), "El segundo comando no puede entrar durante la animación.");
            Assert.That(processor.State.Position, Is.EqualTo(new HexCoordinates(1, 0)));
            Assert.That(processor.State.ActionPoints, Is.EqualTo(4));

            presentation.CompleteNext();
            await first;

            var frames = 0;
            while (presentation.Calls < 2 && frames++ < 1000)
            {
                await Task.Yield();
            }

            Assert.That(presentation.Calls, Is.EqualTo(2), "La cola debe liberar el segundo comando.");
            presentation.CompleteNext();
            await second;

            Assert.That(processor.State.Position, Is.EqualTo(new HexCoordinates(2, 0)));
        }

        [Test]
        public async Task Submit_InterleavedCommands_SpendsExactActionPoints()
        {
            var processor = new CommandProcessor(ShipModel.Start(), new ImmediatePresentation());

            await processor.SubmitAsync(new MoveCommand(new HexCoordinates(2, 0), 2));
            await processor.SubmitAsync(new AttackCommand(actionPointCost: 2, damage: 3, range: 3));
            await processor.SubmitAsync(new MoveCommand(new HexCoordinates(3, 0), 1));

            Assert.That(processor.State.ActionPoints, Is.EqualTo(1));
            Assert.That(processor.State.EnemyHull, Is.EqualTo(3));
            Assert.That(processor.State.Position, Is.EqualTo(new HexCoordinates(3, 0)));
        }

        [Test]
        public async Task Submit_NewCommandVariant_RequiresNoPipelineChange()
        {
            var processor = new CommandProcessor(ShipModel.Start(), new ImmediatePresentation());

            var outcome = await processor.SubmitAsync(new ShieldCommand(actionPointCost: 2, shieldAmount: 4));

            Assert.That(outcome.IsAccepted, Is.True);
            Assert.That(processor.State.Shields, Is.EqualTo(4));
            Assert.That(processor.State.ActionPoints, Is.EqualTo(4));
        }

        [Test]
        public void EndTurn_RefillsActionPointsToMaximum()
        {
            var processor = new CommandProcessor(
                ShipModel.Start().WithActionPoints(1),
                new ImmediatePresentation());

            processor.EndTurn();

            Assert.That(processor.State.ActionPoints, Is.EqualTo(processor.State.MaxActionPoints));
        }

        [Test]
        public async Task Submit_DuringEnemyTurn_IsRejectedWithWrongPhase()
        {
            var presentation = new ImmediatePresentation();
            var processor = new CommandProcessor(ShipModel.Start(), presentation);
            processor.BeginEnemyTurn();

            var outcome = await processor.SubmitAsync(new MoveCommand(new HexCoordinates(1, 0), 1));

            Assert.That(outcome.IsAccepted, Is.False);
            Assert.That(outcome.Validation.Rejection, Is.EqualTo(CommandRejection.WrongPhase));
            Assert.That(presentation.Calls, Is.Zero);
        }

        private sealed class ImmediatePresentation : ICommandPresentation
        {
            public int Calls { get; private set; }

            public Task PlayAsync(CommandExecution execution)
            {
                Calls++;
                return Task.CompletedTask;
            }
        }

        private sealed class ControlledPresentation : ICommandPresentation
        {
            private readonly Queue<TaskCompletionSource<bool>> _pending = new Queue<TaskCompletionSource<bool>>();

            public int Calls { get; private set; }

            public Task PlayAsync(CommandExecution execution)
            {
                Calls++;
                var completion = new TaskCompletionSource<bool>();
                _pending.Enqueue(completion);
                return completion.Task;
            }

            public void CompleteNext() => _pending.Dequeue().SetResult(true);
        }
    }
}
