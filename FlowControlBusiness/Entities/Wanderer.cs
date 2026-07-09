using System;
using FlowControlModel;
using FlowControlModel.Entities;

namespace FlowControlBusiness.Entities;

/// <summary>
/// A simple autonomous behavior: the entity alternates between standing still
/// and walking in a random direction (e.g. grazing cows).
/// The randomness is seeded by the entity's id, so runs are deterministic.
/// </summary>
public class Wanderer(float speed = 0.05f) : EntityLogic
{
    private const int MinPhaseTicks = 20;
    private const int MaxPhaseTicks = 80;

    private Random? _rng;
    private Vec2 _step = Vec2.Zero;
    private int _phaseTicksLeft;

    public override Vec2 Tick(IEntity entity)
    {
        _rng ??= new Random(unchecked((int)entity.Id * 7919 + 12345));

        if (_phaseTicksLeft <= 0)
        {
            _phaseTicksLeft = _rng.Next(MinPhaseTicks, MaxPhaseTicks);
            if (_rng.NextDouble() < 0.5)
            {
                _step = Vec2.Zero; // stand and graze
            }
            else
            {
                var angle = _rng.NextDouble() * 2 * Math.PI;
                _step = new Vec2((float)Math.Cos(angle), (float)Math.Sin(angle)) * speed;
            }
        }

        _phaseTicksLeft--;
        return _step;
    }

    public override EntityLogic Copy() => new Wanderer(speed);
}
