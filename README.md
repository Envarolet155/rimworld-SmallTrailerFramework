# Small Trailer Framework

Small Trailer Framework is a lightweight RimWorld 1.6 framework for trailer-like units that can share one internal state across building, towing, and caravan-oriented modes.

The core assembly intentionally does not implement concrete mode switching. It exposes:

- `SmallTrailerState` for durable shared state and internal containers.
- `CompSmallTrailerUnit` for attaching state to buildings or packed tokens.
- `ISmallTrailerModeHandler` for building/towing/caravan state transitions.
- `ISmallTrailerInventoryBridge` for best-effort caravan inventory integration.
- `ISmallTrailerSpeedWorker` for pawn/unit based speed calculations.
- `SmallTrailerRegistry` for optional plugins and downstream mods.

The included `SmallTrailerFramework.ExamplePlugin` registers a default example handler. It provides a buildable small trailer using the included textures, plus simple loading, towing, unloading, and best-effort inventory restoration. This is sample gameplay built on the framework, not behavior hard-coded into the framework core.

## Build

Build with:

```powershell
dotnet build Source\SmallTrailerFramework\SmallTrailerFramework.csproj
```

The project targets `.NET Framework 4.7.2` and references RimWorld assemblies from the local Steam install layout.
