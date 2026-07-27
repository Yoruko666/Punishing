using System.Collections.Generic;

public class StateMachine
{
    public StateBase CurrentState;
    public Dictionary<PlayerState, StateBase> States = new();

    private PlayerController Owner;

    public StateMachine(PlayerController owner)
    {
        Owner = owner;
    }

    public void RegisterState(PlayerState playerState, StateBase state)
    {
        States.Add(playerState, state);
    }

    public void SwitchState(PlayerState state)
    {
        CurrentState?.OnExit();
        CurrentState = States[state];
        if (CurrentState is not AttackState)
            Owner.ComboIndex = 0;
        CurrentState.OnEnter();
    }

    public void Update()
    {
        CurrentState?.OnUpdate();
    }
}