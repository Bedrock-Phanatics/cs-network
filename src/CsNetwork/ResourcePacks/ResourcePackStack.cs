using System;
using System.Collections.Generic;

namespace CsNetwork.ResourcePacks;

public sealed record PackStackEntry(
    Guid PackId,
    PackVersion Version,
    string SubPackName = "");

public sealed record ResourcePackStack(
    IReadOnlyList<PackStackEntry> ResourcePacks,
    IReadOnlyList<PackStackEntry> BehaviorPacks,
    IReadOnlyList<PackStackEntry> RaytracingPacks,
    IReadOnlyList<string> Experiments,
    bool ExperimentsPreviouslyToggled = false,
    bool MustAccept = false,
    bool HasEditorPacks = false,
    string BaseGameVersion = "1.26.40")
{
    public static readonly ResourcePackStack Empty = new(
        ResourcePacks: [],
        BehaviorPacks: [],
        RaytracingPacks: [],
        Experiments: []);
}
