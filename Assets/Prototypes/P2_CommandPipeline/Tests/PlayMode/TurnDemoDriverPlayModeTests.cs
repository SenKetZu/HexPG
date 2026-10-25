using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoidHex.P2.Application;
using VoidHex.P2.Unity;

namespace VoidHex.P2.Tests.PlayMode
{
    /// <summary>
    /// Único test de Play Mode del TP: verifica que Unity invoque al adaptador y que las
    /// animaciones se completen dentro del ciclo de vida real, algo que un test de Edit Mode
    /// no puede demostrar.
    /// </summary>
    public sealed class TurnDemoDriverPlayModeTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }
        }

        [UnityTest]
        public IEnumerator Driver_OnStart_SubmitsCommandsAndCompletesAnimations()
        {
            _root = new GameObject("P2_Driver");
            var driver = _root.AddComponent<TurnDemoDriver>();
            var outcomes = new List<CommandOutcome>();
            driver.CommandResolved += outcomes.Add;

            yield return null;

            var frames = 0;
            while (!driver.HasCompletedDemo && frames++ < 600)
            {
                yield return null;
            }

            Assert.That(driver.HasCompletedDemo, Is.True, "El driver debe terminar la secuencia.");
            Assert.That(outcomes, Has.Count.EqualTo(3));
            Assert.That(outcomes.TrueForAll(outcome => outcome.IsAccepted), Is.True);
            Assert.That(driver.State.ActionPoints, Is.EqualTo(1));
            Assert.That(driver.Phase, Is.EqualTo(TurnPhase.PlayerTurn));
        }
    }
}
