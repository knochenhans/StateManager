using Godot;
using System;

public partial class State<TState> : GodotObject where TState : notnull
{
    public Action OnEnter { get; }
    public Action OnExit { get; }
    public float Duration { get; set; } = 0.0f;
    public TState NextStateName { get; set; }
    public bool DurationIsRandom { get; set; } = false;

    public State(Action onEnter = null, Action onExit = null, float duration = 0.0f, TState nextStateName = default, bool durationIsRandom = false)
    {
        OnEnter = onEnter;
        OnExit = onExit;
        Duration = duration;
        NextStateName = nextStateName;
        DurationIsRandom = durationIsRandom;
    }

    public void Enter() => OnEnter?.Invoke();
    public void Exit() => OnExit?.Invoke();
}
