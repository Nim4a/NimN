namespace ServiceLib.Tests.Models;

public class NiNReleaseTests
{
    private static GitHubRelease Release(string tag, bool draft = false) => new()
    {
        TagName = tag, Draft = draft,
        Assets = [new() { Name = "NiN-windows-64.zip" }, new() { Name = "NiN-windows-64-desktop.zip" }]
    };

    [Test]
    public async Task RejectsUpstreamAndIncompleteReleases()
    {
        var releases = new List<GitHubRelease>
        {
            Release("7.25.1-P99"),
            new() { TagName = "v7.25.1-nin.99", Assets = [] },
            Release("v7.25.1-nin.100", true),
            Release("garbage-nin.999"),
            Release("v7.25.1-nin.3")
        };
        await NiNRelease.SelectTag(releases).Should().BeEqualTo("v7.25.1-nin.3");
    }

    [Test]
    public async Task AutomatedRevisionIsNewerAndDoesNotHideNewBaseVersion()
    {
        const string automated = "v7.25.1-nin.1789600000.b1c96713";
        await (new SemanticVersion(automated) > new SemanticVersion("v7.25.1-nin.3")).Should().BeTrue();
        await (new SemanticVersion("v7.26.0-nin.1") > new SemanticVersion(automated)).Should().BeTrue();
        await NiNRelease.SelectTag([Release("v7.25.1-nin.3"), Release(automated)])
            .Should().BeEqualTo(automated);
    }

    [Test]
    public async Task RespectsPrereleasePreferenceAndNumericRevisionOrder()
    {
        var preview = Release("v7.25.1-nin.11");
        preview.Prerelease = true;
        var releases = new List<GitHubRelease> { preview, Release("v7.25.1-nin.9"), Release("v7.25.1-nin.10") };
        await NiNRelease.SelectTag(releases).Should().BeEqualTo("v7.25.1-nin.10");
        await NiNRelease.SelectTag(releases, true).Should().BeEqualTo("v7.25.1-nin.11");
    }

    [Test]
    public async Task EmptyOrUnrelatedReleasesProduceNoUpdate()
    {
        await (NiNRelease.SelectTag(null) == null).Should().BeTrue();
        await (NiNRelease.SelectTag([Release("7.25.1-P26")]) == null).Should().BeTrue();
    }
}
