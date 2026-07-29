using UnityEngine;

public class CameraController : SingletonMonoBehaviour<CameraController>
{
    public float Sensitivity = 100;

    private Vector2 LookDirection;

    // ================ 视角自动转向 ================

    private float _lastLookTime;
    private float _autoRotateTargetYaw;
    private const float AutoRotateDelay = 2f;
    private const float AutoRotateSpeed = 30f;
    private PlayerController _player;

    private void Start()
    {
        LookDirection = Vector2.zero;
        _player = FindObjectOfType<PlayerController>();
    }

    private void Update()
    {
        Vector2 lookInput = InputManager.Instance.LookInput;

        // 鼠标/右摇杆活跃时重置空闲计时并取消自动旋转
        if (lookInput.sqrMagnitude > 0.01f)
        {
            _lastLookTime = Time.time;
        }

        // 正常鼠标/摇杆控制
        LookDirection += Sensitivity * Time.deltaTime * lookInput;

        // 执行自动旋转
        if (Time.time - _lastLookTime >= AutoRotateDelay && _player != null)
        {
            Vector3 fwd = _player.transform.forward;
            _autoRotateTargetYaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
            LookDirection.x = Mathf.MoveTowardsAngle(LookDirection.x, _autoRotateTargetYaw, AutoRotateSpeed * Time.deltaTime);
        }

        // 钳制俯仰并应用旋转
        LookDirection.y = Mathf.Clamp(LookDirection.y, -75, 75);
        transform.rotation = Quaternion.Euler(-LookDirection.y, LookDirection.x, 0);
    }

    public Vector3 GetForwardVector()
    {
        Vector3 forward = transform.forward;
        forward.y = 0;
        return forward.normalized;
    }

    public Vector3 GetRightVector()
    {
        Vector3 right = transform.right;
        right.y = 0;
        return right.normalized;
    }
}
