namespace NekoFlow
{
    public interface IState
    {
        void OnEnter();
        void OnTick();
        void OnFixedTick();
        void OnExit();
    }
}
