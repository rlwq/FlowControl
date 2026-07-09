# FlowControl

A factory-automation game on Godot 4.6 (C# / .NET 10): a chunked procedural world where the
player places machines and automates them — up to a custom in-game scripting language.

## Architecture

The solution is split into three projects with strict boundaries:

| Project | Role |
|---|---|
| `FlowControlModel` | Pure, engine-free simulation: world, machines, entities, inventories, commands |
| `FlowControlBusiness` | Machine and entity logics, world generation (depends on the model only) |
| `FlowControlGodotClient` | Godot scenes, views, input and resources (the only project referencing GodotSharp) |

Key invariants:

- The world is modified **only** through `WorldSimCommand`s queued into `WorldSim`;
  views react to `IChunkManager` events.
- Concrete `Machine`, `Entity`, `Chunk` are internal to the model; outside code sees
  `IMachine`, `IEntity`, `IChunkManager`.
- Chunks are permanent and always simulated; only client-side chunk *views* load and unload.

Browse the [API reference](api/FlowControlModel.yml) for the full picture.
