/// <summary>
/// Lucia 专属模块（保留壳，待后续新增机制）。
/// SpSkill 键位4 逻辑已移除，配置保留在 JSON 中。
/// </summary>
public class LuciaModule : CharacterModule
{
    // ================ 属性钳制 ================

    public override float ApplyAttributeClamp(string attributeName, float value)
    {
        return value;
    }
}
