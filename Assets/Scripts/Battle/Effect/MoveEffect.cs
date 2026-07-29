using UnityEngine;

/// <summary>相对角色朝向的位移方向</summary>
public enum MoveDirection
{
    Forward,
    Backward,
    Left,
    Right
}

/// <summary>
/// 位移效果。在 [StartTime, EndTime] 窗口内，按指定方向以 Speed 推动角色。
/// 可给闪避（Dodge）或带冲刺的攻击 Ability 配置，实现实际位移。
/// </summary>
public class MoveEffect : AbilityEffect
{
    public float StartTime = 0f;
    public float EndTime = 0.3f;
    public float Speed = 8f;
    public MoveDirection Direction = MoveDirection.Forward;

    public override void OnUpdate(PlayerController owner, float timer)
    {
        if (timer < StartTime || timer > EndTime) return;
        if (owner.CharacterController == null) return;

        Vector3 dir = Direction switch
        {
            MoveDirection.Forward => owner.transform.forward,
            MoveDirection.Backward => -owner.transform.forward,
            MoveDirection.Left => -owner.transform.right,
            MoveDirection.Right => owner.transform.right,
            _ => Vector3.zero
        };
        owner.CharacterController.Move(Speed * Time.deltaTime * dir);
    }
}
