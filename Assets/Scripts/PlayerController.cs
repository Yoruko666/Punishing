using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;

public class PlayerController : CharacterBase
{
    public StateMachine StateMachine;
    public CharacterController CharacterController;

    public PlayerConfig PlayerConfig;

    public float Speed = 4.5f;
    public bool CanAction = true;
    public int ComboIndex = 0;

    /// <summary>无敌标志，由 InvincibleEffect 控制，供受击/伤害系统查询</summary>
    public bool IsInvincible = false;

    /// <summary>当前待释放/正在释放的 Ability，由 AbilityState 在 OnEnter 读取</summary>
    public AbilityConfig PendingAbility { get; private set; }

    private readonly Dictionary<string, AbilityConfig> _abilityMap = new();
    private readonly Dictionary<string, float> _coolDowns = new();

    protected override void Start()
    {
        base.Start();

        LoadPlayerConfig();
        BuildAbilityMap();

        StateMachine = new StateMachine();
        StateMachine.RegisterState(PlayerState.Idle, new IdleState(this));
        StateMachine.RegisterState(PlayerState.Run, new RunState(this));
        StateMachine.RegisterState(PlayerState.Ability, new AbilityState(this));
        StateMachine.SwitchState(PlayerState.Idle);

        CharacterController = GetComponent<CharacterController>();
    }

    private void BuildAbilityMap()
    {
        _abilityMap.Clear();
        if (PlayerConfig?.Abilities == null) return;
        foreach (var ability in PlayerConfig.Abilities)
        {
            if (ability != null && !string.IsNullOrEmpty(ability.Id))
                _abilityMap[ability.Id] = ability;
        }
    }

    public AbilityConfig GetAbility(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        _abilityMap.TryGetValue(id, out var ability);
        return ability;
    }

    // ---------------- Ability 激活 ----------------

    /// <summary>按 Id 释放 Ability（含冷却检查）</summary>
    public bool ActivateAbilityById(string id)
    {
        AbilityConfig ability = GetAbility(id);
        if (ability == null) return false;
        if (GetCoolDownRemaining(id) > 0) return false; // 冷却中

        PendingAbility = ability;
        SwitchState(PlayerState.Ability);
        return true;
    }

    /// <summary>根据当前 ComboIndex 释放普通攻击链中对应的一段</summary>
    public bool ActivateComboAttack()
    {
        var combo = PlayerConfig?.ComboAbilityIds;
        if (combo == null || combo.Count == 0) return false;

        // 释放普攻时立刻转到移动输入方向
        Vector3 dir = GetInputDirection();
        if (dir != Vector3.zero)
            RotateImmediate(dir);

        int idx = Mathf.Clamp(ComboIndex, 0, combo.Count - 1);
        return ActivateAbilityById(combo[idx]);
    }

    /// <summary>设置连招索引（由 ComboEffect 调用）</summary>
    public void ApplyCombo(int value)
    {
        int count = PlayerConfig?.ComboAbilityIds?.Count ?? 0;
        if (count <= 0)
        {
            ComboIndex = 0;
            return;
        }

        if (value >= count) value = 0;
        if (value < 0) value = 0;

        ComboIndex = value;
    }

    // ---------------- 冷却 ----------------

    public void StartAbilityCoolDown(string id)
    {
        AbilityConfig ability = GetAbility(id);
        if (ability != null && ability.CoolDown > 0)
            _coolDowns[id] = ability.CoolDown;
    }

    public float GetCoolDownRemaining(string id)
    {
        if (string.IsNullOrEmpty(id)) return 0;
        _coolDowns.TryGetValue(id, out float remaining);
        return remaining > 0 ? remaining : 0;
    }

    private void UpdateCoolDowns()
    {
        if (_coolDowns.Count == 0) return;

        float dt = Time.deltaTime;
        // 复制键集合以便在遍历中修改字典的值
        var keys = new List<string>(_coolDowns.Keys);
        foreach (var key in keys)
        {
            if (_coolDowns[key] > 0)
                _coolDowns[key] -= dt;
        }
    }

    // ---------------- UI 辅助（热键 Ability） ----------------

    public AbilityConfig GetSkillAbility(int index)
    {
        var ids = PlayerConfig?.SkillAbilityIds;
        if (ids == null || index < 0 || index >= ids.Count) return null;
        return GetAbility(ids[index]);
    }

    /// <summary>技能 Ability 剩余冷却的归一化比例 [0,1]，供 UI 遮罩 fillAmount 使用</summary>
    public float GetSkillAbilityCoolDownRatio(int index)
    {
        AbilityConfig ability = GetSkillAbility(index);
        if (ability == null || ability.CoolDown <= 0) return 0;
        return GetCoolDownRemaining(ability.Id) / ability.CoolDown;
    }

    // ---------------- 主循环与输入 ----------------

    private void Update()
    {
        StateMachine.Update();
        UpdateCoolDowns();

        if (CanAction)
            ProcessInput();
    }

    /// <summary>集中处理通用输入（热键 Ability → 攻击 → 闪避 → 移动）</summary>
    private void ProcessInput()
    {
        // 热键 Ability（1/2/3/4）优先
        if (CheckSkillAbilityInput())
            return;

        // 普通攻击
        if (InputManager.Instance.AttackPressed)
        {
            ActivateComboAttack();
            return;
        }

        // 闪避：有移动输入时转向该方向并前冲，无输入时后撤
        if (InputManager.Instance.DodgePressed)
        {
            Vector3 dir = GetInputDirection();
            if (dir != Vector3.zero)
            {
                RotateImmediate(dir);
                ActivateAbilityById(PlayerConfig.DodgeForwardId);
            }
            else
            {
                ActivateAbilityById(PlayerConfig.DodgeBackwardId);
            }
            return;
        }

        // 移动 → 跑步（已在跑步中则跳过，避免重置 RunStage）
        if (InputManager.Instance.CheckMoveInput() && StateMachine.CurrentState is not RunState)
        {
            SwitchState(PlayerState.Run);
        }
    }

    /// <summary>检测技能 Ability 输入，返回是否触发</summary>
    private bool CheckSkillAbilityInput()
    {
        var ids = PlayerConfig?.SkillAbilityIds;
        if (ids == null) return false;

        for (int i = 0; i < ids.Count; i++)
        {
            if (InputManager.Instance.SkillPressed(i))
                return ActivateAbilityById(ids[i]);
        }
        return false;
    }

    private void LoadPlayerConfig()
    {
        string path = Path.Combine(Application.dataPath, "Config", "1001.json");
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            PlayerConfig = JsonConvert.DeserializeObject<PlayerConfig>(json);
        }
        else
        {
            Debug.LogWarning($"配置文件不存在: {path}");
        }
    }

    public void SwitchState(PlayerState state)
    {
        StateMachine.SwitchState(state);
    }

    // ---------------- 输入方向与旋转 ----------------

    /// <summary>获取键盘输入对应的世界空间方向（基于摄像机朝向）</summary>
    public Vector3 GetInputDirection()
    {
        Vector2 move = InputManager.Instance.MoveInput;
        if (move.magnitude < 0.1f) return Vector3.zero;
        return (move.y * CameraController.Instance.GetForwardVector()
               + move.x * CameraController.Instance.GetRightVector()).normalized;
    }

    /// <summary>立即旋转角色面向目标方向</summary>
    public void RotateImmediate(Vector3 targetDirection)
    {
        if (targetDirection.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.LookRotation(targetDirection);
    }
}
