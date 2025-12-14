namespace NekoFlow.FSM
{
    public interface IState
    {
        void OnEnter();
        void OnTick(float deltaTime);
        void OnExit();
    }
}