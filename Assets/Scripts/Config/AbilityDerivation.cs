/// <summary>可触发派生的输入类型</summary>
public enum DeriveInput
{
    Attack,   // 鼠标左键
    Dodge,    // 闪避键（Shift）
    Hotkey1,  // 数字键 1
    Hotkey2,  // 数字键 2
    Hotkey3,  // 数字键 3
    Hotkey4   // 数字键 4
}

/// <summary>
/// Ability 派生配置：在 [StartTime, EndTime] 时间窗口内按下指定 Input，
/// 即派生（切换）到 TargetId 对应的 Ability，用于实现招式树 / 派生连招。
/// 例如「普攻第一段 → 按 1 键接特殊技能」。
/// </summary>
public class AbilityDerivation
{
    /// <summary>触发派生的输入</summary>
    public DeriveInput Input;

    /// <summary>派生窗口起点（秒）；派生在此刻之后才会真正执行，支持提前 0.3s 预输入</summary>
    public float StartTime;

    /// <summary>派生窗口终点（秒）；为 0 时表示直到动画结束（AnimTime）</summary>
    public float EndTime;

    /// <summary>派生目标 Ability 的 Id</summary>
    public string TargetId;
}
