using System;
using System.Collections.Generic;

namespace Farhat.Core
{
    /// <summary>Small enum-keyed state machine with enter, exit and tick callbacks.</summary>
    public sealed class StateMachine<TState> where TState : struct
    {
        readonly Dictionary<TState, Action> onEnter = new Dictionary<TState, Action>();
        readonly Dictionary<TState, Action> onExit = new Dictionary<TState, Action>();
        readonly Dictionary<TState, Action<float>> onTick = new Dictionary<TState, Action<float>>();
        bool started;

        public TState Current { get; private set; }

        public StateMachine<TState> Configure(TState state, Action enter, Action<float> tick, Action exit)
        {
            if (enter != null) onEnter[state] = enter;
            if (tick != null) onTick[state] = tick;
            if (exit != null) onExit[state] = exit;
            return this;
        }

        public void Set(TState next)
        {
            if (started && EqualityComparer<TState>.Default.Equals(Current, next))
            {
                return;
            }
            Action exit;
            if (started && onExit.TryGetValue(Current, out exit)) exit();
            Current = next;
            started = true;
            Action enter;
            if (onEnter.TryGetValue(next, out enter)) enter();
        }

        public void Tick(float deltaTime)
        {
            Action<float> tick;
            if (started && onTick.TryGetValue(Current, out tick)) tick(deltaTime);
        }
    }
}
