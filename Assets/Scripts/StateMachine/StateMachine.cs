using System.Collections.Generic;

public class StateMachine
{
    public StateBase CurrentState;
    public Dictionary<PlayerState, StateBase> States = new();

    public void RegisterState(PlayerState playerState, StateBase state)
    {
        States.Add(playerState, state);
    }

    public void SwitchState(PlayerState state)
    {
        CurrentState?.OnExit();
        CurrentState = States[state];
        CurrentState.OnEnter();
    }

    public void Update()
    {
        CurrentState?.OnUpdate();
    }
}
