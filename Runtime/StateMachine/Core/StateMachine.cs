using System;
using System.Collections.Generic;

namespace NekoFlow.FSM
{
    public sealed partial class StateMachine
    {
        private const int DefaultTransitionCapacity = 8;

        private IState _currentState;
        public IState CurrentState => _currentState;

        private readonly Dictionary<Type, List<Transition>> _transitions = new(DefaultTransitionCapacity);
        private readonly List<Transition> _anyTransitions = new(DefaultTransitionCapacity);
        private List<Transition> _currentTransitions;

        private float _timeInState;
        public float TimeInState => _timeInState;

        /// <summary>
        /// Update the state machine with an explicit delta time, accumulating time spent in the current state.
        /// Ordering per frame:
        ///   1. Accumulate <paramref name="deltaTime"/> into <see cref="TimeInState"/> so transition
        ///      conditions always see an up-to-date value.
        ///   2. Evaluate transitions. If one fires, call <see cref="SetState"/> and return — the new
        ///      state's first <see cref="IState.OnTick"/> will run next frame.
        ///   3. Otherwise call <see cref="IState.OnTick"/> on the current state.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_currentState != null)
                _timeInState += deltaTime;

            var transition = GetTransition();
            if (transition != null)
            {
                SetState(transition.To);
                return;
            }

            _currentState?.OnTick(deltaTime);
        }

        /// <summary>
        /// Immediately set the current state, calling OnExit on the old state and OnEnter on the new state.
        /// </summary>
        public void SetState(IState state)
        {
            if (state == _currentState) return;

            _currentState?.OnExit();

            _currentState = state;
            _timeInState = 0f;

            _currentTransitions = null;
            if (_currentState != null)
                _transitions.TryGetValue(_currentState.GetType(), out _currentTransitions);

            _currentState?.OnEnter();
        }

        /// <summary>
        /// Add a transition from one state to another with a condition.
        /// Transitions are keyed by the runtime type of <paramref name="from"/>. Two different
        /// instances of the same class share the same transition list, so each state type should
        /// have at most one instance registered per machine.
        /// </summary>
        public void AddTransition(IState from, IState to, Func<bool> predicate)
        {
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            if (_transitions.TryGetValue(from.GetType(), out var transitions) == false)
            {
                transitions = new List<Transition>(DefaultTransitionCapacity);
                _transitions[from.GetType()] = transitions;

                if (_currentState != null && _currentState.GetType() == from.GetType())
                    _currentTransitions = transitions;
            }

            transitions.Add(new Transition(to, predicate));
        }

        /// <summary>
        /// Add a transition that can occur from any state to the specified state with a condition.
        /// </summary>
        public void AddAnyTransition(IState state, Func<bool> predicate)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            _anyTransitions.Add(new Transition(state, predicate));
        }

        /// <summary>
        /// Get the transition to the next state based on the current state and any active conditions.
        /// Any-state transitions are always evaluated before state-specific transitions;
        /// the first matching transition wins.
        /// </summary>
        private Transition GetTransition()
        {
            for (int i = 0; i < _anyTransitions.Count; i++)
            {
                var t = _anyTransitions[i];
                if (t.Condition())
                    return t;
            }

            var list = _currentTransitions;
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var t = list[i];
                    if (t.Condition())
                        return t;
                }
            }

            return null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Fill a provided buffer with potential transitions from the current state.
        /// This avoids per-call allocations. The buffer is cleared before filling.
        /// </summary>
        public void GetPotentialTransitionsNonAlloc(List<IState> buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            buffer.Clear();

            for (int i = 0; i < _anyTransitions.Count; i++)
            {
                buffer.Add(_anyTransitions[i].To);
            }

            if (_currentState != null && _transitions.TryGetValue(_currentState.GetType(), out var currentTransitions))
            {
                for (int i = 0; i < currentTransitions.Count; i++)
                {
                    buffer.Add(currentTransitions[i].To);
                }
            }
        }
#endif
    }
}