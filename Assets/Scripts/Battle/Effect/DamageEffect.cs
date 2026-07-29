using UnityEngine;

/// <summary>
/// 伤害效果。在 ExecTime 时刻于角色前方做一次球形范围检测，
/// 对命中的 IDamageable 目标造成 Damage 点伤害。
/// 可与 MoveEffect 组合实现「突进 + 命中」的位移技能。
/// </summary>
public class DamageEffect : AbilityEffect
{
    /// <summary>命中判定触发时刻（秒）</summary>
    public float ExecTime = 0.2f;

    /// <summary>造成的伤害值</summary>
    public float Damage = 10f;

    /// <summary>检测中心相对角色前方的距离</summary>
    public float Range = 2f;

    /// <summary>检测球半径</summary>
    public float Radius = 1.5f;

    private bool _executed;

    public override void OnEnter(PlayerController owner)
    {
        _executed = false;
    }

    public override void OnUpdate(PlayerController owner, float timer)
    {
        if (_executed || timer < ExecTime) return;
        _executed = true;

        Vector3 center = owner.transform.position + owner.transform.forward * Range;
        Collider[] hits = Physics.OverlapSphere(center, Radius);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target))
                target.TakeDamage(Damage);
        }
    }
}
