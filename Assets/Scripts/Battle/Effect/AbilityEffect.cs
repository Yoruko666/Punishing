using Newtonsoft.Json;

/// <summary>
/// Ability 效果基类。普通攻击、技能、闪避统一为 Ability，
/// 每个 Ability 可配置任意组合的 AbilityEffect（连招/无敌/位移等），
/// 由 AbilityState 在对应生命周期调用。
/// 通过 Type 字段进行多态反序列化（见 AbilityEffectConverter）。
/// </summary>
[JsonConverter(typeof(AbilityEffectConverter))]
public abstract class AbilityEffect
{
    /// <summary>效果类型标识，用于反序列化，如 "Combo" / "Invincible" / "Move"</summary>
    public string Type;

    /// <summary>Ability 进入时触发（瞬时类效果在此执行，如修改 comboIndex）</summary>
    public virtual void OnEnter(PlayerController owner) { }

    /// <summary>Ability 进行中每帧触发（持续类效果在此执行，如无敌窗口、位移）</summary>
    public virtual void OnUpdate(PlayerController owner, float timer) { }

    /// <summary>Ability 结束/被打断时触发（用于收尾清理，如关闭无敌）</summary>
    public virtual void OnExit(PlayerController owner) { }
}
