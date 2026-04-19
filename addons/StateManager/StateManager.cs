using Godot;
using Godot.Collections;

using System;

public partial class StateManager<[MustBeVariant] TState> : Node where TState : notnull
{
    [Signal] public delegate void StateChangedEventHandler(bool silent = false);

    private Dictionary<TState, State<TState>> states = [];
    private Timer stateTimer;
    private Timer preActivationTimer;
    private TState pendingNextState = default;
    private bool hasPendingNextState = false;
    private TState pendingStateToActivate = default;
    private bool hasPendingPreActivation = false;
    private TState currentStateName = default;

    public StateManager()
    {
        stateTimer = new Timer
        {
            OneShot = true
        };
        stateTimer.Timeout += OnStateTimerTimeout;
        AddChild(stateTimer);

        // timer used to wait before activating a state (PreActivationTime)
        preActivationTimer = new Timer
        {
            OneShot = true
        };
        preActivationTimer.Timeout += OnPreActivationTimeout;
        AddChild(preActivationTimer);
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

        // stop any running timers / clear pending transitions before starting a new enter
        stateTimer.Stop();
        hasPendingNextState = false;
        preActivationTimer.Stop();
        hasPendingPreActivation = false;

        // if the state defines a pre-activation wait, start the pre-activation timer
        if (state.PreActivationBaseTime + state.PreActivationRandomTimeMin + state.PreActivationRandomTimeMax > 0.0f)
        {
            pendingStateToActivate = name;
            hasPendingPreActivation = true;
            preActivationTimer.WaitTime = state.PreActivationBaseTime + GD.RandRange(state.PreActivationRandomTimeMin, state.PreActivationRandomTimeMax);
            preActivationTimer.Start();
            return;
        }

        // otherwise immediately activate
        ActivateState(name, silent);
    }

    // activate a state (enter, start duration timer, handle immediate next)
    private void ActivateState(TState name, bool silent = false)
    {
        var state = GetState(name);
        if (state == null)
            return;

        state.Enter();
        currentStateName = name;
        EmitSignal(SignalName.StateChanged, silent);

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
            EnterState(pendingNextState);
    }

    public TState GetCurrentStateName() => currentStateName;
}
