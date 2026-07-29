using System.Collections.Generic;

public class PlayerConfig
{
    /// <summary>所有 Ability 定义（普攻/技能/闪避统一存放，用 Id 区分）</summary>
    public List<AbilityConfig> Abilities;

    /// <summary>普通攻击连招链，按 ComboIndex 顺序排列的 Ability Id</summary>
    public List<string> ComboAbilityIds;

    /// <summary>数字键 1/2/3/4 对应的 Ability Id</summary>
    public List<string> SkillAbilityIds;

    /// <summary>前向闪避 Ability Id</summary>
    public string DodgeForwardId;

    /// <summary>后向闪避 Ability Id</summary>
    public string DodgeBackwardId;
}
