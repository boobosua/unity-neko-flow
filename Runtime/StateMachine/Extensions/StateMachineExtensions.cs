using System;

namespace NekoFlow.FSM
{
    public static class StateMachineExtensions
    {
        /// <summary>
        /// Set the initial state of the state machine.
        /// </summary>
        public static StateBehaviour StartWith(this StateBehaviour smc, IState state)
        {
            var sm = smc.GetStateMachine();
            sm.SetState(state);
            return smc;
        }

        /// <summary>
        /// Set the initial state on a pure StateMachine (no component).
        /// </summary>
        public static StateMachine StartWith(this StateMachine sm, IState state)
        {
            sm.SetState(state);
            return sm;
        }

        /// <summary>
        /// Add a transition from one state to another with a condition.
        /// </summary>
        public static StateBehaviour At(this StateBehaviour smc, IState from, IState to, Func<bool> condition)
        {
            var sm = smc.GetStateMachine();
            sm.AddTransition(from, to, condition);
            return smc;
        }

        /// <summary>
        /// Add a transition on a pure StateMachine.
        /// </summary>
        public static StateMachine At(this StateMachine sm, IState from, IState to, Func<bool> condition)
        {
            sm.AddTransition(from, to, condition);
            return sm;
        }

        /// <summary>
        /// Add a transition that can occur from any state to the specified state with a condition.
        /// </summary>
        public static StateBehaviour Any(this StateBehaviour smc, IState to, Func<bool> condition)
        {
            var sm = smc.GetStateMachine();
            sm.AddAnyTransition(to, condition);
            return smc;
        }

        /// <summary>
        /// Add an Any-state transition on a pure StateMachine.
        /// </summary>
        public static StateMachine Any(this StateMachine sm, IState to, Func<bool> condition)
        {
            sm.AddAnyTransition(to, condition);
            return sm;
        }

        /// <summary>
        /// Check if the current state is of type T.
        /// </summary>
        public static bool Is<T>(this StateMachine stateMachine) where T : IState
        {
            return stateMachine.CurrentState is T;
        }

        /// <summary>
        /// Get the current state cast to type T, or null if the cast fails.
        /// </summary>
        public static T Get<T>(this StateMachine stateMachine) where T : class, IState
        {
            return stateMachine.CurrentState as T;
        }
    }
}
