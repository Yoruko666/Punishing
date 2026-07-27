using UnityEngine;

public class DashState : StateBase
{
    private float _timer;
    private DashDirection _dashDirection;
    private float dashAnimTime;
    private float dashForwardTime = 1.1f;
    private float dashBackwardTime = 1.267f;

    public DashState(PlayerController owner, DashDirection direction) : base(owner)
    {
        _dashDirection = direction;
        dashAnimTime = direction == DashDirection.Forward ? dashForwardTime : dashBackwardTime;
    }

    public override void OnEnter()
    {
        Owner.CanAction = false;
        _timer = 0;
        if (_dashDirection == DashDirection.Forward)
        {
            Owner.PlayAnim("DashForward");
            Owner.PlaySound(Owner.PlayerConfig.DashForwardName);
        }
        else
        {
            Owner.PlayAnim("DashBackward");
            Owner.PlaySound(Owner.PlayerConfig.DashBackwardName);
        }
    }

    public override void OnUpdate()
    {
        _timer += Time.deltaTime;
        if (_timer >= 0.5f)
        {
            Owner.CanAction = true;
        }
        if (_timer >= dashAnimTime)
        {
            Owner.SwitchState(PlayerState.Idle);
        }
    }

    public override void OnExit()
    {
    }

    public enum DashDirection
    {
        Forward,
        Backward
    }
}
