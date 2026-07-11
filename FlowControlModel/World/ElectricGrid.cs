using System;
using System.Collections.Generic;
using System.Linq;
using FlowControlModel.Machines;

namespace FlowControlModel.World;

/// <summary>
/// The electric network solver (Factorio-style). Poles whose centers are within wire
/// reach of each other form networks (connected components); a machine belongs to the
/// network of the first pole whose supply radius covers its footprint. Every tick,
/// generators offer power into their network and each consumer receives the share
/// <c>min(1, supply / demand)</c> of its demand — written to
/// <see cref="Machine.PowerSatisfaction"/> for the logics to read on their next tick.
/// </summary>
/// <remarks>
/// Deterministic: poles and machines are processed in id order, and the network layout
/// is rebuilt lazily (only after a machine was placed or removed). Consumers draw their
/// full demand whenever covered — idle machines do not release power yet.
/// </remarks>
internal sealed class ElectricGrid(ChunkManager chunkManager)
{
    private bool _dirty = true;

    /// <summary> Network index of every covered machine (poles included). </summary>
    private readonly Dictionary<uint, int> _networkOf = [];

    /// <summary> Power offered into each network during the current tick. </summary>
    private readonly List<float> _supply = [];

    /// <summary> Total demand of the covered consumers of each network. </summary>
    private readonly List<float> _demand = [];

    /// <summary> Must be called whenever a machine is placed or removed. </summary>
    public void MarkDirty() => _dirty = true;

    /// <summary>
    /// Offers power into the generator's network for the current tick.
    /// Power offered by a generator standing outside any network is wasted.
    /// </summary>
    public void OfferPower(Machine generator, float amount)
    {
        if (amount <= 0)
            return;
        RebuildIfDirty();
        if (_networkOf.TryGetValue(generator.Id, out var network))
            _supply[network] += amount;
    }

    /// <summary>
    /// Distributes the power offered during this tick: every covered consumer receives
    /// <c>min(1, supply / demand)</c>, uncovered consumers receive 0.
    /// Called once per tick, after all machines have ticked.
    /// </summary>
    public void Resolve()
    {
        RebuildIfDirty();

        foreach (var machine in chunkManager.AllMachines)
        {
            if (machine.Lite.PowerDemand <= 0)
                continue;
            machine.PowerSatisfaction =
                _networkOf.TryGetValue(machine.Id, out var network) && _demand[network] > 0
                    ? Math.Min(1f, _supply[network] / _demand[network])
                    : 0f;
        }

        for (var i = 0; i < _supply.Count; i++)
            _supply[i] = 0f;
    }

    /// <summary> Recomputes pole networks and machine coverage after world changes. </summary>
    private void RebuildIfDirty()
    {
        if (!_dirty)
            return;
        _dirty = false;

        _networkOf.Clear();
        _supply.Clear();
        _demand.Clear();

        var poles = chunkManager.AllMachines
            .Where(machine => machine.Lite.Pole != null)
            .OrderBy(machine => machine.Id)
            .ToList();

        // Union-find over poles: connect when centers are within the larger wire reach
        var parent = Enumerable.Range(0, poles.Count).ToArray();
        int Root(int i) => parent[i] == i ? i : parent[i] = Root(parent[i]);
        for (var i = 0; i < poles.Count; i++)
        for (var j = i + 1; j < poles.Count; j++)
        {
            var reach = Math.Max(poles[i].Lite.Pole!.WireReach, poles[j].Lite.Pole!.WireReach);
            if (poles[i].Rect.GetCenter().DistanceTo(poles[j].Rect.GetCenter()) <= reach
                && Root(i) != Root(j))
                parent[Root(j)] = Root(i);
        }

        // Assign a compact network index per component, in pole id order
        var networkOfRoot = new Dictionary<int, int>();
        var networkOfPole = new int[poles.Count];
        for (var i = 0; i < poles.Count; i++)
        {
            if (!networkOfRoot.TryGetValue(Root(i), out var network))
            {
                network = networkOfRoot.Count;
                networkOfRoot.Add(Root(i), network);
                _supply.Add(0f);
                _demand.Add(0f);
            }
            networkOfPole[i] = network;
            _networkOf[poles[i].Id] = network;
        }

        // Cover machines: the first pole (in id order) whose supply radius reaches
        // the machine's footprint decides its network
        foreach (var machine in chunkManager.AllMachines.OrderBy(machine => machine.Id))
        {
            if (machine.Lite.Pole != null)
                continue;
            for (var i = 0; i < poles.Count; i++)
            {
                if (DistanceToRect(poles[i].Rect.GetCenter(), machine.Rect)
                    > poles[i].Lite.Pole!.SupplyRadius)
                    continue;
                _networkOf[machine.Id] = networkOfPole[i];
                if (machine.Lite.PowerDemand > 0)
                    _demand[networkOfPole[i]] += machine.Lite.PowerDemand;
                break;
            }
        }
    }

    /// <summary> Distance from a point to the closest point of a rectangle (0 inside). </summary>
    private static float DistanceToRect(Vec2 point, RectI rect)
    {
        var closest = new Vec2(
            Math.Clamp(point.X, rect.Position.X, rect.End.X),
            Math.Clamp(point.Y, rect.Position.Y, rect.End.Y));
        return closest.DistanceTo(point);
    }
}
