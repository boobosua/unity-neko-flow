using System;

namespace NekoFlow
{
    public static class StateMachineExtensions
    {
        public static StateMachine Begin(this StateMachine stateMachine, IState state)
        {
            stateMachine.SetState(state);
            return stateMachine;
        }

        public static StateMachine At(this StateMachine stateMachine, IState from, IState to, Func<bool> condition)
        {
            stateMachine.AddTransition(from, to, condition);
            return stateMachine;
        }

        public static StateMachine Any(this StateMachine stateMachine, IState to, Func<bool> condition)
        {
            stateMachine.AddAnyTransition(to, condition);
            return stateMachine;
        }

        public static bool Is<T>(this StateMachine stateMachine) where T : IState
        {
            return stateMachine.CurrentState is T;
        }

        public static T Get<T>(this StateMachine stateMachine) where T : class, IState
        {
            return stateMachine.CurrentState as T;
        }
    }
}
