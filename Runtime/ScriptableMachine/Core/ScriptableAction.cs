using UnityEngine;

namespace NekoFlow.Scriptable
{
    /// <summary>
    /// Stateless SO behavior executed while a state is active.
    /// </summary>
    public abstract class ScriptableAction<T> : ScriptableObject where T : Component
    {
        public virtual void OnEnter(T ctx) { }
        public virtual void OnUpdate(T ctx) { }
        public virtual void OnFixedUpdate(T ctx) { }
        public virtual void OnLateUpdate(T ctx) { }
        public virtual void OnExit(T ctx) { }
    }
}
