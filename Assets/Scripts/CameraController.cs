using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : SingletonMonoBehaviour<CameraController>
{
    public float Sensitivity = 100;

    private Vector2 LookDirection;

    private void Start()
    {
        LookDirection = Vector2.zero;
    }

    private void Update()
    {
        LookDirection += Sensitivity * Time.deltaTime * InputManager.Instance.LookInput;
        LookDirection.y = Mathf.Clamp(LookDirection.y, - 75, 75);
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
