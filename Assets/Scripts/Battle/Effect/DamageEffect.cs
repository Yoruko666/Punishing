using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 伤害效果。在 ExecTime 时刻执行一次范围检测，
/// 对命中的 IDamageable 目标造成 BaseDamage × DamageMultiplier 点伤害。
/// 通过 DetectionShape 配置检测形状（球体/立方体等），默认检测 Enemy 层。
/// </summary>
public class DamageEffect : AbilityEffect
{
    /// <summary>伤害判定触发时刻（秒）</summary>
    public float ExecTime = 0.2f;

    /// <summary>基础伤害值，最终伤害 = BaseDamage × AbilityConfig.DamageMultiplier</summary>
    public float BaseDamage = 10f;

    /// <summary>检测形状配置（支持 Sphere / Box 等，通过 DetectionShapeConverter 反序列化）</summary>
    public DetectionShape DetectionShape;

    private bool _executed;
    private static readonly int EnemyLayerMask = LayerMask.GetMask("Enemy");
    private readonly HashSet<Collider> _hitTargets = new();

    /// <summary>上次伤害判定的时刻（Time.time），Gizmo 用，0 表示未执行过。</summary>
    public float LastExecutedTime { get; private set; }

    public override void OnEnter(PlayerController owner)
    {
        _executed = false;
        _hitTargets.Clear();
    }

    public override void OnUpdate(PlayerController owner, float timer)
    {
        if (_executed || timer < ExecTime) return;
        if (DetectionShape == null) return;

        _executed = true;
        LastExecutedTime = Time.time;

        float damage = BaseDamage * owner.PendingAbility.DamageMultiplier;

        Collider[] hits = DetectionShape.Detect(owner.transform, EnemyLayerMask);
        foreach (var hit in hits)
        {
            if (hit == null || _hitTargets.Contains(hit)) continue;
            _hitTargets.Add(hit);

            if (hit.TryGetComponent<IDamageable>(out var target))
            {
                if (target is PlayerController pc && pc.IsInvincible) continue;
                target.TakeDamage(damage);
            }
        }
    }

    public override void OnExit(PlayerController owner)
    {
        _hitTargets.Clear();
    }
}
