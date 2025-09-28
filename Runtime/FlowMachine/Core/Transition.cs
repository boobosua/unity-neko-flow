using System;

namespace NekoFlow.FSM
{
    public partial class FlowMachine
    {
        /// <summary>
        /// Represents a transition from one state to another with a condition.
        /// </summary>
        private class Transition
        {
            public Func<bool> Condition { get; }
            public IState To { get; }

            public Transition(IState to, Func<bool> condition)
            {
                To = to ?? throw new ArgumentNullException(nameof(to));
                Condition = condition ?? throw new ArgumentNullException(nameof(condition));
            }
        }
    }
}