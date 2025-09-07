using UnityEngine;

namespace NekoFlow
{
    public abstract class BaseState<T> : IState where T : MonoBehaviour
    {
        protected T _context;
        protected GameObject _gameObject;
        protected Transform _transform;

        public BaseState(T context)
        {
            _context = context;
            _gameObject = context.gameObject;
            _transform = context.transform;
        }

        public virtual void OnEnter() { }
        public virtual void OnTick() { }
        public virtual void OnFixedTick() { }
        public virtual void OnExit() { }
    }
}
