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

        // 持续监听攻击键：从 ExitTime 前 0.3s 起预输入缓冲
        if (InputManager.Instance.AttackPressed)
        {
            if (timer > currentAbility.ExitTime - 0.3f)
                bufferedCombo = true;
        }

        // ExitTime 已过 → 开放动作
        if (timer > currentAbility.ExitTime)
        {
            // 任意 Ability 都支持预输入续连：有缓冲则按当前 ComboIndex 派生下一段
            // （闪避/技能可用 ComboEffect 预设段位，预输入派生时被读到）
            if (bufferedCombo)
            {
                Owner.ActivateComboAttack();
                return;
            }
            // 无预输入 → 连招中断，归零
            Owner.ResetCombo();
            Owner.CanAction = true;
        }

        // 动画结束 → 回 Idle（预输入已在 ExitTime 处派生，此处不再处理）
        if (timer > currentAbility.AnimTime)
        {
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

    public override void OnExit()
    {
        if (currentAbility?.AbilityEffects != null)
        {
            foreach (var effect in currentAbility.AbilityEffects)
                effect?.OnExit(Owner);
        }
    }
}
