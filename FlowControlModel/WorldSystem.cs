using System;
using FlowControlModel.World;

namespace FlowControlModel;

/// <summary>
/// A simulation-wide process plugged into <see cref="WorldSim"/> (e.g. an order generator).
/// Systems are ticked once per simulation tick, after machines, entities and ground
/// spawners, in registration order.
/// </summary>
public interface IWorldSystem
{
    /// <summary> Executes one quant of the system. </summary>
    void Tick(IWorldContext world);
}

/// <summary>
/// The capability surface a <see cref="IWorldSystem"/> acts through: read the world,
/// consume deterministic randomness and enqueue commands (executed next tick).
/// </summary>
public interface IWorldContext
{
    /// <summary> The seed this world was created with (drives generation and gameplay randomness). </summary>
    int Seed { get; }

    /// <summary> Number of ticks simulated so far. </summary>
    ulong TickCount { get; }

    /// <summary>
    /// The simulation's deterministic random source: seeded by <see cref="Seed"/>,
    /// consumed in tick order, reproducible across runs. Never use any other randomness.
    /// </summary>
    Random Random { get; }

    /// <summary> The logical state of the world (read-only queries and events). </summary>
    IChunkManager ChunkManager { get; }

    /// <summary> Enqueues a command; it executes at the start of the next tick. </summary>
    void Enqueue(WorldSimCommand command);
}
