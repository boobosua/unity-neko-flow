using System;

namespace NekoFlow
{
    public partial class StateMachine
    {
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