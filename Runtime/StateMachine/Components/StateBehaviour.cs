using UnityEngine;

namespace NekoFlow.FSM
{
    [DisallowMultipleComponent]
    public abstract class StateBehaviour : MonoBehaviour
    {
        private readonly StateMachine _stateMachine = new();

        protected virtual void Update()
        {
            _stateMachine.Tick(Time.deltaTime);
        }

        /// <summary>
        /// Check if the state machine is currently in the specified state type.
        /// </summary>
        public bool IsInState<T>() where T : IState
        {
            return _stateMachine.Is<T>();
        }

        /// <summary>
        /// Try to get the current state as type T.
        /// </summary>
        public bool TryGetCurrentState<T>(out T currentState) where T : class, IState
        {
            currentState = _stateMachine.Get<T>();
            return currentState != null;
        }

        /// <summary>
        /// Get the time spent in the current state.
        /// </summary>
        public float GetTimeInCurrentState()
        {
            return _stateMachine.TimeInState;
        }

        internal IState GetCurrentState()
        {
            return _stateMachine.CurrentState;
        }

        internal StateMachine GetStateMachine()
        {
            return _stateMachine;
        }
    }
}
