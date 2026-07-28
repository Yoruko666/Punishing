/// <summary>
/// 无敌（无敌帧）效果。在 [StartTime, EndTime] 时间窗口内将角色置为无敌，
/// 供后续受击/伤害系统查询 PlayerController.IsInvincible。
/// 主要用于闪避（Dodge）这类 Ability。
/// </summary>
public class InvincibleEffect : AbilityEffect
{
    public float StartTime = 0f;
    public float EndTime = 0.4f;

    public override void OnUpdate(PlayerController owner, float timer)
    {
        owner.IsInvincible = timer >= StartTime && timer <= EndTime;
    }

    public override void OnExit(PlayerController owner)
    {
        // Ability 结束/被打断时兜底关闭无敌，避免残留
        owner.IsInvincible = false;
    }
}
