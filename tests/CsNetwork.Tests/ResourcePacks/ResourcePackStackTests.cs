using System;
using CsNetwork.ResourcePacks;
using Xunit;

namespace CsNetwork.Tests.ResourcePacks;

public sealed class ResourcePackStackTests
{
    [Fact]
    public void ResourcePackStack_Properties_Match()
    {
        var entry1 = new PackStackEntry(Guid.NewGuid(), new PackVersion(1, 0, 0), "subpack1");
        var entry2 = new PackStackEntry(Guid.NewGuid(), new PackVersion(2, 0, 0));

        var stack = new ResourcePackStack(
            ResourcePacks: [entry1, entry2],
            BehaviorPacks: [],
            RaytracingPacks: [entry1],
            Experiments: ["gametest", "upcoming_creator_features"],
            ExperimentsPreviouslyToggled: true,
            MustAccept: true,
            HasEditorPacks: false,
            BaseGameVersion: "1.21.60");

        Assert.Equal(2, stack.ResourcePacks.Count);
        Assert.Empty(stack.BehaviorPacks);
        Assert.Single(stack.RaytracingPacks);
        Assert.Equal(2, stack.Experiments.Count);
        Assert.True(stack.ExperimentsPreviouslyToggled);
        Assert.True(stack.MustAccept);
        Assert.False(stack.HasEditorPacks);
        Assert.Equal("1.21.60", stack.BaseGameVersion);
    }
}
