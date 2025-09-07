using System;
using System.Collections.Generic;

namespace NekoFlow
{
    public class StateMachine
    {
        private class Transition
        {
            public IState To { get; }
            public IPredicate Condition { get; }

            public Transition(IState to, IPredicate condition)
            {
                To = to ?? throw new ArgumentNullException(nameof(to));
                Condition = condition ?? throw new ArgumentNullException(nameof(condition));
            }
        }

        private IState _currentState;
        private readonly Dictionary<Type, List<Transition>> _transitions = new();
        private List<Transition> _currentTransitions = new();
        private readonly List<Transition> _anyTransitions = new();

        // Cached empty list to avoid allocations
        private static readonly List<Transition> EmptyTransitions = new(0);

        public IState CurrentState => _currentState;
        public bool IsRunning => _currentState != null;

        public void Tick()
        {
            var transition = GetTransition();
            if (transition != null)
                ChangeState(transition.To);

            _currentState?.OnTick();
        }

        public void FixedTick()
        {
            _currentState?.OnFixedTick();
        }

        public void Initialize(IState initialState)
        {
            if (initialState == null) return;
            if (_currentState != null) return; // Already initialized

            _currentState = initialState;
            UpdateCurrentTransitions();
            _currentState.OnEnter();
        }

        private void ChangeState(IState newState)
        {
            if (_currentState == newState) return;

            _currentState?.OnExit();
            _currentState = newState;
            UpdateCurrentTransitions();
            _currentState?.OnEnter();
        }

        private void UpdateCurrentTransitions()
        {
            if (_currentState != null && _transitions.TryGetValue(_currentState.GetType(), out var transitions))
                _currentTransitions = transitions;
            else
                _currentTransitions = EmptyTransitions;
        }

        public void AddTransition(IState from, IState to, Func<bool> condition) =>
            AddTransition(from, to, new FuncPredicate(condition));

        public void AddTransition(IState from, IState to, IPredicate condition)
        {
            if (from == null || to == null || condition == null) return;

            var fromType = from.GetType();
            if (!_transitions.TryGetValue(fromType, out var transitions))
            {
                transitions = new List<Transition>();
                _transitions[fromType] = transitions;
            }

            transitions.Add(new Transition(to, condition));
        }

        public void AddAnyTransition(IState to, Func<bool> condition) =>
            AddAnyTransition(to, new FuncPredicate(condition));

        public void AddAnyTransition(IState to, IPredicate condition)
        {
            if (to == null || condition == null) return;
            _anyTransitions.Add(new Transition(to, condition));
        }

        private Transition GetTransition()
        {
            // Any transitions have priority.
            foreach (var transition in _anyTransitions)
            {
                if (transition.Condition.Evaluate())
                    return transition;
            }

            // Current state transitions.
            foreach (var transition in _currentTransitions)
            {
                if (transition.Condition.Evaluate())
                    return transition;
            }

            return null;
        }

        public void RemoveTransitions(IState from)
        {
            if (from != null)
                _transitions.Remove(from.GetType());
        }

        public void RemoveAnyTransition(IState to)
        {
            if (to == null) return;
            for (int i = _anyTransitions.Count - 1; i >= 0; i--)
            {
                if (_anyTransitions[i].To == to)
                    _anyTransitions.RemoveAt(i);
            }
        }

        public void Stop()
        {
            if (_currentState == null) return;

            _currentState.OnExit();
            _currentState = null;
            _currentTransitions = EmptyTransitions;
        }

        public void Dispose()
        {
            Stop();
            _transitions.Clear();
            _anyTransitions.Clear();
        }
    }
}
