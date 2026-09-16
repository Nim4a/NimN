namespace ServiceLib.Common;

public static class NimNRelease
{
    // Never install inherited PattN releases or partially uploaded packages.
    public static string? SelectTag(List<GitHubRelease>? releases, bool preRelease = false) => releases?
        .Where(r => !r.Draft && (preRelease || !r.Prerelease) && r.TagName != null && System.Text.RegularExpressions.Regex.IsMatch(r.TagName, @"^v\d+\.\d+\.\d+-nimn\.\d+(?:\.[0-9a-f]{8})?$")
            && r.Assets?.Any(a => a.Name == "NimN-windows-64.zip") == true
            && r.Assets?.Any(a => a.Name == "NimN-windows-64-desktop.zip") == true)
        .Where(r => new SemanticVersion(r.TagName).ToStandardVersionString("v") == r.TagName)
        .OrderByDescending(r => new SemanticVersion(r.TagName))
        .Select(r => r.TagName).FirstOrDefault();
}
