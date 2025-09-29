using System;
using UnityEngine;

namespace NekoFlow.Scriptable
{
    /// Attach to the agent GameObject alongside your controller (any Component of type T).
    [DisallowMultipleComponent]
    public abstract class ScriptableFlowRunner<T> : MonoBehaviour where T : Component
    {
        [SerializeField] private ScriptableState<T> _initialState;

        public ScriptableState<T> CurrentState { get; private set; }
        public T Context { get; private set; }
        public float TimeInCurrentState => Time.time - _enterTime;

        public event Action<ScriptableState<T>, ScriptableState<T>> OnStateChanged;

        private float _enterTime;
        private bool _isTransitioning;

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (!Context && TryGetComponent<T>(out var ctx))
            {
                Context = ctx;
            }
        }
#endif


        private void Awake()
        {
            if (TryGetComponent<T>(out var ctx))
            {
                Context = ctx;
            }
            else
            {
                Debug.LogError($"[{nameof(ScriptableFlowRunner<T>)}] No component of type {typeof(T).Name} found on GameObject '{gameObject.name}'. Disabling the flow runner.", this);
            }
        }

        private void Start()
        {
            if (_initialState != null && Context != null && CurrentState == null)
                TransitionTo(_initialState);
        }

        private void Update()
        {
            var cs = CurrentState; var ctx = Context;
            if (!cs || !ctx) return;

            if (cs.HasActions) cs.Update(ctx);

            if (cs.HasTransitions && !_isTransitioning)
            {
                float t = TimeInCurrentState; // compute once
                var next = cs.CheckTransitions(ctx, t);
                if (next != null) TransitionTo(next);
            }
        }

        private void FixedUpdate()
        {
            var cs = CurrentState;
            var ctx = Context;
            if (!cs || !ctx || !cs.HasActions) return;
            cs.FixedUpdate(ctx);
        }

        private void LateUpdate()
        {
            var cs = CurrentState;
            var ctx = Context;
            if (!cs || !ctx || !cs.HasActions) return;
            cs.LateUpdate(ctx);
        }

        public void ResetFlow(ScriptableState<T> startState = null)
        {
            if (!Context) return;
            if (CurrentState) CurrentState.Exit(Context);
            CurrentState = null;
            if (startState != null) _initialState = startState;
            if (_initialState != null) TransitionTo(_initialState);
        }

        public bool TryTransitionTo(ScriptableState<T> next)
        {
            if (!next || !Context || _isTransitioning) return false;
            TransitionTo(next);
            return true;
        }

        public void TransitionTo(ScriptableState<T> next)
        {
            if (!next || !Context) return;

            _isTransitioning = true;
            var prev = CurrentState;
            try
            {
                if (prev != null)
                    prev.Exit(Context);
                CurrentState = next;
                _enterTime = Time.time;
                CurrentState.Enter(Context);
                OnStateChanged?.Invoke(prev, CurrentState);
            }
            finally
            {
                _isTransitioning = false;
            }
        }
    }
}
