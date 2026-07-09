using System;
using FlowControlModel;
using FlowControlModel.Factories;
using Xunit;

namespace FlowControlModel.Tests;

public class RegistryBuilderTests
{
    [Fact]
    public void DuplicateRegistration_Throws()
    {
        var builder = new Registry.RegistryBuilder()
            .RegisterMachine("chest", new Vec2I(2, 1))
            .RegisterItem("iron_bar", 16)
            .RegisterGround("grass");

        Assert.Throws<ArgumentException>(() => builder.RegisterMachine("chest", new Vec2I(1, 1)));
        Assert.Throws<ArgumentException>(() => builder.RegisterItem("iron_bar", 8));
        Assert.Throws<ArgumentException>(() => builder.RegisterGround("grass"));
        Assert.Throws<ArgumentException>(() => builder.RegisterEntity("player", new Vec2(1, 1)));
    }

    [Fact]
    public void UseAfterBuild_Throws()
    {
        var builder = new Registry.RegistryBuilder();
        builder.Build();

        Assert.Throws<InvalidOperationException>(() => builder.RegisterGround("grass"));
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void LogicForUnknownKind_Throws()
    {
        var builder = new Registry.RegistryBuilder();

        Assert.Throws<ArgumentException>(
            () => builder.RegisterMachineLogic("ghost", new FlowControlBusiness.Machines.Dumb()));
        Assert.Throws<ArgumentException>(
            () => builder.RegisterEntityLogic("ghost", new FlowControlBusiness.Entities.Wanderer()));
    }

    [Fact]
    public void SpawnerWithUnknownItem_ThrowsOnBuild()
    {
        var builder = new Registry.RegistryBuilder()
            .RegisterGround("iron_deposit", spawnsItemKind: "iron_ore", spawnPeriodTicks: 50);

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }
}
