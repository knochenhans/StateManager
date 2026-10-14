using System;

#nullable enable

public class State<TState>(Action<bool>? onEnter,
                           Action? onExit,
                           double duration = 0.0,
                           double preActivationBaseTime = 0.0,
                           double preActivationRandomTimeMin = 0.0,
                           double preActivationRandomTimeMax = 0.0,
                           TState? nextStateName = default,
                           bool durationIsRandom = false) where TState : notnull
{
    public Action<bool>? OnEnter = onEnter;
    public Action? OnExit = onExit;
    public double Duration = duration;
    public double PreActivationBaseTime = preActivationBaseTime;
    public double PreActivationRandomTimeMin = preActivationRandomTimeMin;
    public double PreActivationRandomTimeMax = preActivationRandomTimeMax;
    public TState? NextStateName = nextStateName;
    public bool DurationIsRandom = durationIsRandom;

    public void Enter(bool silent = false) => OnEnter?.Invoke(silent);
    public void Exit() => OnExit?.Invoke();
}