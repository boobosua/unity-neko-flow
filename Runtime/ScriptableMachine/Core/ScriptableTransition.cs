using System;
using UnityEngine;

namespace NekoFlow.Scriptable
{
    [Serializable]
    public struct ScriptableTransition<T> where T : Component
    {
        [Tooltip("Condition to evaluate for this transition.")]
        public ScriptableCondition<T> Condition;

        [Tooltip("Target state if the condition is met.")]
        public ScriptableState<T> TargetState;
    }
}
