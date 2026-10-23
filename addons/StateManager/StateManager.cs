using Godot;
using System;
using System.Collections.Generic;

public partial class StateManager<[MustBeVariant] TState> where TState : notnull
{
    public event Action<TState, TState, bool> StateChanged;

    Dictionary<TState, State<TState>> states = [];

    double stateTimeRemaining = 0.0f;
    double preActivationTimeRemaining = 0.0f;

    TState pendingNextState = default;
    bool hasPendingNextState = false;
    TState pendingStateToActivate = default;
    bool hasPendingPreActivation = false;
    TState currentStateName = default;

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

        var oldStateName = currentStateName;

        state.Enter(silent);
        currentStateName = name;
        StateChanged?.Invoke(oldStateName, name, silent);

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

    public SaveData Save()
    {
        var data = new SaveData
        {
            [nameof(currentStateName)] = currentStateName.ToString(),
            [nameof(stateTimeRemaining)] = stateTimeRemaining,
            [nameof(hasPendingNextState)] = hasPendingNextState,
            [nameof(pendingNextState)] = pendingNextState.ToString(),
            [nameof(preActivationTimeRemaining)] = preActivationTimeRemaining,
            [nameof(hasPendingPreActivation)] = hasPendingPreActivation,
            [nameof(pendingStateToActivate)] = pendingStateToActivate.ToString()
        };
        return data;
    }

    public void Load(SaveData data)
    {
        currentStateName = (TState)Enum.Parse(typeof(TState), data[nameof(currentStateName)].ToString());
        stateTimeRemaining = (double)data[nameof(stateTimeRemaining)];
        hasPendingNextState = (bool)data[nameof(hasPendingNextState)];
        pendingNextState = (TState)Enum.Parse(typeof(TState), data[nameof(pendingNextState)].ToString());
        preActivationTimeRemaining = (double)data[nameof(preActivationTimeRemaining)];
        hasPendingPreActivation = (bool)data[nameof(hasPendingPreActivation)];
        pendingStateToActivate = (TState)Enum.Parse(typeof(TState), data[nameof(pendingStateToActivate)].ToString());
    }
}