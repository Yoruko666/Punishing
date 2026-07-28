/// <summary>
/// 设置 ComboIndex 的 Ability 效果。
/// - Value = N → 直接设为 N，超出连招链长度则回绕到 0
///
/// 配置示例：
///   普攻链共 5 段，Attack1 配 { "Value": 1 } → 打完后 ComboIndex=1，
///   退出窗口再接键则派生 Attack2（combo[1]）。
///   技能配 { "Value": 0 } → 打断连招。
///   闪避配 { "Value": 2 } → 闪避后接第三段普攻。
/// </summary>
public class ComboEffect : AbilityEffect
{
    public int Value = 0;

    public override void OnEnter(PlayerController owner)
    {
        owner.ApplyCombo(Value);
    }
}
