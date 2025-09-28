namespace NekoFlow.FSM
{
    public interface IState
    {
        void OnEnter();
        void OnTick();
        void OnFixedTick();
        void OnLateTick();
        void OnExit();
    }
}