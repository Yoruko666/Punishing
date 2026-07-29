using UnityEngine;

/// <summary>
/// 敌人基础控制器。
/// 当前阶段仅实现「站立 + 可受击」，用于给角色的攻击提供伤害检测目标。
/// 行动逻辑后续由行为树接入（不复用角色状态机），因此这里不做任何移动/攻击处理，
/// 仅保留受击相关的可重写钩子（OnHit / OnDead）供后续扩展。
/// </summary>
[RequireComponent(typeof(Collider))]
public class EnemyController : CharacterBase, IDamageable
{
    [Header("生命值")]
    public float MaxHealth = 100f;

    /// <summary>当前生命值，运行时由受击逻辑更新</summary>
    public float CurrentHealth { get; private set; }

    /// <summary>是否已死亡</summary>
    public bool IsDead => CurrentHealth <= 0f;

    protected override void Start()
    {
        base.Start();
        CurrentHealth = MaxHealth;
    }

    /// <summary>
    /// 受到伤害。由攻击方（DamageEffect）通过 IDamageable 接口调用。
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        Debug.Log($"{name} 受到 {amount} 点伤害，剩余生命值 {CurrentHealth}/{MaxHealth}");

        if (IsDead)
            OnDead();
        else
            OnHit(amount);
    }

    /// <summary>受击反馈（受击动画 / 音效 / 硬直等）。站立测试敌人暂不实现。</summary>
    protected virtual void OnHit(float amount)
    {
    }

    /// <summary>死亡处理。后续可接入死亡动画、掉落、销毁等逻辑。</summary>
    protected virtual void OnDead()
    {
        Debug.Log($"{name} 已死亡");
    }
}
