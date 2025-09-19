using System;

namespace NekoFlow
{
    public static class FlowMachineExtensions
    {
        /// <summary>
        /// Set the initial state of the state machine.
        /// </summary>
        public static FlowMachine StartWith(this FlowMachine stateMachine, IState state)
        {
            stateMachine.SetState(state);
            return stateMachine;
        }

        /// <summary>
        /// Add a transition from one state to another with a condition.
        /// </summary>
        public static FlowMachine At(this FlowMachine stateMachine, IState from, IState to, Func<bool> condition)
        {
            stateMachine.AddTransition(from, to, condition);
            return stateMachine;
        }

        /// <summary>
        /// Add a transition that can occur from any state to the specified state with a condition.
        /// </summary>
        public static FlowMachine Any(this FlowMachine stateMachine, IState to, Func<bool> condition)
        {
            stateMachine.AddAnyTransition(to, condition);
            return stateMachine;
        }

        /// <summary>
        /// Check if the current state is of type T.
        /// </summary>
        public static bool Is<T>(this FlowMachine stateMachine) where T : IState
        {
            return stateMachine.CurrentState is T;
        }

        /// <summary>
        /// Get the current state cast to type T, or null if the cast fails.
        /// </summary>
        public static T Get<T>(this FlowMachine stateMachine) where T : class, IState
        {
            return stateMachine.CurrentState as T;
        }
    }
}
