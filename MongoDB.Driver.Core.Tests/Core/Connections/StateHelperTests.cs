using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Connections
{
    [TestFixture]
    public class StateHelperTests
    {
        [Test]
        public void Current_should_return_initial_state_after_construction()
        {
            var state = new StateHelper(3);

            Assert.AreEqual(3, state.Current);
        }

        [Test]
        public void Current_should_return_new_state_after_a_successful_change()
        {
            var state = new StateHelper(3);
            state.TryChange(5);

            Assert.AreEqual(5, state.Current);
        }

        [Test]
        public void Current_should_return_current_state_after_a_successful_change()
        {
            var state = new StateHelper(3);
            state.TryChange(4, 5);

            Assert.AreEqual(3, state.Current);
        }

        [Test]
        [TestCase(0, 0, false)]
        [TestCase(0, 1, true)]
        [TestCase(1, 0, true)]
        [TestCase(1, 1, false)]
        public void TryChange_with_one_parameter(int currentState, int newState, bool expectedResult)
        {
            var state = new StateHelper(currentState);
            var result = state.TryChange(newState);

            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        [TestCase(0, 0, 1, true)]
        [TestCase(0, 1, 1, false)]
        [TestCase(0, 1, 2, false)]
        [TestCase(0, 1, 0, false)]
        public void TryChange_with_two_parameters(int currentState, int fromState, int toState, bool expectedResult)
        {
            var state = new StateHelper(currentState);
            var result = state.TryChange(fromState, toState);

            Assert.AreEqual(expectedResult, result);
        }
    }
}