/// <summary>
/// 通用的属性修改效果。通过 AttributeName 指定要修改的属性，
/// 同一效果类可处理任意角色的任意属性（剑气、怒气、能量等），
/// 无需为每个属性创建专属 AbilityEffect 子类。
///
/// 配置示例：
///   { "Type": "ModifyAttribute", "AttributeName": "Rage", "Value": 10 }
///   { "Type": "ModifyAttribute", "AttributeName": "Health", "Value": -5 }
/// </summary>
public class ModifyAttributeEffect : AbilityEffect
{
    /// <summary>属性名称（字符串，无 enum 约束，支持任意自定义属性）</summary>
    public string AttributeName;

    /// <summary>修改值（正数增加，负数减少）</summary>
    public float Value = 0;

    public override void OnEnter(PlayerController owner)
    {
        owner.ModifyAttribute(AttributeName, Value);
    }
}
