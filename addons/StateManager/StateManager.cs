using Godot;
using System;
using System.Collections.Generic;

public partial class StateManager<[MustBeVariant] TState> where TState : notnull
{
    public event Action<bool> StateChanged;

    private Dictionary<TState, State<TState>> states = [];

    private double stateTimeRemaining = 0.0f;
    private double preActivationTimeRemaining = 0.0f;

    private TState pendingNextState = default;
    private bool hasPendingNextState = false;
    private TState pendingStateToActivate = default;
    private bool hasPendingPreActivation = false;
    private TState currentStateName = default;

    public void Update(double delta)
    {
        if (hasPendingPreActivation)
        {
            preActivationTimeRemaining -= delta;
            if (preActivationTimeRemaining <= 0.0)
                OnPreActivationTimeout();
        }

        if (hasPendingNextState)
        {
            stateTimeRemaining -= delta;
            if (stateTimeRemaining <= 0.0)
                OnStateTimerTimeout();
        }
    }

    public void AddState(TState name, State<TState> state) => states[name] = state;

    public void SetState(TState name, State<TState> state)
    {
        if (states.ContainsKey(name))
            states[name] = state;
        else
            AddState(name, state);
    }

    public void SetDefaultState(TState name)
    {
        if (states.ContainsKey(name))
            currentStateName = name;
    }

    public State<TState> GetState(TState name) => states.TryGetValue(name, out State<TState> value) ? value : null;

    public void RemoveState(TState name) => states.Remove(name);

    public void EnterState(TState name, bool silent = false)
    {
        var state = GetState(name);
        if (state == null)
            return;

        if (name.Equals(currentStateName) && !silent)
            return;

        // Stop any running timers / clear pending transitions before starting a new enter
        stateTimeRemaining = 0.0;
        hasPendingNextState = false;
        preActivationTimeRemaining = 0.0;
        hasPendingPreActivation = false;

        // If the state defines a pre-activation wait, start the pre-activation countdown
        if (state.PreActivationBaseTime + state.PreActivationRandomTimeMin + state.PreActivationRandomTimeMax > 0.0)
        {
            pendingStateToActivate = name;
            hasPendingPreActivation = true;
            preActivationTimeRemaining = state.PreActivationBaseTime + GD.RandRange(state.PreActivationRandomTimeMin, state.PreActivationRandomTimeMax);
            return;
        }

        // Otherwise immediately activate
        ActivateState(name, silent);
    }

    // Activate a state (enter, start duration timer, handle immediate next)
    private void ActivateState(TState name, bool silent = false)
    {
        var state = GetState(name);
        if (state == null)
            return;

        state.Enter(silent);
        currentStateName = name;
        StateChanged?.Invoke(silent);

        if (state.Duration > 0.0)
        {
            if (state.DurationIsRandom)
                state.Duration = GD.RandRange(0.1, state.Duration);

            stateTimeRemaining = state.Duration;
            pendingNextState = (TState)Convert.ChangeType(state.NextStateName, typeof(TState));
            hasPendingNextState = true;
        }
        else
        {
            hasPendingNextState = false;

            if (!state.NextStateName.Equals(currentStateName))
                EnterState((TState)Convert.ChangeType(state.NextStateName, typeof(TState)));
        }
    }

    private void OnPreActivationTimeout()
    {
        if (hasPendingPreActivation)
        {
            hasPendingPreActivation = false;
            var toActivate = pendingStateToActivate;
            ActivateState(toActivate);
        }
    }

    private void OnStateTimerTimeout()
    {
        if (hasPendingNextState)
        {
            hasPendingNextState = false;
            EnterState(pendingNextState);
        }
    }

    public TState GetCurrentStateName() => currentStateName;
}