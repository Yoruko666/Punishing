using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;

public class PlayerController : CharacterBase
{
    public StateMachine StateMachine;
    public CharacterController CharacterController;

    public PlayerConfig PlayerConfig;

    public float Speed = 4.5f;
    public bool CanAction = true;
    public int ComboIndex = 0;

    /// <summary>当前信号球消数（1/2/3），供技能系统读取</summary>
    public int CurrentMatchCount { get; set; } = 1;

    /// <summary>无敌标志，由 InvincibleEffect 控制，供受击/伤害系统查询</summary>
    public bool IsInvincible = false;

    /// <summary>当前待释放/正在释放的 Ability，由 AbilityState 在 OnEnter 读取</summary>
    public AbilityConfig PendingAbility { get; private set; }

    /// <summary>角色专属模块（如 LuciaModule），没有则为 null</summary>
    public CharacterModule Module { get; private set; }

    // ================ 信号球系统 ================

    public enum SignalOrbType { Red, Yellow, Blue }

    private readonly List<SignalOrbType> _signalOrbs = new(8);
    private readonly (int, int)[] _SignalOrbGroup = new (int, int)[MaxSignalOrbs];
    private const int MaxSignalOrbs = 8;

    /// <summary>连击计数与窗口计时，用于信号球生成</summary>
    private int _comboAttackCount;
    private float _comboAttackTimer;
    private const float ComboAttackWindow = 1.5f;
    private const int MinAttacksForOrb = 2;

    /// <summary>信号球对应技能 ID（红/黄/蓝 → Skill1/Skill2/Skill3）</summary>
    private static readonly string[] OrbSkillIds = { "Skill1", "Skill2", "Skill3" };

    /// <summary>信号球颜色，供 UI 使用</summary>
    public static readonly Color[] OrbColors =
    {
        new Color(1f, 0.27f, 0.27f), // Red
        new Color(1f, 0.84f, 0f),    // Yellow
        new Color(0.27f, 0.53f, 1f)  // Blue
    };

    private readonly Dictionary<string, AbilityConfig> _abilityMap = new();
    private readonly AttributeSet _attributeSet = new();

    protected override void Start()
    {
        base.Start();

        LoadPlayerConfig();
        BuildAbilityMap();

        // 自动发现挂载在本体上的角色专属模块
        Module = GetComponent<CharacterModule>();
        if (Module != null) Module.Initialize(this);

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

    /// <summary>按 Id 释放 Ability（不含冷却检查，信号球系统代替了冷却）</summary>
    public bool ActivateAbilityById(string id)
    {
        AbilityConfig ability = GetAbility(id);
        if (ability == null) return false;

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

        // 记录连击用于信号球生成
        OnAttackPerformed();

        return ActivateAbilityById(combo[idx]);
    }

    /// <summary>打断并重置普攻连招链。任何"非连招攻击"的动作（移动、闪避、技能、自然中断）都应调用</summary>
    public void ResetCombo()
    {
        ComboIndex = 0;
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

    // ---------------- 信号球系统 ----------------

    // 每次释放普攻时调用，跟踪连击窗口以生成信号球
    private void OnAttackPerformed()
    {
        _comboAttackCount++;
        _comboAttackTimer = 0f;

        if (_comboAttackCount >= MinAttacksForOrb && _signalOrbs.Count < MaxSignalOrbs)
        {
            GenerateSignalOrb();
            _comboAttackCount = 0;
        }
    }

    // 更新信号球分组
    private void UpdateSignalOrbGroup()
    {
        for (int i = 0; i < _signalOrbs.Count; i++)
            _SignalOrbGroup[i] = (-1, -1);
        for (int i = 0; i < _signalOrbs.Count; i++)
        {
            int groupCount = 0;
            if (_SignalOrbGroup[i] != (-1, -1)) 
                continue;
            for(int j = 0; j < 3; j++)
            {
                if (i + j >= _signalOrbs.Count || _signalOrbs[i] != _signalOrbs[i + j])
                    break;
                groupCount++;
            }
            for(int j = 0; j < groupCount; j++)
                _SignalOrbGroup[i + j] = (i, groupCount);
        }
    }

    // 产生信号球
    private void GenerateSignalOrb()
    {
        if (_signalOrbs.Count >= MaxSignalOrbs) return;
        SignalOrbType type = (SignalOrbType)Random.Range(0, 3);
        _signalOrbs.Add(type); 
        UpdateSignalOrbGroup();
    }

    // 消除信号球
    public bool TryConsumeSignalOrb(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MaxSignalOrbs) return false;
        int listIndex = MaxSignalOrbs - 1 - slotIndex;
        if (listIndex < 0 || listIndex >= _signalOrbs.Count) return false;

        SignalOrbType type = _signalOrbs[listIndex];

        int headIndex = _SignalOrbGroup[listIndex].Item1;
        int matchCount = _SignalOrbGroup[listIndex].Item2;
        _signalOrbs.RemoveRange(headIndex, matchCount);

        CurrentMatchCount = matchCount;
        string matchPrefix = matchCount == 3 ? "三消" : matchCount == 2 ? "二消" : "单消";
        string skillId = OrbSkillIds[(int)type];
        Debug.Log($"[信号球] {matchPrefix} 键{slotIndex + 1} ({type}, {skillId})");
        UpdateSignalOrbGroup();
        return ActivateAbilityById(skillId);
    }

    /// <summary>获取当前信号球列表（供 UI 使用），索引 0 = 最早（右侧/键 8）</summary>
    public List<SignalOrbType> GetSignalOrbs() => _signalOrbs;

    /// <summary>获取信号球最大数量</summary>
    public int GetMaxSignalOrbs() => MaxSignalOrbs;

    /// <summary>信号球列表索引 → 视觉位置（0=左/键1, 7=右/键8）</summary>
    public int ListIndexToSlot(int listIndex) => MaxSignalOrbs - 1 - listIndex;

    /// <summary>信号球生成窗口跟踪，每帧更新超时重置</summary>
    private void UpdateComboWindow()
    {
        if (_comboAttackCount > 0)
        {
            _comboAttackTimer += Time.deltaTime;
            if (_comboAttackTimer >= ComboAttackWindow)
            {
                _comboAttackCount = 0;
                _comboAttackTimer = 0f;
            }
        }
    }

    // ---------------- 属性（AttributeSet） ----------------

    /// <summary>
    /// 通用属性修改入口，供 ModifyAttributeEffect 调用。
    /// 钳制/特殊逻辑委托给 Module.ApplyAttributeClamp。
    /// </summary>
    public void ModifyAttribute(string attributeName, float delta)
    {
        _attributeSet.EnsureAttribute(attributeName);
        float newVal = _attributeSet.GetAttribute(attributeName) + delta;
        newVal = Module != null ? Module.ApplyAttributeClamp(attributeName, newVal) : newVal;
        _attributeSet.SetBaseAttribute(attributeName, newVal);
    }

    /// <summary>获取任意属性的当前值</summary>
    public float GetAttribute(string attributeName) => _attributeSet.GetAttribute(attributeName);

    // ---------------- 主循环与输入 ----------------

    private void Update()
    {
        StateMachine.Update();
        UpdateComboWindow();

        // 信号球激活：任何状态下均可使用（按键 1~8 → 位置 0~7）
        for (int i = 0; i < MaxSignalOrbs; i++)
        {
            if (InputManager.Instance.OrbActivatePressed(i))
            {
                TryConsumeSignalOrb(i);
            }
        }

        if (CanAction)
            ProcessInput();
    }

    /// <summary>
    /// 集中处理通用输入（终极技 → 攻击 → 闪避 → 移动）。
    /// 信号球激活在 Update 中统一处理，不受 CanAction 限制。
    /// 旧版技能热键 1/2/3/4 已全部移除，改为信号球系统。
    /// </summary>
    private void ProcessInput()
    {
        // 终极技（Q 键）优先于一切
        if (InputManager.Instance.UltimatePressed)
        {
            var ultimateId = PlayerConfig?.UltimateAbilityId;
            if (!string.IsNullOrEmpty(ultimateId))
                ActivateAbilityById(ultimateId);
            return;
        }

        // 普通攻击 —— 唯一沿用 / 推进连招索引的入口
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

    // ================ Module 钩子兼容（保留接口，被移除的 SpSkill 不受影响） ================

    /// <summary>Ability 进行中每帧的 Module 预输入处理（已弃用，保留空调用）</summary>
    public void NotifyModuleAbilityUpdate(float timer, float exitTime)
    {
        if (Module != null) Module.OnAbilityUpdate(timer, exitTime);
    }

    /// <summary>ExitTime 尝试激活 Module 的缓冲技能（已弃用，始终返回 false）</summary>
    public bool TryConsumeModuleBufferedSkill() => false;

    /// <summary>ExitTime 无预输入时通知 Module 重置专属状态（已弃用）</summary>
    public void NotifyModuleAbilityEnd()
    {
        if (Module != null) Module.OnAbilityExitNoBuffer();
    }

    /// <summary>旧版热键 4 输入交由 Module 处理（已弃用，始终返回 false）</summary>
    public bool TryHandleModuleSkillKey(int index) => false;
}
