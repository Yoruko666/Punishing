using UnityEngine;
using System.Collections;

public class LuciaModule : CharacterModule
{
    // ================ Blade Will 状态 ================

    /// <summary>三消蓝后是否准备好进入 Blade Will（下一次三消任意色触发）</summary>
    private bool _bladeWillReady;

    /// <summary>Blade Will 状态是否激活</summary>
    private bool _bladeWillActive;

    /// <summary>SpSkill 循环索引（0~3 对应 SpSkill1~4）</summary>
    private int _spSkillCycleIndex;

    /// <summary>剑意 5 秒倒计时协程引用，用于提前取消（如重复进入）</summary>
    private Coroutine _bladeWillCoroutine;

    /// <summary>三消蓝就绪状态 5 秒超时协程引用</summary>
    private Coroutine _bladeWillReadyCoroutine;

    private const float BladeWillDuration = 5f;

    // ================ 初始化 ================

    public override void OnModuleInit()
    {
        _bladeWillReady = false;
        _bladeWillActive = false;
        _spSkillCycleIndex = 0;
        StopCoroutineSafe(ref _bladeWillCoroutine);
        StopCoroutineSafe(ref _bladeWillReadyCoroutine);
    }

    // ================ 信号球系统 ================

    public override bool TryOverrideOrbSkill(PlayerController.SignalOrbType type, int matchCount, out string overrideSkillId)
    {
        overrideSkillId = null;

        // 1) Blade Will 中白色球 → SpSkill 循环
        if (_bladeWillActive && type == PlayerController.SignalOrbType.White)
        {
            overrideSkillId = $"SpSkill{_spSkillCycleIndex + 1}";
            _spSkillCycleIndex = (_spSkillCycleIndex + 1) % 4;
            return true;
        }

        // 2) Blade Will 就绪时任意三消 → 进入 Blade Will（必须先于"三消蓝标记"，否则再次三消蓝会被截获）
        if (_bladeWillReady && matchCount == 3)
        {
            EnterBladeWill();
            overrideSkillId = "BladeWillEntry";
            return true;
        }

        // 3) 三消蓝 → 标记 Blade Will 就绪（只有不在 Blade Will 就绪状态下时才触发标记）
        if (type == PlayerController.SignalOrbType.Blue && matchCount == 3)
        {
            _bladeWillReady = true;
            // 重置 5 秒超时：就绪后 5 秒内没三消则自动过期
            StopCoroutineSafe(ref _bladeWillReadyCoroutine);
            _bladeWillReadyCoroutine = StartCoroutine(BladeWillReadyTimerCoroutine());
            Debug.Log("[LuciaModule] 三消蓝 —— Blade Will 就绪（5秒内三消触发 Blade Will）");
            return false; // 仍执行普通蓝技能
        }

        return false;
    }

    public override bool GetOrbOverride(out PlayerController.SignalOrbType overrideType)
    {
        if (_bladeWillActive)
        {
            overrideType = PlayerController.SignalOrbType.White;
            return true;
        }
        overrideType = PlayerController.SignalOrbType.Red;
        return false;
    }

    // ================ Blade Will 状态转换 ================

    private void EnterBladeWill()
    {
        _bladeWillReady = false;
        _bladeWillActive = true;
        _spSkillCycleIndex = 0;

        // 停止就绪超时和之前剑意协程
        StopCoroutineSafe(ref _bladeWillReadyCoroutine);
        StopCoroutineSafe(ref _bladeWillCoroutine);
        _bladeWillCoroutine = StartCoroutine(BladeWillTimerCoroutine());

        // 剩余球全转白色 + 额外 2 颗
        ConvertAllOrbsToWhite(2);

        Debug.Log("[LuciaModule] 进入 Blade Will 状态（5秒）—— 所有球转为白色");
    }

    private IEnumerator BladeWillTimerCoroutine()
    {
        yield return new WaitForSeconds(BladeWillDuration);
        ExitBladeWill();
    }

    /// <summary>三消蓝就绪超时：5 秒内未触发则自动取消就绪状态。</summary>
    private IEnumerator BladeWillReadyTimerCoroutine()
    {
        yield return new WaitForSeconds(BladeWillDuration);
        _bladeWillReady = false;
        _bladeWillReadyCoroutine = null;
        Debug.Log("[LuciaModule] Blade Will 就绪已过期（5秒未触发）");
    }

    /// <summary>安全停止并置空协程引用。</summary>
    private void StopCoroutineSafe(ref Coroutine co)
    {
        if (co != null)
        {
            StopCoroutine(co);
            co = null;
        }
    }

    private void ExitBladeWill()
    {
        _bladeWillActive = false;
        _bladeWillCoroutine = null;

        // 剩余白色球转回随机色
        ConvertWhiteOrbsToRandom();

        Debug.Log("[LuciaModule] 退出 Blade Will 状态 —— 剩余白色球已转回随机色");
    }

    // ================ 信号球转换（原 PlayerController 露西亚专有逻辑） ================

    /// <summary>将所有球转为白色，并额外追加 bonusCount 颗（Blade Will 入场用）。</summary>
    private void ConvertAllOrbsToWhite(int bonusCount)
    {
        int existing = Owner.OrbCount;
        Owner.ClearOrbs();
        int total = Mathf.Min(existing + bonusCount, Owner.GetMaxDataSignalOrbs());
        for (int i = 0; i < total; i++)
            Owner.GenerateSignalOrb(PlayerController.SignalOrbType.White);
    }

    /// <summary>将所有白色球转回随机色（红/黄/蓝），Blade Will 退出用。</summary>
    private void ConvertWhiteOrbsToRandom()
    {
        int count = Owner.OrbCount;
        for (int i = 0; i < count; i++)
        {
            if (Owner.GetOrbType(i) == PlayerController.SignalOrbType.White)
                Owner.SetOrbType(i, (PlayerController.SignalOrbType)Random.Range(0, 3));
        }
        Owner.NotifyOrbsReset();
        Owner.RebuildOrbGroups();
    }

    // ================ 属性钳制 ================

    public override float ApplyAttributeClamp(string attributeName, float value)
    {
        return value;
    }

}
