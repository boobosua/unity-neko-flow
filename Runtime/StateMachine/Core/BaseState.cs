using UnityEngine;

namespace NekoFlow.FSM
{
    public class BaseState<T> : IState where T : MonoBehaviour
    {
        protected readonly T _context;
        protected readonly GameObject _gameObject;
        protected readonly Transform _transform;

        /// <param name="context">
        /// The MonoBehaviour this state operates on. References to <c>gameObject</c> and
        /// <c>transform</c> are cached at construction time. If the component is destroyed while
        /// the state machine is still running, guard with <c>_context == null</c> before accessing
        /// any Unity objects.
        /// </param>
        public BaseState(T context)
        {
            _context = context;
            _gameObject = context.gameObject;
            _transform = context.transform;
        }

        public virtual void OnEnter() { }
        public virtual void OnTick(float deltaTime) { }
        public virtual void OnExit() { }
    }
}
