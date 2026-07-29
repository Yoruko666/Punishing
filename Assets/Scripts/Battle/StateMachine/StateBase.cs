public abstract class StateBase
{
    public PlayerController Owner;
    public StateBase(PlayerController owner)
    {
        Owner = owner;
    }
    public abstract void OnEnter();
    public abstract void OnUpdate();
    public abstract void OnExit();
}
