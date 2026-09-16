namespace ServiceLib.Tests.Models;

public class NimNReleaseTests
{
    private static GitHubRelease Release(string tag, bool draft = false) => new()
    {
        TagName = tag, Draft = draft,
        Assets = [new() { Name = "NimN-windows-64.zip" }, new() { Name = "NimN-windows-64-desktop.zip" }]
    };

    [Test]
    public async Task RejectsUpstreamAndIncompleteReleases()
    {
        var releases = new List<GitHubRelease>
        {
            Release("7.25.1-P99"),
            new() { TagName = "v7.25.1-nimn.99", Assets = [] },
            Release("v7.25.1-nimn.100", true),
            Release("garbage-nimn.999"),
            Release("v7.25.1-nimn.3")
        };
        await NimNRelease.SelectTag(releases).Should().BeEqualTo("v7.25.1-nimn.3");
    }

    [Test]
    public async Task AutomatedRevisionIsNewerAndDoesNotHideNewBaseVersion()
    {
        const string automated = "v7.25.1-nimn.1789600000.b1c96713";
        await (new SemanticVersion(automated) > new SemanticVersion("v7.25.1-nimn.3")).Should().BeTrue();
        await (new SemanticVersion("v7.26.0-nimn.1") > new SemanticVersion(automated)).Should().BeTrue();
        await NimNRelease.SelectTag([Release("v7.25.1-nimn.3"), Release(automated)])
            .Should().BeEqualTo(automated);
    }

    [Test]
    public async Task RespectsPrereleasePreferenceAndNumericRevisionOrder()
    {
        var preview = Release("v7.25.1-nimn.11");
        preview.Prerelease = true;
        var releases = new List<GitHubRelease> { preview, Release("v7.25.1-nimn.9"), Release("v7.25.1-nimn.10") };
        await NimNRelease.SelectTag(releases).Should().BeEqualTo("v7.25.1-nimn.10");
        await NimNRelease.SelectTag(releases, true).Should().BeEqualTo("v7.25.1-nimn.11");
    }

    [Test]
    public async Task EmptyOrUnrelatedReleasesProduceNoUpdate()
    {
        await (NimNRelease.SelectTag(null) == null).Should().BeTrue();
        await (NimNRelease.SelectTag([Release("7.25.1-P26")]) == null).Should().BeTrue();
    }
}
