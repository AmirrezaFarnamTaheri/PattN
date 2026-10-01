using AmazTool;

namespace ServiceLib.Tests.Reviver;

public class ArchivePathGuardTests
{
    [Test]
    public async Task ResolveUnderRoot_NormalEntry_ShouldStayInsideRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "pattn-updater-root");
        var result = ArchivePathGuard.ResolveUnderRoot(root, "bin/pattn-discovery/helper");

        await result.StartsWith(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase).Should().BeTrue();
        await result.EndsWith(
            Path.Combine("bin", "pattn-discovery", "helper"),
            StringComparison.OrdinalIgnoreCase).Should().BeTrue();
    }

    [Test]
    public async Task ResolveUnderRoot_ParentTraversal_ShouldBeRejected()
    {
        var root = Path.Combine(Path.GetTempPath(), "pattn-updater-root");

        var action = () => ArchivePathGuard.ResolveUnderRoot(root, "../../outside.exe");

        await action.Should().ThrowAsync<InvalidDataException>();
    }

    [Test]
    public async Task ResolveUnderRoot_RootedPath_ShouldBeRejected()
    {
        var root = Path.Combine(Path.GetTempPath(), "pattn-updater-root");
        var rooted = Path.GetFullPath(Path.Combine(root, "..", "outside.exe"));

        var action = () => ArchivePathGuard.ResolveUnderRoot(root, rooted);

        await action.Should().ThrowAsync<InvalidDataException>();
    }
}
