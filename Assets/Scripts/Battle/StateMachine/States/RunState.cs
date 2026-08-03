using UnityEngine;

public class RunState : StateBase
{
    private float _timer;
    private RunStage _runStage;
    private readonly float _runStartTime = 0.833f;
    private readonly float _runEndTime_L = 1.7f;
    private readonly float _runEndTime_R = 1.5f;

    public RunState(PlayerController owner) : base(owner)
    {
    }

    public override void OnEnter()
    {
        _timer = _runStartTime;
        _runStage = RunStage.Start;
        Owner.PlayAnim("RunStart");
    }

    public override void OnUpdate()
    {
        switch (_runStage)
        {
            case RunStage.Start:
                RotateTowards(GetDirection());
                _timer -= Time.deltaTime;
                if (_timer <= 0)
                {
                    _runStage = RunStage.Loop;
                    Owner.PlayAnim("Run");
                }
                CheckStop();
                break;
            case RunStage.Loop:
                RotateTowards(GetDirection());
                Owner.CharacterController.Move(Owner.Speed * Time.deltaTime * Owner.transform.forward);
                CheckStop();
                break;
            case RunStage.End:
                _timer -= Time.deltaTime;
                if (_timer <= 0)
                {
                    Owner.SwitchState(PlayerState.Idle);
                }
                if (InputManager.Instance.CheckMoveInput())
                {
                    Owner.SwitchState(PlayerState.Run);
                }
                break;
        }
    }

    public override void OnExit()
    {
    }

    private void CheckStop()
    {
        if (!InputManager.Instance.CheckMoveInput())
        {
            _runStage = RunStage.End;
            if (_runStage == RunStage.Start || Owner.GetAnimNormalizedTime() < 0.5f)
            {
                _timer = _runEndTime_L;
                Owner.PlayAnim("RunEnd_L");
            }
            else
            {
                _timer = _runEndTime_R;
                Owner.PlayAnim("RunEnd_R");
            }
        }
    }

    private Vector3 GetDirection()
    {
        return (InputManager.Instance.MoveInput.y * CameraController.Instance.GetForwardVector()
               + InputManager.Instance.MoveInput.x * CameraController.Instance.GetRightVector()).normalized;
    }

    private void RotateTowards(Vector3 targetDirection)
    {
        if (targetDirection.sqrMagnitude < 0.001f) return;
        Quaternion currentRotation = Quaternion.LookRotation(Owner.transform.forward);
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        Owner.transform.rotation = Quaternion.RotateTowards(currentRotation, targetRotation, 720 * Time.deltaTime);
    }

    private enum RunStage
    {
        Start, Loop, End
    }

    private enum FrontLeg
    {
        Left, Right
    }
}
