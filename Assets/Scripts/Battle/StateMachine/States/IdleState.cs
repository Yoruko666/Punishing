public class IdleState : StateBase
{
    public IdleState(PlayerController owner) : base(owner)
    {
    }

    public override void OnEnter()
    {
        // Idle 不锁定动作
        Owner.PlayAnim("Idle");
    }

    public override void OnUpdate()
    {
    }

    public override void OnExit()
    {
    }
}
