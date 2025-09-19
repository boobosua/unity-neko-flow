using UnityEngine;

namespace NekoFlow
{
    [DisallowMultipleComponent]
    public abstract class FlowBehaviour : MonoBehaviour
    {
        [Header("Flow Settings")]
        [SerializeField] private bool _enableFixedTick = false;
        [SerializeField] private bool _enableLateTick = false;
        private readonly FlowMachine _mainFlow = new();

        protected virtual void Update()
        {
            _mainFlow?.Tick();
        }

        protected virtual void FixedUpdate()
        {
            if (_enableFixedTick)
                _mainFlow?.FixedTick();
        }

        protected virtual void LateUpdate()
        {
            if (_enableLateTick)
                _mainFlow?.LateTick();
        }

        public bool IsInState<T>() where T : IState
        {
            return _mainFlow?.Is<T>() ?? false;
        }

        public T GetCurrentState<T>() where T : class, IState
        {
            return _mainFlow?.Get<T>();
        }

        public IState GetCurrentState()
        {
            return _mainFlow?.CurrentState;
        }

        public FlowMachine GetFlowMachine()
        {
            return _mainFlow;
        }

        protected FlowMachine MainFlow => _mainFlow;
    }
}
