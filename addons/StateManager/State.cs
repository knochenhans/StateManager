using Godot;
using System;

#nullable enable

public partial class State<TState>(Action? onEnter, Action? onExit, double duration = 0.0, double preActivationBaseTime = 0.0, double preActivationRandomTimeMin = 0.0, double preActivationRandomTimeMax = 0.0, TState? nextStateName = default, bool durationIsRandom = false) : GodotObject where TState : notnull
{
    public Action? OnEnter = onEnter;
    public Action? OnExit = onExit;
    public double Duration = duration;
    public double PreActivationBaseTime = preActivationBaseTime;
    public double PreActivationRandomTimeMin = preActivationRandomTimeMin;
    public double PreActivationRandomTimeMax = preActivationRandomTimeMax;
    public TState? NextStateName = nextStateName;
    public bool DurationIsRandom = durationIsRandom;

    public void Enter() => OnEnter?.Invoke();
    public void Exit() => OnExit?.Invoke();
}
