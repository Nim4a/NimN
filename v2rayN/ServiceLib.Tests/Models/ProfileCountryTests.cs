using ServiceLib.Common;

namespace ServiceLib.Tests.Models;

public class ProfileCountryTests
{
    [Test]
    [Arguments("(DE) 203.0.113.1", "🇺🇸 New York", "DE")]
    [Arguments("(Germany) 203.0.113.1", "US-01", "DE")]
    [Arguments("", "🇫🇷 Paris", "FR")]
    [Arguments("", "آلمان 01", "DE")]
    [Arguments("", "DE-01", "DE")]
    [Arguments("", "UK London", "GB")]
    [Arguments("", "in service", null)]
    [Arguments("(unknown)", "Unnamed", null)]
    [Arguments("", "US1", "US")]
    [Arguments(null, null, null)]
    public async Task ResolvesHints(string? ip, string? remark, string? expected)
    {
        await ProfileCountry.Resolve(ip, remark).Should().BeEqualTo(expected);
    }

    [Test]
    [Arguments("(JP) 192.0.2.1", " fr ", "DE-01", "JP", "FR")]
    [Arguments("", "uk", "🇺🇸 New York", null, "GB")]
    [Arguments("(DE) 203.0.113.1", null, "France", "DE", null)]
    [Arguments("(unknown) 192.0.2.1", "invalid", "🇯🇵 Japan", null, null)]
    [Arguments(null, null, "DE-01", null, null)]
    [Arguments("", "", "France", null, null)]
    public async Task LocationFlagsUseIndependentSources(string? ip, string? endpoint, string remarks, string? expectedExit, string? expectedEndpoint)
    {
        var profile = new ProfileItemModel { IpInfo = ip!, ServerCountryCode = endpoint, Remarks = remarks };
        var exitProperty = typeof(ProfileItemModel).GetProperty("ExitCountryCode");
        var endpointProperty = typeof(ProfileItemModel).GetProperty("EndpointCountryCode");
        await (exitProperty != null).Should().BeEqualTo(true);
        await (endpointProperty != null).Should().BeEqualTo(true);
        await ((string?)exitProperty!.GetValue(profile)).Should().BeEqualTo(expectedExit);
        await ((string?)endpointProperty!.GetValue(profile)).Should().BeEqualTo(expectedEndpoint);
    }

    [Test]
    public async Task ServerCountryOverridesLabelButNotMeasuredExit()
    {
        var profile = new ProfileItemModel { Remarks = "DE-01" };
        var property = typeof(ProfileItemModel).GetProperty("ServerCountryCode");
        await (property != null).Should().BeEqualTo(true);
        var notifications = 0;
        profile.PropertyChanged += (_, e) => { if (e.PropertyName == "CountryCode") notifications++; };
        property!.SetValue(profile, "FR");
        await profile.CountryCode.Should().BeEqualTo("FR");
        await notifications.Should().BeEqualTo(1);
        profile.IpInfo = "(JP) 192.0.2.1";
        await profile.CountryCode.Should().BeEqualTo("JP");
        profile.IpInfo = "";
        property.SetValue(profile, null);
        await profile.CountryCode.Should().BeEqualTo("DE");
    }

    [Test]
    public async Task IndependentLocationChangesNotifyBindings()
    {
        var profile = new ProfileItemModel { Remarks = "DE-01" };
        var changes = new List<string?>();
        profile.PropertyChanged += (_, e) => changes.Add(e.PropertyName);
        profile.IpInfo = "(JP) 192.0.2.1";
        await changes.Contains("ExitCountryCode").Should().BeEqualTo(true);
        await profile.ExitCountryCode.Should().BeEqualTo("JP");
        await profile.EndpointCountryCode.Should().BeEqualTo(null);
        changes.Clear();
        profile.ServerCountryCode = "FR";
        await changes.Contains("EndpointCountryCode").Should().BeEqualTo(true);
        await profile.ExitCountryCode.Should().BeEqualTo("JP");
        await profile.EndpointCountryCode.Should().BeEqualTo("FR");
        changes.Clear();
        profile.IpInfo = "";
        profile.ServerCountryCode = null;
        await changes.Contains("ExitCountryCode").Should().BeEqualTo(true);
        await changes.Contains("EndpointCountryCode").Should().BeEqualTo(true);
        await profile.ExitCountryCode.Should().BeEqualTo(null);
        await profile.EndpointCountryCode.Should().BeEqualTo(null);
        profile.Remarks = "France";
        await profile.ExitCountryCode.Should().BeEqualTo(null);
        await profile.EndpointCountryCode.Should().BeEqualTo(null);
    }

    [Test]
    public async Task CountryChangesNotifyBindings()
    {
        var profile = new ProfileItemModel { Remarks = "🇺🇸 New York" };
        var notifications = 0;
        profile.PropertyChanged += (_, e) => { if (e.PropertyName == "CountryCode") notifications++; };
        profile.IpInfo = "(DE) 203.0.113.1";
        await profile.CountryCode.Should().BeEqualTo("DE");
        await notifications.Should().BeEqualTo(1);
        profile.IpInfo = "";
        profile.Remarks = "🇫🇷 Paris";
        await profile.CountryCode.Should().BeEqualTo("FR");
        await notifications.Should().BeEqualTo(3);
    }

    [Test]
    public async Task BundledFlagsArePngAndUnknownIsBlank()
    {
        foreach (var code in new[] { "DE", "FR", "IR", "US", "GB" })
        {
            using var stream = ProfileCountry.OpenFlag(code);
            if (stream == null) throw new Exception($"Missing flag: {code}");
            var signature = new byte[8];
            stream.ReadExactly(signature);
            await Convert.ToHexString(signature).Should().BeEqualTo("89504E470D0A1A0A");
        }
        await (ProfileCountry.OpenFlag("../../bad") == null).Should().BeEqualTo(true);
    }
}
