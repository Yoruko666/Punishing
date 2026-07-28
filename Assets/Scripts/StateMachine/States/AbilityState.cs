using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 统一 Ability 状态：普通攻击、技能、闪避都由它驱动。
/// 按时间线播放动画/特效/音效，执行 AbilityEffect，并处理派生 / 普攻缓冲。
/// </summary>
public class AbilityState : StateBase
{
    private AbilityConfig currentAbility;
    private float timer;
    private int effectIndex;
    private int soundIndex;
    private bool bufferedCombo;
    private AbilityDerivation bufferedDerivation;

    /// <summary>当前 Ability 是否为普攻连招链的一员（链内 Ability 在 ExitTime 即派生，链外持缓冲到动画结束）</summary>
    private bool IsComboAbility =>
        currentAbility != null && Owner.PlayerConfig?.ComboAbilityIds?.Contains(currentAbility.Id) == true;

    public AbilityState(PlayerController owner) : base(owner)
    {
    }

    public override void OnEnter()
    {
        Owner.CanAction = false;
        currentAbility = Owner.PendingAbility;
        if (currentAbility == null)
        {
            Owner.SwitchState(PlayerState.Idle);
            return;
        }

        Owner.PlayAnim(currentAbility.AnimName);
        timer = 0;
        effectIndex = 0;
        soundIndex = 0;
        bufferedCombo = false;
        bufferedDerivation = null;

        if (currentAbility.CoolDown > 0)
            Owner.StartAbilityCoolDown(currentAbility.Id);

        // 应用「进入即触发」的效果（如 ComboEffect 修改连招索引）
        if (currentAbility.AbilityEffects != null)
        {
            foreach (var effect in currentAbility.AbilityEffects)
                effect?.OnEnter(Owner);
        }
    }

    public override void OnUpdate()
    {
        timer += Time.deltaTime;

        // 持续型效果（无敌窗口、位移、伤害等）
        if (currentAbility.AbilityEffects != null)
        {
            foreach (var effect in currentAbility.AbilityEffects)
                effect?.OnUpdate(Owner, timer);
        }

        // 派生（招式树）：优先于普攻缓冲
        if (TryHandleDerivation())
            return;

        // 持续监听攻击键：
        // 普通招式从 ExitTime 前 0.3s 起监听；ListenAttackFromStart 的招式（闪避）整个过程都监听
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            if (currentAbility.ListenAttackFromStart || timer > currentAbility.ExitTime - 0.3f)
                bufferedCombo = true;
        }

        // ExitTime 已过 → 开放动作
        if (timer > currentAbility.ExitTime)
            Owner.CanAction = true;

        // ExitTime 已过且有攻击缓冲 → 普攻链内 Ability、或允许从头预输入的 Ability（闪避）立即派生普攻
        if (timer > currentAbility.ExitTime && bufferedCombo && (IsComboAbility || currentAbility.ListenAttackFromStart))
        {
            Owner.ActivateComboAttack();
            return;
        }

        // 动画结束 → 有缓冲则派生（闪避等非链内 Ability 的预输入在此释放），否则重置连招回 Idle
        if (timer > currentAbility.AnimTime)
        {
            if (bufferedCombo)
            {
                Owner.ActivateComboAttack();
                return;
            }
            Owner.ComboIndex = 0;
            Owner.SwitchState(PlayerState.Idle);
            return;
        }

        // 按时间线执行视觉特效
        while (currentAbility.EffectList != null && effectIndex < currentAbility.EffectList.Count && timer > currentAbility.EffectList[effectIndex].ExecTime)
        {
            SkillEffect effect = currentAbility.EffectList[effectIndex];
            Addressables.LoadAssetAsync<GameObject>(effect.EffectName).Completed += (obj) =>
            {
                if (obj.Status == AsyncOperationStatus.Succeeded)
                {
                    GameObject go = Object.Instantiate(obj.Result, Owner.transform);
                    go.transform.SetLocalPositionAndRotation(effect.PositionOffset, Quaternion.Euler(effect.RotationOffset));
                }
            };
            effectIndex++;
        }

        // 按时间线执行音效
        while (currentAbility.SoundEffectList != null && soundIndex < currentAbility.SoundEffectList.Count && timer > currentAbility.SoundEffectList[soundIndex].ExecTime)
        {
            Owner.PlaySound(currentAbility.SoundEffectList[soundIndex].SoundName);
            soundIndex++;
        }
    }

    /// <summary>
    /// 检测并执行招式派生。支持提前 0.3s 预输入缓冲，
    /// 到达派生窗口起点后切换到目标 Ability。返回 true 表示已发生派生。
    /// </summary>
    private bool TryHandleDerivation()
    {
        if (currentAbility.Derivations == null || currentAbility.Derivations.Count == 0)
            return false;

        // 缓冲：在窗口（含 0.3s 预输入）内按下对应输入
        if (bufferedDerivation == null)
        {
            foreach (var d in currentAbility.Derivations)
            {
                float winEnd = d.EndTime > 0 ? d.EndTime : currentAbility.AnimTime;
                if (timer >= d.StartTime - 0.3f && timer <= winEnd && Owner.GetDeriveInputDown(d.Input))
                {
                    bufferedDerivation = d;
                    break;
                }
            }
        }

        // 执行：到达窗口起点后切换
        if (bufferedDerivation != null && timer >= bufferedDerivation.StartTime)
        {
            if (Owner.ActivateAbilityById(bufferedDerivation.TargetId))
                return true;

            // 派生失败（如目标在冷却）→ 清除缓冲，避免卡住
            bufferedDerivation = null;
        }

        return false;
    }

    public override void OnExit()
    {
        // 收尾清理（如关闭无敌）
        if (currentAbility?.AbilityEffects != null)
        {
            foreach (var effect in currentAbility.AbilityEffects)
                effect?.OnExit(Owner);
        }
    }
}
