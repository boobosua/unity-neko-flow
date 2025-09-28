using System;
using System.Collections.Generic;

namespace NekoFlow.FSM
{
    public partial class FlowMachine
    {
        private IState _currentState;
        public IState CurrentState => _currentState;

        private readonly Dictionary<Type, List<Transition>> _transitions = new();
        private readonly List<Transition> _anyTransitions = new();

        /// <summary>
        /// Update the state machine, checking for transitions and calling OnTick on the current state.
        /// </summary>
        public void Tick()
        {
            var transition = GetTransition();
            if (transition != null)
                SetState(transition.To);

            _currentState?.OnTick();
        }

        /// <summary>
        /// Update the state machine for fixed updates, calling OnFixedTick on the current state.
        /// </summary>
        public void FixedTick()
        {
            _currentState?.OnFixedTick();
        }

        /// <summary>
        /// Update the state machine for late updates, calling OnLateTick on the current state.
        /// </summary>
        public void LateTick()
        {
            _currentState?.OnLateTick();
        }

        /// <summary>
        /// Immediately set the current state, calling OnExit on the old state and OnEnter on the new state.
        /// </summary>
        public void SetState(IState state)
        {
            if (state == _currentState) return;
            _currentState?.OnExit();
            _currentState = state;
            _currentState?.OnEnter();
        }

        /// <summary>
        /// Add a transition from one state to another with a condition.
        /// </summary>
        public void AddTransition(IState from, IState to, Func<bool> predicate)
        {
            if (_transitions.TryGetValue(from.GetType(), out var transitions) == false)
            {
                transitions = new List<Transition>();
                _transitions[from.GetType()] = transitions;
            }

            transitions.Add(new Transition(to, predicate));
        }

        /// <summary>
        /// Add a transition that can occur from any state to the specified state with a condition.
        /// </summary>
        public void AddAnyTransition(IState state, Func<bool> predicate)
        {
            _anyTransitions.Add(new Transition(state, predicate));
        }

        /// <summary>
        /// Get potential transitions from the current state for debugging.
        /// </summary>
        public List<IState> GetPotentialTransitions()
        {
            var potentialStates = new List<IState>();

            // Add any-state transitions
            for (int i = 0; i < _anyTransitions.Count; i++)
            {
                potentialStates.Add(_anyTransitions[i].To);
            }

            // Add current state specific transitions
            if (_currentState != null && _transitions.TryGetValue(_currentState.GetType(), out var currentTransitions))
            {
                for (int i = 0; i < currentTransitions.Count; i++)
                {
                    potentialStates.Add(currentTransitions[i].To);
                }
            }

            return potentialStates;
        }

        /// <summary>
        /// Get the transition to the next state based on the current state and any active conditions.
        /// </summary>
        private Transition GetTransition()
        {
            for (int i = 0; i < _anyTransitions.Count; i++)
            {
                if (_anyTransitions[i].Condition())
                {
                    return _anyTransitions[i];
                }
            }

            if (_currentState != null && _transitions.TryGetValue(_currentState.GetType(), out var currentTransitions))
            {
                for (int i = 0; i < currentTransitions.Count; i++)
                {
                    if (currentTransitions[i].Condition())
                    {
                        return currentTransitions[i];
                    }
                }
            }

            return null;
        }
    }
}