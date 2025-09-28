using UnityEngine;

namespace NekoFlow.Scriptable
{
    /// <summary>
    /// Stateless SO condition checked for transitions.
    /// </summary>
    public abstract class ScriptableCondition<T> : ScriptableObject where T : Component
    {
        public abstract bool IsMet(T ctx, float timeInState);
    }
}
