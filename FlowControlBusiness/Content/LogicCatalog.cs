using System;
using System.Collections.Generic;
using System.Text.Json;
using FlowControlBusiness.Entities;
using FlowControlBusiness.Machines;
using FlowControlModel.Entities;
using FlowControlModel.Machines;

namespace FlowControlBusiness.Content;

/// <summary>
/// Maps logic names used in content manifests to factories creating logic prototypes.
/// A factory receives the manifest entry's optional <c>logicParams</c> JSON object.
/// </summary>
public sealed class LogicCatalog
{
    private readonly Dictionary<string, Func<JsonElement?, MachineLogic>> _machineLogics = [];
    private readonly Dictionary<string, Func<JsonElement?, EntityLogic>> _entityLogics = [];

    /// <summary> Registers a machine logic factory under a manifest name. </summary>
    public LogicCatalog AddMachineLogic(string name, Func<JsonElement?, MachineLogic> factory)
    {
        _machineLogics.Add(name, factory);
        return this;
    }

    /// <summary> Registers an entity logic factory under a manifest name. </summary>
    public LogicCatalog AddEntityLogic(string name, Func<JsonElement?, EntityLogic> factory)
    {
        _entityLogics.Add(name, factory);
        return this;
    }

    /// <summary> Creates a machine logic prototype by its manifest name. </summary>
    public MachineLogic CreateMachineLogic(string name, JsonElement? logicParams) =>
        _machineLogics.TryGetValue(name, out var factory)
            ? factory(logicParams)
            : throw new ContentException($"Unknown machine logic '{name}'.");

    /// <summary> Creates an entity logic prototype by its manifest name. </summary>
    public EntityLogic CreateEntityLogic(string name, JsonElement? logicParams) =>
        _entityLogics.TryGetValue(name, out var factory)
            ? factory(logicParams)
            : throw new ContentException($"Unknown entity logic '{name}'.");

    /// <summary> Reads a float parameter from a <c>logicParams</c> object, with a default. </summary>
    public static float GetFloat(JsonElement? logicParams, string name, float defaultValue) =>
        logicParams is { } element && element.TryGetProperty(name, out var value)
            ? value.GetSingle()
            : defaultValue;

    /// <summary> Reads a string parameter from a <c>logicParams</c> object, with a default. </summary>
    public static string GetString(JsonElement? logicParams, string name, string defaultValue) =>
        logicParams is { } element && element.TryGetProperty(name, out var value)
            ? value.GetString() ?? defaultValue
            : defaultValue;

    /// <summary> The catalog of all built-in logics. </summary>
    public static LogicCatalog Standard() => new LogicCatalog()
        .AddMachineLogic("dumb", _ => new Dumb())
        .AddMachineLogic("oven", _ => new Oven())
        .AddMachineLogic("manipulator", _ => new Manipulator())
        .AddMachineLogic("burner_generator", p => new BurnerGenerator(
            GetFloat(p, "power", 10f),
            (int)GetFloat(p, "burnTicks", 200),
            GetString(p, "fuel", "coal")))
        .AddEntityLogic("wanderer", p => new Wanderer(GetFloat(p, "speed", 0.05f)));
}
