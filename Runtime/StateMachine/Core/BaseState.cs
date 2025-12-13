using UnityEngine;

namespace NekoFlow
{
    public class BaseState<T> : IState where T : MonoBehaviour
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
        public virtual void OnTick(float deltaTime) { }
        public virtual void OnExit() { }
    }
}
