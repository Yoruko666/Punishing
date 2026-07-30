using UnityEngine;
public abstract class CharacterModule : MonoBehaviour
{
    /// <summary>所属 PlayerController，由 Initialize 注入</summary>
    public PlayerController Owner { get; private set; }

    /// <summary>PlayerController.Start() 调此方法注入 Owner 并触发初始化</summary>
    public void Initialize(PlayerController owner)
    {
        Owner = owner;
        OnModuleInit();
    }

    /// <summary>子类在此做初始化（此时 Owner 已可用）</summary>
    public virtual void OnModuleInit() { }

    // ================ 信号球系统钩子 ================

    /// <summary>
    /// 消球时询问 Module 是否替换释放的技能 ID。
    /// 返回 true 时用 overrideSkillId 替代默认映射，false 走默认（颜色→SkillId）。
    /// </summary>
    public virtual bool TryOverrideOrbSkill(PlayerController.SignalOrbType type, int matchCount, out string overrideSkillId)
    {
        overrideSkillId = null;
        return false;
    }

    /// <summary>
    /// 生成新信号球时，Module 可强制指定颜色。
    /// 返回 true 表示使用 overrideType，false 走默认随机颜色。
    /// </summary>
    public virtual bool GetOrbOverride(out PlayerController.SignalOrbType overrideType)
    {
        overrideType = PlayerController.SignalOrbType.Red;
        return false;
    }

    // ================ 属性 ================

    /// <summary>
    /// 属性修改时的钳制/特殊逻辑。
    /// value 为本次修改后未钳制的数值，返回钳制后的最终值。
    /// 默认直接返回 value（无钳制）。
    /// </summary>
    public virtual float ApplyAttributeClamp(string attributeName, float value) => value;

}
