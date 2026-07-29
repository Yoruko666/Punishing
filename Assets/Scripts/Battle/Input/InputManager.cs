using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 基于 New Input System 的集中式输入管理。
/// 以代码方式定义 Action 与绑定（键鼠 + 手柄），对外只暴露语义化的输入查询，
/// 其它系统一律通过本类读取输入，避免直接依赖具体设备或旧版 Input API。
///
/// - 持续值：MoveInput / LookInput
/// - 帧触发：AttackPressed / DodgePressed / UltimatePressed / OrbActivatePressed(index)
/// </summary>
public class InputManager : SingletonMonoBehaviour<InputManager>
{
    // ---------------- 对外接口 ----------------

    /// <summary>移动输入（WASD / 左摇杆），归一化的 2D 向量</summary>
    public Vector2 MoveInput => _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;

    /// <summary>视角输入（鼠标位移 / 右摇杆），供 CameraController 使用</summary>
    public Vector2 LookInput => _look != null ? _look.ReadValue<Vector2>() : Vector2.zero;

    /// <summary>本帧是否按下攻击键（鼠标左键 / 手柄西键）</summary>
    public bool AttackPressed => _attack != null && _attack.WasPressedThisFrame();

    /// <summary>本帧是否按下闪避键（LeftShift / 手柄东键）</summary>
    public bool DodgePressed => _dodge != null && _dodge.WasPressedThisFrame();

    /// <summary>本帧是否按下终极技键（Q / 手柄北键）</summary>
    public bool UltimatePressed => _ultimate != null && _ultimate.WasPressedThisFrame();

    /// <summary>本帧是否按下第 index 个信号球激活键（0~7 对应按键 1~8）</summary>
    public bool OrbActivatePressed(int index)
    {
        if (_orbKeys == null || index < 0 || index >= _orbKeys.Length) return false;
        return _orbKeys[index].WasPressedThisFrame();
    }

    /// <summary>是否存在有效移动输入（用于跑步状态判定）</summary>
    public bool CheckMoveInput() => MoveInput.magnitude > 0.1f;

    // ---------------- Action 定义 ----------------

    private InputActionMap _map;
    private InputAction _move;
    private InputAction _look;
    private InputAction _attack;
    private InputAction _dodge;
    private InputAction _ultimate;
    private InputAction[] _orbKeys;

    protected override void OnAwake()
    {
        BuildActions();
        _map.Enable();
    }

    private void OnDestroy()
    {
        _map?.Disable();
        _map?.Dispose();
    }

    /// <summary>以代码构建全部 Action 与设备绑定（键鼠 + 手柄）</summary>
    private void BuildActions()
    {
        _map = new InputActionMap("Player");

        // 移动：WASD / 手柄左摇杆
        _move = _map.AddAction("Move", InputActionType.Value);
        _move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        _move.AddBinding("<Gamepad>/leftStick");

        // 视角：鼠标位移 / 手柄右摇杆
        // 注意：鼠标 delta 为像素位移，手柄摇杆为 [-1,1]，两者尺度不同，
        // 灵敏度差异由 CameraController 的 Sensitivity 统一调节。
        _look = _map.AddAction("Look", InputActionType.Value);
        _look.AddBinding("<Mouse>/delta");
        _look.AddBinding("<Gamepad>/rightStick");

        // 攻击：G键 / 手柄西键（X / □）
        _attack = _map.AddAction("Attack", InputActionType.Button);
        _attack.AddBinding("<Keyboard>/g");
        _attack.AddBinding("<Gamepad>/buttonWest");

        // 闪避：LeftShift / 手柄东键（B / ○）
        _dodge = _map.AddAction("Dodge", InputActionType.Button);
        _dodge.AddBinding("<Keyboard>/leftShift");
        _dodge.AddBinding("<Gamepad>/buttonEast");

        // 终极技：Q / 手柄北键（Y / △）
        _ultimate = _map.AddAction("Ultimate", InputActionType.Button);
        _ultimate.AddBinding("<Keyboard>/q");
        _ultimate.AddBinding("<Gamepad>/buttonNorth");

        // 信号球激活键：数字键 1~8（位置从左到右 1~8，右端为 8）
        string[] orbKeys = { "1", "2", "3", "4", "5", "6", "7", "8" };
        _orbKeys = new InputAction[orbKeys.Length];
        for (int i = 0; i < orbKeys.Length; i++)
        {
            InputAction orbAction = _map.AddAction($"Orb{i + 1}", InputActionType.Button);
            orbAction.AddBinding($"<Keyboard>/{orbKeys[i]}");
            _orbKeys[i] = orbAction;
        }
    }
}
