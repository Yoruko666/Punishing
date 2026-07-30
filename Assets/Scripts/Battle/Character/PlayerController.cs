using UnityEngine;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;

public class PlayerController : CharacterBase, IDamageable
{
    public StateMachine StateMachine;
    public CharacterController CharacterController;

    public PlayerConfig PlayerConfig;

    public float Speed = 4.5f;
    public bool CanAction = true;
    public int ComboIndex = 0;

    /// <summary>待消费的信号球消数（1/2/3），通过 ConsumePendingMatchCount() 一次性读取并清空。</summary>
    private int _pendingMatchCount;
    /// <summary>读取并清空待消费的消球数。非消球技能（普攻/闪避/大招）永远读到 0。</summary>
    public int ConsumePendingMatchCount()
    {
        int val = _pendingMatchCount;
        _pendingMatchCount = 0;
        return val;
    }

    /// <summary>无敌标志，由 InvincibleEffect 控制，供受击/伤害系统查询</summary>
    public bool IsInvincible = false;

    /// <summary>当前待释放/正在释放的 Ability，由 AbilityState 在 OnEnter 读取</summary>
    public AbilityConfig PendingAbility { get; private set; }

    /// <summary>角色专属模块（如 LuciaModule），没有则为 null</summary>
    public CharacterModule Module { get; private set; }

    // ================ 信号球系统 ================

    public enum SignalOrbType { Red, Yellow, Blue, White }

    /// <summary>信号球数据：Id 为唯一身份（同色球也彼此不同），Type 为颜色。</summary>
    public struct SignalOrb
    {
        public int Id;
        public SignalOrbType Type;
    }

    private readonly List<SignalOrb> _signalOrbs = new(16);
    private readonly (int, int)[] _SignalOrbGroup = new (int, int)[MaxSignalOrbs];
    private const int MaxSignalOrbs = 16;
    /// <summary>UI 只显示该数量个信号球，键盘也只响应 1-8 键</summary>
    private const int MaxVisibleSignalOrbs = 8;

    /// <summary>自增计数器，为每颗新球分配唯一 Id。</summary>
    private int _nextOrbId;

    /// <summary>获得一颗球：参数为新球（含 Id），供视图层追加视图。</summary>
    public event Action<SignalOrb> OnOrbAdded;

    /// <summary>消除一批球：参数为(起始列表下标, 数量)，与 RemoveRange 一致，供视图层删除对应视图。</summary>
    public event Action<int, int> OnOrbsRemoved;

    /// <summary>信号球全量刷新（如 Blade Will 转球/退出），视图层应销毁全部并重建。</summary>
    public event Action OnOrbsReset;

    /// <summary>连击计数与窗口计时，用于信号球生成</summary>
    private int _comboAttackCount;
    private float _comboAttackTimer;
    private const float ComboAttackWindow = 1.5f;
    private const int MinAttacksForOrb = 2;

    /// <summary>信号球对应技能 ID（红/黄/蓝 → Skill1/Skill2/Skill3）</summary>
    private static readonly string[] OrbSkillIds = { "Skill1", "Skill2", "Skill3" };

    /// <summary>信号球贴图缓存（SignalOrbType 枚举索引 → Addressables key），启动时从 PlayerConfig.OrbSprites 填充。</summary>
    private readonly string[] _orbSprites = new string[4];

    private readonly Dictionary<string, AbilityConfig> _abilityMap = new();
    private readonly AttributeSet _attributeSet = new();

    protected override void Start()
    {
        base.Start();

        LoadPlayerConfig();
        // 缓存信号球贴图
        var orbDict = PlayerConfig?.OrbSprites;
        if (orbDict != null)
        {
            for (int i = 0; i < 4; i++)
            {
                var type = (SignalOrbType)i;
                if (orbDict.TryGetValue(type.ToString(), out var spriteKey) && !string.IsNullOrEmpty(spriteKey))
                    _orbSprites[i] = spriteKey;
            }
        }
        BuildAbilityMap();

        // 自动发现挂载在本体上的角色专属模块
        Module = GetComponent<CharacterModule>();
        if (Module != null) Module.Initialize(this);

        InitAttributes();

        StateMachine = new StateMachine();
        StateMachine.RegisterState(PlayerState.Idle, new IdleState(this));
        StateMachine.RegisterState(PlayerState.Run, new RunState(this));
        StateMachine.RegisterState(PlayerState.Ability, new AbilityState(this));
        StateMachine.SwitchState(PlayerState.Idle);

        CharacterController = GetComponent<CharacterController>();
    }

    /// <summary>初始化基础属性（HP、MaxHP 等）</summary>
    private void InitAttributes()
    {
        _attributeSet.EnsureAttribute("MaxHP", 500f);
        _attributeSet.EnsureAttribute("HP", _attributeSet.GetAttribute("MaxHP"));
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

    // 更新信号球分组（白色球仅能单消，红/黄/蓝最多三消）
    private void UpdateSignalOrbGroup()
    {
        for (int i = 0; i < _signalOrbs.Count; i++)
            _SignalOrbGroup[i] = (-1, -1);
        for (int i = 0; i < _signalOrbs.Count; i++)
        {
            int groupCount = 0;
            if (_SignalOrbGroup[i] != (-1, -1)) 
                continue;
            int maxGroup = _signalOrbs[i].Type == SignalOrbType.White ? 1 : 3;
            for(int j = 0; j < maxGroup; j++)
            {
                if (i + j >= _signalOrbs.Count || _signalOrbs[i].Type != _signalOrbs[i + j].Type)
                    break;
                groupCount++;
            }
            for(int j = 0; j < groupCount; j++)
                _SignalOrbGroup[i + j] = (i, groupCount);
        }
    }

    // 产生信号球（默认随机颜色，Module 可强制指定颜色）
    private void GenerateSignalOrb()
    {
        if (_signalOrbs.Count >= MaxSignalOrbs) return;

        SignalOrbType type;
        if (Module != null && Module.GetOrbOverride(out SignalOrbType overrideType))
            type = overrideType;
        else
            type = (SignalOrbType)UnityEngine.Random.Range(0, 3);

        var orb = new SignalOrb
        {
            Id = _nextOrbId++,
            Type = type,
        };
        _signalOrbs.Add(orb);
        UpdateSignalOrbGroup();
        OnOrbAdded?.Invoke(orb);
    }

    /// <summary>强制生成指定颜色的球（Blade Will 状态添加白球等）</summary>
    public void GenerateSignalOrb(SignalOrbType forcedType)
    {
        if (_signalOrbs.Count >= MaxSignalOrbs) return;
        var orb = new SignalOrb { Id = _nextOrbId++, Type = forcedType };
        _signalOrbs.Add(orb);
        UpdateSignalOrbGroup();
        OnOrbAdded?.Invoke(orb);
    }

    // 消除信号球（Module 可拦截并返回自定义 skillId）
    private int GetDataListIndex(int slotIndex)
    {
        // slotIndex: 0=键1(最右/编号8/最旧), MaxVisible-1=键8(最左/编号1/最新可见)
        // 可见窗口固定从列表索引 0 开始，9-16 为等候区不响应键位
        return  8 - slotIndex - 1;
    }

    public bool TryConsumeSignalOrb(int slotIndex)
    {
        if (!CanAction) return false;
        if (slotIndex < 0 || slotIndex >= MaxVisibleSignalOrbs) return false;
        int listIndex = GetDataListIndex(slotIndex);
        if (listIndex < 0 || listIndex >= _signalOrbs.Count) return false;

        int headIndex = _SignalOrbGroup[listIndex].Item1;
        int matchCount = _SignalOrbGroup[listIndex].Item2;
        SignalOrbType type = _signalOrbs[headIndex].Type;
        _signalOrbs.RemoveRange(headIndex, matchCount);
        OnOrbsRemoved?.Invoke(headIndex, matchCount);

        _pendingMatchCount = matchCount;
        string matchPrefix = matchCount == 3 ? "三消" : matchCount == 2 ? "二消" : "单消";
        UpdateSignalOrbGroup();

        // Module 可拦截消球行为（Blade Will 白球→SpSkill、三消蓝标记、三消后 Blade Will 入场等）
        if (Module != null && Module.TryOverrideOrbSkill(type, matchCount, out string overrideSkillId))
        {
            Debug.Log($"[信号球] {matchPrefix} 键{slotIndex + 1} ({type}, → {overrideSkillId})");
            return ActivateAbilityById(overrideSkillId);
        }

        string skillId = OrbSkillIds[(int)type];
        Debug.Log($"[信号球] {matchPrefix} 键{slotIndex + 1} ({type}, {skillId})");
        return ActivateAbilityById(skillId);
    }

    /// <summary>按数据列表索引直接消除（UI 等候区场景下绕过 slot 换算）。</summary>
    public bool TryConsumeSignalOrbByDataIndex(int listIndex)
    {
        if (!CanAction) return false;
        if (listIndex < 0 || listIndex >= _signalOrbs.Count) return false;

        int headIndex = _SignalOrbGroup[listIndex].Item1;
        int matchCount = _SignalOrbGroup[listIndex].Item2;
        SignalOrbType type = _signalOrbs[headIndex].Type;
        _signalOrbs.RemoveRange(headIndex, matchCount);
        OnOrbsRemoved?.Invoke(headIndex, matchCount);

        _pendingMatchCount = matchCount;
        string matchPrefix = matchCount == 3 ? "三消" : matchCount == 2 ? "二消" : "单消";
        UpdateSignalOrbGroup();

        if (Module != null && Module.TryOverrideOrbSkill(type, matchCount, out string overrideSkillId))
        {
            Debug.Log($"[信号球] {matchPrefix} (data[{listIndex}], {type}, → {overrideSkillId})");
            return ActivateAbilityById(overrideSkillId);
        }

        string skillId = OrbSkillIds[(int)type];
        Debug.Log($"[信号球] {matchPrefix} (data[{listIndex}], {type}, {skillId})");
        return ActivateAbilityById(skillId);
    }

    /// <summary>获取当前信号球列表（供 UI 使用），索引 0 = 最早（右侧/键 8）</summary>
    public List<SignalOrb> GetSignalOrbs() => _signalOrbs;

    /// <summary>获取指定信号球类型的贴图 Addressables key，由 PlayerConfig.OrbSprites 配置，启动时缓存。</summary>
    public string GetOrbSprite(SignalOrbType type) => _orbSprites[(int)type];

    /// <summary>获取信号球最大数量（UI 显示用，= 可见球数量）</summary>
    public int GetMaxSignalOrbs() => MaxVisibleSignalOrbs;

    /// <summary>获取信号球最大数据容量</summary>
    public int GetMaxDataSignalOrbs() => MaxSignalOrbs;

    // ---------------- 信号球底层操作 API（供 Module 使用） ----------------

    /// <summary>当前信号球总数。</summary>
    public int OrbCount => _signalOrbs.Count;

    /// <summary>读取指定数据索引位置的信号球颜色。</summary>
    public SignalOrbType GetOrbType(int dataIndex) => _signalOrbs[dataIndex].Type;

    /// <summary>修改指定数据索引位置的信号球颜色。</summary>
    public void SetOrbType(int dataIndex, SignalOrbType newType)
    {
        var orb = _signalOrbs[dataIndex];
        orb.Type = newType;
        _signalOrbs[dataIndex] = orb;
    }

    /// <summary>清空所有信号球（触发 OnOrbsReset），供 Module 全量重建使用。</summary>
    public void ClearOrbs()
    {
        _signalOrbs.Clear();
        OnOrbsReset?.Invoke();
    }

    /// <summary>通知 UI 全量刷新（Module 批量修改球颜色后调用）。</summary>
    public void NotifyOrbsReset() => OnOrbsReset?.Invoke();

    /// <summary>重建信号球分组缓存（Module 批量操作后调用）。</summary>
    public void RebuildOrbGroups() => UpdateSignalOrbGroup();

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

    // ---------------- 属性与受击（AttributeSet / IDamageable） ----------------

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

    /// <summary>实现 IDamageable：受到伤害。由敌方 DamageEffect 调用。</summary>
    public void TakeDamage(float amount)
    {
        if (IsInvincible || amount <= 0f) return;

        float hp = _attributeSet.GetAttribute("HP");
        hp = Mathf.Max(0f, hp - amount);
        _attributeSet.SetBaseAttribute("HP", hp);

        float maxHp = _attributeSet.GetAttribute("MaxHP");
        Debug.Log($"{name} 受到 {amount} 点伤害，剩余 HP {hp}/{maxHp}");
    }

    // ---------------- 主循环与输入 ----------------

    private void Update()
    {
        StateMachine.Update();
        UpdateComboWindow();

        // 信号球激活：任何状态下均可使用（按键 1~8）
        for (int i = 0; i < MaxVisibleSignalOrbs; i++)
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

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (PendingAbility?.AbilityEffects == null) return;

        foreach (var effect in PendingAbility.AbilityEffects)
        {
            if (effect is DamageEffect damage && damage.DetectionShape != null && damage.LastExecutedTime > 0f
                && Time.time - damage.LastExecutedTime <= 0.1f)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
                Vector3 worldPos = transform.TransformPoint(damage.DetectionShape.Offset);

                if (damage.DetectionShape is SphereDetection sphere)
                {
                    Gizmos.DrawSphere(worldPos, sphere.Radius);
                }
                else if (damage.DetectionShape is BoxDetection box)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawCube(damage.DetectionShape.Offset, box.HalfExtents * 2f);
                    Gizmos.matrix = Matrix4x4.identity;
                }
            }
        }
    }
#endif
}
