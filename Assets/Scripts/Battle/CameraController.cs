using Cinemachine;
using UnityEngine;

public class CameraController : SingletonMonoBehaviour<CameraController>
{
    private const float sensitivity = 20;

    private Vector2 LookDirection;

    // 自动转向
    private float _lastLookTime;
    private float _autoRotateTargetYaw;
    private const float AutoRotateDelay = 2f;
    private const float AutoRotateSpeed = 30f;
    private PlayerController _player;
    private CinemachineVirtualCamera _cinemachineVirtualCamera;

    private void Start()
    {
        EventCenter.AddListener<Transform>(EventType.OnCharacterSwitch, BindCharacter);
        LookDirection = Vector2.zero;
        _cinemachineVirtualCamera = GetComponent<CinemachineVirtualCamera>();
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
        LookDirection += sensitivity * Time.deltaTime * lookInput;

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

    public void BindCharacter(Transform character)
    {
        Transform cameraReference = FindFirstChildRecursive(character, "CameraReference");
        _cinemachineVirtualCamera.Follow = cameraReference;
        _cinemachineVirtualCamera.LookAt = cameraReference;
        _player = character.GetComponent<PlayerController>();
    }

    private static Transform FindFirstChildRecursive(Transform parent, string name)
    {
        if (parent.name == name)
            return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindFirstChildRecursive(parent.GetChild(i), name);
            if (result != null)
                return result;
        }
        return null;
    }
}
