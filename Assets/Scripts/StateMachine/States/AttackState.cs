using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AttackState : StateBase
{
    private List<AttackConfig> attackList;
    private AttackConfig currentAttack;
    private float timer;
    private int EffectIndex = 0;
    private int SoundIndex = 0;
    private bool bufferedAttack;

    public AttackState(PlayerController owner) : base(owner)
    {
        attackList = owner.PlayerConfig.AttackList;
    }

    public override void OnEnter()
    {
        Owner.CanAction = false;
        currentAttack = attackList[Owner.ComboIndex];
        Owner.PlayAnim(currentAttack.AnimName);
        Owner.ComboIndex++;
        if (Owner.ComboIndex >= attackList.Count)
            Owner.ComboIndex = 0;
        timer = 0;
        EffectIndex = 0;
        SoundIndex = 0;
        bufferedAttack = false;
    }

    public override void OnUpdate()
    {
        timer += Time.deltaTime;

        // 预输入窗口：ComboTime 前 0.3s 内按下攻击键则缓存
        float preInputStart = currentAttack.ComboTime - 0.3f;
        if (timer > preInputStart && Input.GetKeyDown(KeyCode.Mouse0))
        {
            bufferedAttack = true;
        }

        // ComboTime 已过 → 若有缓存则立即连击，否则开放动作
        if (timer > currentAttack.ComboTime)
        {
            if (bufferedAttack)
            {
                Owner.SwitchState(PlayerState.Attack);
                return;
            }
            Owner.CanAction = true;
        }

        // ExitTime 已过 → 连击链结束，重置索引
        if (timer > currentAttack.ExitTime)
        {
            Owner.ComboIndex = 0;
        }

        // 动画结束
        if (timer > currentAttack.AnimTime)
        {
            Owner.SwitchState(PlayerState.Idle);
        }

        // 按时间线执行特效
        while (currentAttack.EffectList != null && EffectIndex < currentAttack.EffectList.Count && timer > currentAttack.EffectList[EffectIndex].ExecTime)
        {
            SkillEffect effect = currentAttack.EffectList[EffectIndex];
            Addressables.LoadAssetAsync<GameObject>(effect.EffectName).Completed += (obj) =>
            {
                if (obj.Status == AsyncOperationStatus.Succeeded)
                {
                    GameObject go = GameObject.Instantiate(obj.Result, Owner.transform);
                    go.transform.SetLocalPositionAndRotation(effect.PositionOffset, Quaternion.Euler(effect.RotationOffset));
                }
            };
            EffectIndex++;
        }

        // 按时间线执行音效
        while (currentAttack.SoundEffectList != null && SoundIndex < currentAttack.SoundEffectList.Count && timer > currentAttack.SoundEffectList[SoundIndex].ExecTime)
        {
            Owner.PlaySound(currentAttack.SoundEffectList[SoundIndex].SoundName);
            SoundIndex++;
        }
    }

    public override void OnExit()
    {
    }
}
