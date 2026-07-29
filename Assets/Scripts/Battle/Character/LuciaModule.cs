/// <summary>
/// Lucia 专属模块：剑气（SwordEnergy）+ SpSkill 段数循环。
/// 所有与 Lucia 角色机制相关的逻辑集中在此，不污染 PlayerController / AbilityState。
/// </summary>
public class LuciaModule : CharacterModule
{
    private int _spSkillCycleIndex;
    private bool _bufferedSpSkill;
    private const int MaxSwordEnergy = 12;

    private static readonly string[] SpSkillIds = { "SpSkill1", "SpSkill2", "SpSkill3", "SpSkill4" };

    // ================ 剑气快捷方法 ================

    private int GetSwordEnergy() => (int)Owner.GetAttribute(AttributeTypes.LuciaSwordEnergy);

    private void AddSwordEnergy(int value) => Owner.ModifyAttribute(AttributeTypes.LuciaSwordEnergy, value);

    // ================ Ability 预输入 ================

    public override void OnAbilityUpdate(float timer, float exitTime)
    {
        if (InputManager.Instance.SkillPressed(3) && timer > exitTime - 0.3f)
            _bufferedSpSkill = true;
    }

    public override bool TryActivateBufferedSkill()
    {
        if (!_bufferedSpSkill) return false;
        _bufferedSpSkill = false;
        return ActivateSpSkill();
    }

    public override void OnAbilityExitNoBuffer()
    {
        _spSkillCycleIndex = 0;
    }

    // ================ 按键 4 处理 ================

    public override bool HandleSkillKey(int skillIndex)
    {
        if (skillIndex != 3) return false; // 只处理按键 4
        return ActivateSpSkill();
    }

    // ================ SpSkill 释放 ================

    /// <summary>消耗一层剑气，释放当前段数的 SpSkill 并推进到下一段</summary>
    private bool ActivateSpSkill()
    {
        if (GetSwordEnergy() <= 0) return false;
        AddSwordEnergy(-1);
        string spId = SpSkillIds[_spSkillCycleIndex];
        _spSkillCycleIndex = (_spSkillCycleIndex + 1) % SpSkillIds.Length;
        return Owner.ActivateAbilityById(spId);
    }

    // ================ 属性钳制 ================

    public override float ApplyAttributeClamp(string attributeName, float value)
    {
        if (attributeName == AttributeTypes.LuciaSwordEnergy)
            return UnityEngine.Mathf.Clamp(value, 0, MaxSwordEnergy);
        return value;
    }
}
