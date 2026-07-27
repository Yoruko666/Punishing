using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class SkillState : StateBase
{
    private SkillConfig currentSkill;
    private float timer;
    private int effectIndex;
    private int soundIndex;

    public SkillState(PlayerController owner) : base(owner)
    {
    }

    public override void OnEnter()
    {
        Owner.CanAction = false;
        var skillList = Owner.PlayerConfig.SkillList;
        currentSkill = skillList[Owner.SkillIndex];
        Owner.PlayAnim(currentSkill.AnimName);
        timer = 0;
        effectIndex = 0;
        soundIndex = 0;
        Owner.StartSkillCoolDown(Owner.SkillIndex);
    }

    public override void OnUpdate()
    {
        timer += Time.deltaTime;

        // 按时间线执行特效
        while (currentSkill.EffectList != null && effectIndex < currentSkill.EffectList.Count && timer > currentSkill.EffectList[effectIndex].ExecTime)
        {
            SkillEffect effect = currentSkill.EffectList[effectIndex];
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
        while (currentSkill.SoundEffectList != null && soundIndex < currentSkill.SoundEffectList.Count && timer > currentSkill.SoundEffectList[soundIndex].ExecTime)
        {
            Owner.PlaySound(currentSkill.SoundEffectList[soundIndex].SoundName);
            soundIndex++;
        }

        // 过了可中断时间后开放动作，由 PlayerController 处理输入
        if (timer > currentSkill.ExitTime)
        {
            Owner.CanAction = true;
        }

        // 技能动画播放完毕
        if (timer > currentSkill.AnimTime)
        {
            Owner.SwitchState(PlayerState.Idle);
        }
    }

    public override void OnExit()
    {
    }
}
