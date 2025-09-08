using System;
using System.Collections.Generic;

namespace NekoFlow
{
    public partial class StateMachine
    {
        private IState _currentState;
        public IState CurrentState => _currentState;

        private readonly Dictionary<Type, List<Transition>> _transitions = new();
        private readonly List<Transition> _anyTransitions = new();

        public void Tick()
        {
            var transition = GetTransition();
            if (transition != null)
                SetState(transition.To);

            _currentState?.OnTick();
        }

        public void FixedTick()
        {
            _currentState?.OnFixedTick();
        }

        public void LateTick()
        {
            _currentState?.OnLateTick();
        }

        public void SetState(IState state)
        {
            if (state == _currentState)
                return;

            _currentState?.OnExit();
            _currentState = state;
            _currentState?.OnEnter();
        }

        public void AddTransition(IState from, IState to, Func<bool> predicate)
        {
            if (_transitions.TryGetValue(from.GetType(), out var transitions) == false)
            {
                transitions = new List<Transition>();
                _transitions[from.GetType()] = transitions;
            }

            transitions.Add(new Transition(to, predicate));
        }

        public void AddAnyTransition(IState state, Func<bool> predicate)
        {
            _anyTransitions.Add(new Transition(state, predicate));
        }

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