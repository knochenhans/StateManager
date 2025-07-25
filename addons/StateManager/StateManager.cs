using Godot;
using Godot.Collections;
using System;

public partial class StateManager<[MustBeVariant] TState> : Node where TState : notnull
{
    private Dictionary<TState, State<TState>> states = [];
    private Timer stateTimer;
    private TState pendingNextState = default;
    private bool hasPendingNextState = false;

    public StateManager()
    {
        stateTimer = new Timer
        {
            OneShot = true
        };
        stateTimer.Timeout += OnStateTimerTimeout;
        AddChild(stateTimer);
        // GD.Print("StateManager initialized.");
    }

    public void AddState(TState name, State<TState> state) => states[name] = state;

    public void SetState(TState name, State<TState> state)
    {
        if (states.ContainsKey(name))
            states[name] = state;
        else
            AddState(name, state);
    }

    public State<TState> GetState(TState name) => states.TryGetValue(name, out State<TState> value) ? value : null;

    public void RemoveState(TState name) => states.Remove(name);

    public void EnterState(TState name)
    {
        // GD.Print($"Entering state: {name}");
        var state = GetState(name);
        if (state == null)
            return;

        state.Enter();

        if (state.Duration > 0.0f)
        {
            if (state.DurationIsRandom)
                state.Duration = (float)GD.RandRange(0.1, state.Duration);
            stateTimer.WaitTime = state.Duration;
            pendingNextState = (TState)Convert.ChangeType(state.NextStateName, typeof(TState));
            hasPendingNextState = true;
            stateTimer.Start();
        }
        else
        {
            hasPendingNextState = false;

            if (state.NextStateName != null)
                EnterState((TState)Convert.ChangeType(state.NextStateName, typeof(TState)));
        }
    }

    private void OnStateTimerTimeout()
    {
        if (hasPendingNextState)
            EnterState(pendingNextState);
    }
}
