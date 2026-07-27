using UnityEngine;
using Newtonsoft.Json;
using System.IO;

public class PlayerController : CharacterBase
{
    public StateMachine StateMachine;
    public CharacterController CharacterController;

    public PlayerConfig PlayerConfig;

    public float Speed = 4.5f;
    public bool CanAction = true;
    public int ComboIndex = 0;

    // 技能冷却计时器数组（秒），与 SkillList 索引对应
    private float[] skillCoolDowns;
    public int SkillIndex { get; set; }

    public float GetSkillCoolDown(int index) => index >= 0 && index < skillCoolDowns?.Length ? skillCoolDowns[index] : 0;

    protected override void Start()
    {
        base.Start();

        LoadPlayerConfig();

        // 初始化冷却数组
        if (PlayerConfig.SkillList != null)
            skillCoolDowns = new float[PlayerConfig.SkillList.Count];

        StateMachine = new StateMachine(this);
        StateMachine.RegisterState(PlayerState.Idle, new IdleState(this));
        StateMachine.RegisterState(PlayerState.Run, new RunState(this));
        StateMachine.RegisterState(PlayerState.DashForward, new DashState(this, DashState.DashDirection.Forward));
        StateMachine.RegisterState(PlayerState.DashBackward, new DashState(this, DashState.DashDirection.Backward));
        StateMachine.RegisterState(PlayerState.Attack, new AttackState(this));
        StateMachine.RegisterState(PlayerState.Skill, new SkillState(this));
        StateMachine.SwitchState(PlayerState.Idle);

        CharacterController = GetComponent<CharacterController>();
    }

    /// <summary>尝试释放指定索引的技能（含冷却检查）</summary>
    public bool TryActivateSkill(int index)
    {
        if (skillCoolDowns == null || index < 0 || index >= skillCoolDowns.Length)
            return false;

        if (skillCoolDowns[index] > 0)
            return false; // 冷却中

        SkillIndex = index;
        SwitchState(PlayerState.Skill);
        return true;
    }

    public void StartSkillCoolDown(int index)
    {
        if (index >= 0 && index < skillCoolDowns?.Length)
            skillCoolDowns[index] = PlayerConfig.SkillList[index].CoolDown;
    }

    /// <summary>检测技能快捷键输入，返回是否触发了技能</summary>
    public bool CheckSkillInput()
    {
        if (skillCoolDowns == null)
            return false;

        for (int i = 0; i < skillCoolDowns.Length; i++)
        {
            KeyCode key = i switch
            {
                0 => KeyCode.Alpha1,
                1 => KeyCode.Alpha2,
                2 => KeyCode.Alpha3,
                3 => KeyCode.Alpha4,
                _ => KeyCode.None
            };
            if (key != KeyCode.None && Input.GetKeyDown(key))
                return TryActivateSkill(i);
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

    private void Update()
    {
        StateMachine.Update();
        UpdateCoolDowns();

        if (CanAction)
            ProcessInput();
    }

    /// <summary>集中处理通用输入（移动↔跑步、攻击、闪避、技能）</summary>
    private void ProcessInput()
    {
        // 技能快捷键优先
        if (CheckSkillInput())
            return;

        // 攻击
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            SwitchState(PlayerState.Attack);
            return;
        }

        // 闪避（根据是否有移动输入决定方向）
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            SwitchState(InputManager.Instance.CheckMoveInput() ? PlayerState.DashForward : PlayerState.DashBackward);
            return;
        }

        // 移动 → 跑步（已在跑步中则跳过，避免重置 RunStage）
        if (InputManager.Instance.CheckMoveInput() && StateMachine.CurrentState is not RunState)
        {
            SwitchState(PlayerState.Run);
        }
    }

    private void UpdateCoolDowns()
    {
        if (skillCoolDowns == null)
            return;

        float dt = Time.deltaTime;
        for (int i = 0; i < skillCoolDowns.Length; i++)
        {
            if (skillCoolDowns[i] > 0)
                skillCoolDowns[i] -= dt;
        }
    }

    public void SwitchState(PlayerState state)
    {
        StateMachine.SwitchState(state);
    }
}
