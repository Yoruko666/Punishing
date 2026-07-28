using System.Collections.Generic;

/// <summary>
/// 统一的 Ability 配置。普通攻击、技能、闪避都用它描述。
/// 通过 AbilityEffects 配置各类可组合的效果（连招/无敌/位移等）。
/// </summary>
public class AbilityConfig
{
    /// <summary>Ability 唯一标识，用于绑定与冷却，如 "Attack1" / "Skill1" / "DodgeForward"</summary>
    public string Id;

    public string AnimName;

    /// <summary>动画总时长，超过后回到 Idle</summary>
    public float AnimTime;



    /// <summary>超出此时间点后，派生窗口关闭且开放后续输入。也是最晚预输入截止点。</summary>
    public float ExitTime;

    /// <summary>
    /// 为 true 时：整个 Ability 过程（timer 从 0 起）都监听攻击预输入，
    /// 一旦在 ExitTime 前缓冲了攻击，到达 ExitTime 即释放普攻（与普攻链派发时机一致）。
    /// 用于闪避取消接普攻等场景。
    /// </summary>
    public bool ListenAttackFromStart;

    /// <summary>冷却（秒），0 表示无冷却（普攻/闪避）</summary>
    public float CoolDown;

    public float DamageMultiplier;

    /// <summary>视觉特效时间线</summary>
    public List<SkillEffect> EffectList;

    /// <summary>音效时间线</summary>
    public List<SoundEffect> SoundEffectList;

    /// <summary>Ability 效果（连招索引/无敌/位移等），可自由组合</summary>
    public List<AbilityEffect> AbilityEffects;

    /// <summary>派生配置：在指定窗口内按指定输入可切换到目标 Ability（招式树）</summary>
    public List<AbilityDerivation> Derivations;
}
