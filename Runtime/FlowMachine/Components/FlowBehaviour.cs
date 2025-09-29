using UnityEngine;

namespace NekoFlow.FSM
{
    [DisallowMultipleComponent]
    public abstract class FlowBehaviour : MonoBehaviour
    {
        private readonly FlowMachine _mainFlow = new();

        protected virtual void Update()
        {
            _mainFlow?.Tick();
        }

        protected virtual void FixedUpdate()
        {
            _mainFlow?.FixedTick();
        }

        protected virtual void LateUpdate()
        {
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
