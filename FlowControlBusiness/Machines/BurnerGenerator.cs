using FlowControlModel.Machines;

namespace FlowControlBusiness.Machines;

/// <summary>
/// Burns fuel from its own inventory and offers electric power into the network
/// while burning. Accepts only its fuel kind (insert filter).
/// The fuel burns at a constant rate regardless of demand (no throttling yet).
/// </summary>
public class BurnerGenerator(float power = 10f, int burnTicksPerFuel = 200, string fuelKind = "coal")
    : MachineLogic
{
    private int _burnTicksLeft;
    private bool _filterSet;

    /// <inheritdoc/>
    public override string? DisplayState =>
        _burnTicksLeft > 0 ? $"Burning ({_burnTicksLeft})" : "No fuel";

    /// <summary> Executes one quant of the logic: burns fuel and offers power. </summary>
    public override void Tick(IBuildingApi building)
    {
        if (!_filterSet)
        {
            building.Machine.Inventory.InsertFilter = lite => lite.Kind == fuelKind;
            _filterSet = true;
        }

        if (_burnTicksLeft <= 0 && building.ConsumeItems(fuelKind, 1) == 1)
            _burnTicksLeft = burnTicksPerFuel;

        if (_burnTicksLeft <= 0)
            return;
        _burnTicksLeft--;
        building.OfferPower(power);
    }

    /// <inheritdoc/>
    public override MachineLogic Copy() => new BurnerGenerator(power, burnTicksPerFuel, fuelKind);
}
