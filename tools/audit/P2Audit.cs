using System.Collections.Generic;
using System.Threading.Tasks;
using VoidHex.P2.Application;
using VoidHex.P2.Domain;

namespace VoidHex.Audit
{
    /// <summary>Verifica P2 contra las aserciones de CommandPipelineTests y la demo de la escena.</summary>
    internal static class P2Audit
    {
        internal static async Task RunAsync()
        {
            // Rechazo sin efectos ni animación.
            var counting = new CountingPresentation();
            var rejected = new CommandProcessor(ShipModel.Start().WithActionPoints(1), counting);
            var outcome = await rejected.SubmitAsync(new MoveCommand(new HexCoordinates(3, 0), 2));
            Program.Check("rechazo -> IsAccepted", outcome.IsAccepted, false);
            Program.Check("rechazo -> motivo", outcome.Validation.Rejection, CommandRejection.NotEnoughActionPoints);
            Program.Check("rechazo -> PA intactos", rejected.State.ActionPoints, 1);
            Program.Check("rechazo -> posición intacta", rejected.State.Position, HexCoordinates.Origin);
            Program.Check("rechazo -> sin animación", counting.Calls, 0);

            // Misma celda: motivo InvalidTarget, no NotEnoughActionPoints.
            var same = new CommandProcessor(ShipModel.Start(), counting);
            var sameOutcome = await same.SubmitAsync(new MoveCommand(HexCoordinates.Origin, 1));
            Program.Check("mover a la misma celda -> InvalidTarget",
                sameOutcome.Validation.Rejection, CommandRejection.InvalidTarget);

            // La lógica se aplica antes de que la animación termine.
            var controlled = new ControlledPresentation();
            var before = new CommandProcessor(ShipModel.Start(), controlled);
            var pending = before.SubmitAsync(new MoveCommand(new HexCoordinates(2, 0), 2));
            Program.Check("estado ya mutado", before.State.Position, new HexCoordinates(2, 0));
            Program.Check("PA ya descontados", before.State.ActionPoints, 4);
            Program.Check("fase durante la animación", before.Phase, TurnPhase.Executing);
            Program.Check("task pendiente", pending.IsCompleted, false);
            controlled.CompleteNext();
            await pending;
            Program.Check("fase al terminar", before.Phase, TurnPhase.PlayerTurn);

            // Cola: el segundo comando no entra durante la animación.
            var queued = new ControlledPresentation();
            var serial = new CommandProcessor(ShipModel.Start(), queued);
            var first = serial.SubmitAsync(new MoveCommand(new HexCoordinates(1, 0), 2));
            var second = serial.SubmitAsync(new MoveCommand(new HexCoordinates(2, 0), 2));
            Program.Check("segundo comando en cola (1 animación)", queued.Calls, 1);
            Program.Check("estado tras el primero", serial.State.Position, new HexCoordinates(1, 0));
            queued.CompleteNext();
            await first;

            var spins = 0;
            while (queued.Calls < 2 && spins++ < 20000) await Task.Yield();
            Program.Check("la cola libera el segundo", queued.Calls, 2);
            queued.CompleteNext();
            await second;
            Program.Check("posición final", serial.State.Position, new HexCoordinates(2, 0));

            // Secuencia intercalada (3 comandos, PA exactos).
            var interleaved = new CommandProcessor(ShipModel.Start(), new CountingPresentation());
            await interleaved.SubmitAsync(new MoveCommand(new HexCoordinates(2, 0), 2));
            await interleaved.SubmitAsync(new AttackCommand(actionPointCost: 2, damage: 3, range: 3));
            await interleaved.SubmitAsync(new MoveCommand(new HexCoordinates(3, 0), 1));
            Program.Check("PA final (el test afirma 1)", interleaved.State.ActionPoints, 1);
            Program.Check("hull enemigo (el test afirma 3)", interleaved.State.EnemyHull, 3);
            Program.Check("posición final (el test afirma (3,0))",
                interleaved.State.Position, new HexCoordinates(3, 0));

            // Ataque fuera de rango.
            var far = new CommandProcessor(ShipModel.Start(), new CountingPresentation());
            var farOutcome = await far.SubmitAsync(new AttackCommand(1, 3, 1));
            Program.Check("ataque fuera de rango -> OutOfRange",
                farOutcome.Validation.Rejection, CommandRejection.OutOfRange);

            // Variante nueva sin tocar el pipeline (OCP).
            var variant = new CommandProcessor(ShipModel.Start(), new CountingPresentation());
            var shield = await variant.SubmitAsync(new ShieldCommand(actionPointCost: 2, shieldAmount: 4));
            Program.Check("shield aceptado", shield.IsAccepted, true);
            Program.Check("escudos (el test afirma 4)", variant.State.Shields, 4);
            Program.Check("PA tras escudo (el test afirma 4)", variant.State.ActionPoints, 4);

            // Recarga de turno.
            var refill = new CommandProcessor(ShipModel.Start().WithActionPoints(1), new CountingPresentation());
            refill.EndTurn();
            Program.Check("EndTurn recarga al máximo", refill.State.ActionPoints, refill.State.MaxActionPoints);

            // Turno enemigo.
            var enemy = new CommandProcessor(ShipModel.Start(), new CountingPresentation());
            enemy.BeginEnemyTurn();
            var wrong = await enemy.SubmitAsync(new MoveCommand(new HexCoordinates(1, 0), 1));
            Program.Check("turno enemigo -> WrongPhase", wrong.Validation.Rejection, CommandRejection.WrongPhase);

            // Secuencia real de la escena (TurnDemoDriver.RunDemoAsync).
            var demo = new CommandProcessor(ShipModel.Start(), new CountingPresentation());
            await demo.SubmitAsync(new MoveCommand(new HexCoordinates(2, 0), 2));
            await demo.SubmitAsync(new AttackCommand(2, 3, 3));
            await demo.SubmitAsync(new MoveCommand(new HexCoordinates(3, 0), 1));
            Program.Check("demo PlayMode: PA final (el test afirma 1)", demo.State.ActionPoints, 1);
            Program.Check("demo PlayMode: fase final", demo.Phase, TurnPhase.PlayerTurn);
        }

        private sealed class CountingPresentation : ICommandPresentation
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

            public void CompleteNext()
            {
                if (_pending.Count == 0) return;
                _pending.Dequeue().SetResult(true);
            }
        }
    }
}
