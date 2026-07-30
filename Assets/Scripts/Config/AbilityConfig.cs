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

    /// <summary>超出此时间点后开放后续输入。也是最晚预输入截止点。</summary>
    public float ExitTime;

    /// <summary>冷却（秒），0 表示无冷却（普攻/闪避）</summary>
    public float CoolDown;

    public float DamageMultiplier;

    /// <summary>信号球贴图（Addressables key），供信号球 UI 显示。为空则回退到纯色显示。</summary>
    public string OrbSprite;

    /// <summary>视觉特效时间线</summary>
    public List<SkillEffect> EffectList;

    /// <summary>音效时间线</summary>
    public List<SoundEffect> SoundEffectList;

    /// <summary>Ability 效果（连招索引/无敌/位移等），可自由组合</summary>
    public List<AbilityEffect> AbilityEffects;

}
