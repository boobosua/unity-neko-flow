using UnityEngine;

namespace NekoFlow.Scriptable
{
    /// <summary>
    /// SO that owns actions and transitions for one logical state (no runtime caches here).
    /// </summary>
    public abstract class ScriptableState<T> : ScriptableObject where T : Component
    {
        [SerializeField] private ScriptableAction<T>[] _actions = new ScriptableAction<T>[0];
        [SerializeField] private ScriptableTransition<T>[] _transitions = new ScriptableTransition<T>[0];

        public bool HasActions => _actions != null && _actions.Length > 0;
        public bool HasTransitions => _transitions != null && _transitions.Length > 0;

        public virtual void OnEnter(T ctx)
        {
            var arr = _actions;
            for (int i = 0, n = arr.Length; i < n; i++)
            {
                if (arr[i] != null) arr[i].OnEnter(ctx);
            }
        }

        public virtual void OnUpdate(T ctx)
        {
            var arr = _actions;
            for (int i = 0, n = arr.Length; i < n; i++)
            {
                if (arr[i] != null) arr[i].OnUpdate(ctx);
            }
        }

        public virtual void OnFixedUpdate(T ctx)
        {
            var arr = _actions;
            for (int i = 0, n = arr.Length; i < n; i++)
            {
                if (arr[i] != null) arr[i].OnFixedUpdate(ctx);
            }
        }

        public virtual void OnLateUpdate(T ctx)
        {
            var arr = _actions;
            for (int i = 0, n = arr.Length; i < n; i++)
            {
                if (arr[i] != null) arr[i].OnLateUpdate(ctx);
            }
        }

        public virtual void OnExit(T ctx)
        {
            var arr = _actions;
            for (int i = 0, n = arr.Length; i < n; i++)
            {
                if (arr[i] != null) arr[i].OnExit(ctx);
            }
        }

        /// Evaluate transitions using runner-provided timeInState.
        public ScriptableState<T> CheckTransitions(T ctx, float timeInState)
        {
            var arr = _transitions;
            for (int i = 0, n = arr.Length; i < n; i++)
            {
                var cond = arr[i].Condition;
                if (cond != null && cond.IsMet(ctx, timeInState))
                    return arr[i].TargetState;
            }
            return null;
        }
    }
}
