using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : SingletonMonoBehaviour<InputManager>
{
    public Vector2 MoveInput;
    public Vector2 LookInput;

    private InputActions _inputActions;

    protected override void OnAwake()
    {
        _inputActions = new InputActions();
    }

    private void Update()
    {
        MoveInput = _inputActions.Player.Move.ReadValue<Vector2>();
        LookInput = _inputActions.Player.Look.ReadValue<Vector2>();
    }

    public bool CheckMoveInput() => MoveInput.magnitude > 0.1f;

    private void OnEnable()
    {
        _inputActions.Enable();
    }

    private void OnDisable()
    {
        _inputActions.Disable();
    }
}
