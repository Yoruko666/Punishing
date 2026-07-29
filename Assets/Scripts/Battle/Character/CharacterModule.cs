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

    // ================ Ability 生命周期钩子 ================

    /// <summary>
    /// 每帧 AbilityState.OnUpdate 时调用，用于角色专属的预输入缓冲。
    /// </summary>
    public virtual void OnAbilityUpdate(float timer, float exitTime) { }

    /// <summary>
    /// 在 AbilityState 到达 ExitTime 时调用。
    /// 返回 true 表示已激活一个缓冲技能（后续不再处理普攻预输入/归零）。
    /// </summary>
    public virtual bool TryActivateBufferedSkill() => false;

    /// <summary>
    /// ExitTime 到达且无任何预输入时调用，用于重置角色专属段数/状态。
    /// </summary>
    public virtual void OnAbilityExitNoBuffer() { }

    // ================ 输入 ================

    /// <summary>
    /// 处理角色专属的技能键（如按键 4 的 SpSkill）。
    /// skillIndex: 0~3 对应 1/2/3/4。
    /// 返回 true 表示已消耗该输入，PlayerController 不再继续处理后续键。
    /// </summary>
    public virtual bool HandleSkillKey(int skillIndex) => false;

    // ================ 属性 ================

    /// <summary>
    /// 属性修改时的钳制/特殊逻辑。
    /// value 为本次修改后未钳制的数值，返回钳制后的最终值。
    /// 默认直接返回 value（无钳制）。
    /// </summary>
    public virtual float ApplyAttributeClamp(string attributeName, float value) => value;
}
