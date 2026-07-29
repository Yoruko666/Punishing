/// <summary>可受击目标接口。任何能被攻击造成伤害的对象（敌人等）实现它。</summary>
public interface IDamageable
{
    void TakeDamage(float amount);
}
