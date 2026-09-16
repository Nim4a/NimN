using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServiceLib.Tests.Services;

public class ServerCountryServiceTests
{
    // ---- direct address inputs (no DNS) -----------------------------------

    [Test]
    [Arguments("10.0.0.1")]
    [Arguments("192.168.1.1")]
    [Arguments("127.0.0.1")]
    [Arguments("169.254.1.1")]
    [Arguments("192.0.2.55")]
    [Arguments("224.0.0.5")]
    [Arguments("240.0.0.1")]
    [Arguments("100.64.0.1")]
    [Arguments("198.18.0.1")]
    [Arguments("0.0.0.0")]
    [Arguments("255.255.255.255")]
    [Arguments("::1")]
    [Arguments("fe80::1")]
    [Arguments("fc00::1")]
    [Arguments("2001:db8::1")]
    [Arguments("ff02::1")]
    [Arguments("::")]
    [Arguments("::ffff:192.168.1.1")]
    [Arguments("192.0.0.1")]
    [Arguments("2001:db8::1")]
    [Arguments("64:ff9b::a00:1")]
    public async Task ResolveAsync_ShouldNotQueryHttpForNonPublicDirectIps(string address)
    {
        var http = new FakeHttp();
        var service = CreateService(http: http);
        var result = await service.ResolveAsync(address);
        await result.Should().BeNull();
        await http.CallCount.Should().BeEqualTo(0);
    }

    [Test]
    public async Task ResolveAsync_NullOrEmptyOrWhitespace_ReturnsNullWithoutHttp()
    {
        var http = new FakeHttp();
        var service = CreateService(http: http);
        await (await service.ResolveAsync(null)).Should().BeNull();
        await (await service.ResolveAsync("")).Should().BeNull();
        await (await service.ResolveAsync("   ")).Should().BeNull();
        await http.CallCount.Should().BeEqualTo(0);
    }

    [Test]
    [Arguments("1.2.3")]
    [Arguments("999.1.1.1")]
    [Arguments("1.2.3.4.5")]
    [Arguments("not a valid host")]
    public async Task ResolveAsync_UnparseableInput_ReturnsNullWithoutHttp(string address)
    {
        var http = new FakeHttp();
        var dns = new FakeDns();
        var service = CreateService(http: http, dns: dns);
        await (await service.ResolveAsync(address)).Should().BeNull();
        await http.CallCount.Should().BeEqualTo(0);
        await dns.CallCount.Should().BeEqualTo(0);
    }

    // ---- public direct IPs -------------------------------------------------

    [Test]
    [Arguments("8.8.8.8", "US")]
    [Arguments("1.1.1.1", "AU")]
    public async Task ResolveAsync_PublicDirectIp_QueriesIpWhoIsAndParsesCountry(string address, string expected)
    {
        var body = System.Text.Json.JsonSerializer.Serialize(new { ip = address, success = true, country_code = expected });
        var http = new FakeHttp((address, body));
        var service = CreateService(http: http);
        var result = await service.ResolveAsync(address);
        await result.Should().BeEqualTo(expected);
        await http.CallCount.Should().BeEqualTo(1);
        await http.LastPath.Should().BeEqualTo("/" + address);
    }

    [Test]
    public async Task ResolveAsync_IpWhoIsFailureOrMissingCode_ReturnsNull()
    {
        var http = new FakeHttp(
            ("9.9.9.9", """{"ip":"9.9.9.9","success":false}"""),
            ("4.4.4.4", """{"ip":"4.4.4.4","success":true}"""),
            ("5.5.5.5", "not-json"));
        var service = CreateService(http: http);
        await (await service.ResolveAsync("9.9.9.9")).Should().BeNull();
        await (await service.ResolveAsync("4.4.4.4")).Should().BeNull();
        await (await service.ResolveAsync("5.5.5.5")).Should().BeNull();
    }

    [Test]
    public async Task ResolveAsync_CountryCodeNormalization_LowercaseAndUkMap()
    {
        var http = new FakeHttp(
            ("8.8.4.4", """{"success":true,"country_code":"us"}"""),
            ("1.0.0.2", """{"success":true,"country_code":"UK"}"""));
        var service = CreateService(http: http);
        await (await service.ResolveAsync("8.8.4.4")).Should().BeEqualTo("US");
        await (await service.ResolveAsync("1.0.0.2")).Should().BeEqualTo("GB");
    }

    // ---- hostnames ---------------------------------------------------------

    [Test]
    public async Task ResolveAsync_Hostname_ResolvesDnsPicksPublicAnswerAndQueries()
    {
        var dns = new FakeDns("example.com", ["10.0.0.1", "8.8.8.8", "192.168.1.1"]);
        var http = new FakeHttp(("8.8.8.8", """{"success":true,"country_code":"US"}"""));
        var service = CreateService(http: http, dns: dns);
        var result = await service.ResolveAsync("example.com");
        await result.Should().BeEqualTo("US");
        await http.LastPath.Should().BeEqualTo("/8.8.8.8");
    }

    [Test]
    public async Task ResolveAsync_HostnameOnlyPrivateAnswers_ReturnsNullWithoutHttp()
    {
        var dns = new FakeDns("internal.corp", ["10.0.0.1", "192.168.1.1"]);
        var http = new FakeHttp();
        var service = CreateService(http: http, dns: dns);
        await (await service.ResolveAsync("internal.corp")).Should().BeNull();
        await http.CallCount.Should().BeEqualTo(0);
    }

    [Test]
    public async Task ResolveAsync_HostnameDnsThrows_ReturnsNullWithoutHttp()
    {
        var http = new FakeHttp();
        var dns = new FakeDns();
        dns.ThrowOnCall = true;
        var service = CreateService(http: http, dns: dns);
        await (await service.ResolveAsync("broken.example.com")).Should().BeNull();
        await http.CallCount.Should().BeEqualTo(0);
    }

    [Test]
    public async Task ResolveAsync_IpLiteralInput_DoesNotCallDns()
    {
        var dns = new FakeDns();
        var http = new FakeHttp(("9.9.9.9", """{"success":true,"country_code":"CH"}"""));
        var service = CreateService(http: http, dns: dns);
        await (await service.ResolveAsync("9.9.9.9")).Should().BeEqualTo("CH");
        await dns.CallCount.Should().BeEqualTo(0);
    }

    // ---- caching & dedupe --------------------------------------------------

    [Test]
    public async Task ResolveAsync_SuccessResultIsCached_NoSecondHttpCall()
    {
        var http = new FakeHttp(("8.8.8.8", """{"success":true,"country_code":"US"}"""));
        var service = CreateService(http: http);
        await (await service.ResolveAsync("8.8.8.8")).Should().BeEqualTo("US");
        await (await service.ResolveAsync("8.8.8.8")).Should().BeEqualTo("US");
        await http.CallCount.Should().BeEqualTo(1);
    }

    [Test]
    public async Task ResolveAsync_FailureIsCachedWithinWindow_NoImmediateRetry()
    {
        var http = new FakeHttp();
        var service = CreateService(http: http);
        await (await service.ResolveAsync("3.3.3.3")).Should().BeNull();
        await (await service.ResolveAsync("3.3.3.3")).Should().BeNull();
        await http.CallCount.Should().BeEqualTo(1);
    }

    [Test]
    public async Task ResolveAsync_ConcurrentDuplicateRequests_DedupeToSingleHttpCall()
    {
        var http = new FakeHttp(("4.4.4.4", """{"success":true,"country_code":"US"}"""));
        var service = CreateService(http: http);
        var tasks = Enumerable.Range(0, 6).Select(_ => service.ResolveAsync("4.4.4.4")).ToArray();
        var results = await Task.WhenAll(tasks);
        foreach (var result in results)
        {
            await result.Should().BeEqualTo("US");
        }
        await http.CallCount.Should().BeEqualTo(1);
    }

    [Test]
    public async Task ResolveAsync_DistinctAddresses_AreResolvedSeparately()
    {
        var http = new FakeHttp(
            ("8.8.8.8", """{"success":true,"country_code":"US"}"""),
            ("1.1.1.1", """{"success":true,"country_code":"AU"}"""));
        var service = CreateService(http: http);
        await (await service.ResolveAsync("8.8.8.8")).Should().BeEqualTo("US");
        await (await service.ResolveAsync("1.1.1.1")).Should().BeEqualTo("AU");
        await http.CallCount.Should().BeEqualTo(2);
    }

    // ---- rate limiting -----------------------------------------------------

    [Test]
    public async Task ResolveAsync_SequentialRequests_AreSpacedAtLeast1100ms()
    {
        var http = new FakeHttp(
            ("8.8.8.8", """{"success":true,"country_code":"US"}"""),
            ("1.1.1.1", """{"success":true,"country_code":"AU"}"""),
            ("4.4.4.4", """{"success":true,"country_code":"US"}"""));
        var service = CreateService(http: http);
        var stopwatch = Stopwatch.StartNew();
        await service.ResolveAsync("8.8.8.8");
        await service.ResolveAsync("1.1.1.1");
        await service.ResolveAsync("4.4.4.4");
        stopwatch.Stop();
        await (stopwatch.ElapsedMilliseconds >= 2200).Should().BeTrue();
    }

    [Test]
    public async Task ConcurrentRequestsRemainSerializedAndSpaced()
    {
        var starts = new System.Collections.Concurrent.ConcurrentQueue<long>();
        var watch = Stopwatch.StartNew();
        var service = new ServerCountryService(async (_, ct) =>
        {
            starts.Enqueue(watch.ElapsedMilliseconds);
            await Task.Delay(25, ct);
            return """{"success":true,"country_code":"US"}""";
        });
        await Task.WhenAll(service.ResolveAsync("8.8.8.8"), service.ResolveAsync("1.1.1.1"), service.ResolveAsync("9.9.9.9"));
        var times = starts.ToArray();
        await (times[2] - times[1] >= 1090).Should().BeTrue();
        await (times[1] - times[0] >= 1090).Should().BeTrue();
    }

    // ---- cancellation ------------------------------------------------------

    [Test]
    public async Task ResolveAsync_PreCancelled_ReturnsNullWithoutHttp()
    {
        var http = new FakeHttp();
        var service = CreateService(http: http);
        await (await service.ResolveAsync("8.8.8.8", new CancellationToken(true))).Should().BeNull();
        await http.CallCount.Should().BeEqualTo(0);
    }

    [Test]
    public async Task ResolveAsync_HttpThrowsOrTimesOut_ReturnsNullWithoutCrash()
    {
        var http = new FakeHttp();
        http.ThrowOnCall = true;
        var service = CreateService(http: http);
        await (await service.ResolveAsync("8.8.8.8")).Should().BeNull();
        http.ThrowOnCall = false;
    }

    [Test]
    public async Task LiveLookupWhenExplicitlyEnabled()
    {
        if (Environment.GetEnvironmentVariable("NIMN_LIVE_GEO_TEST") != "1") return;
        var code = await ServerCountryService.Instance.ResolveAsync("8.8.8.8");
        await code.Should().BeEqualTo("US");
        using var flag = ProfileCountry.OpenFlag(code);
        await (flag != null).Should().BeTrue();
        Console.WriteLine($"LIVE public IP 8.8.8.8 -> {code}; embedded flag loaded");
    }

    // ---- helpers -----------------------------------------------------------

    private static ServerCountryService CreateService(FakeHttp? http = null, FakeDns? dns = null)
    {
        http ??= new FakeHttp();
        dns ??= new FakeDns();
        return new ServerCountryService(http.SendAsync, dns.GetHostAddressesAsync);
    }

    private sealed class FakeHttp : IDisposable
    {
        private readonly Queue<string> _bodies;
        private readonly List<string> _paths = [];

        public FakeHttp(params (string Path, string Body)[] responses)
        {
            _bodies = new Queue<string>(responses.Select(r => r.Body));
            for (var i = 0; i < responses.Length; i++)
            {
                _pathByCall.Add(responses[i].Path);
            }
        }

        private readonly List<string> _pathByCall = [];

        public int CallCount { get; private set; }
        public string? LastPath => _paths.Count > 0 ? _paths[^1] : null;
        public bool ThrowOnCall { get; set; }

        public async Task<string> SendAsync(string url, CancellationToken cancellationToken)
        {
            CallCount++;
            _paths.Add(new Uri(url).AbsolutePath);
            await Task.Delay(5, cancellationToken);
            if (ThrowOnCall)
            {
                throw new HttpRequestException("simulated failure");
            }
            return _bodies.TryDequeue(out var body) ? body : throw new HttpRequestException("no response configured");
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakeDns : IDisposable
    {
        private readonly string? _host;
        private readonly string[] _answers;

        public FakeDns()
        {
        }

        public FakeDns(string host, string[] answers)
        {
            _host = host;
            _answers = answers;
        }

        public int CallCount { get; private set; }
        public bool ThrowOnCall { get; set; }

        public Task<IPAddress[]> GetHostAddressesAsync(string host, CancellationToken cancellationToken)
        {
            CallCount++;
            if (ThrowOnCall || _host != host || _answers == null)
            {
                throw new SocketException((int)SocketError.HostNotFound);
            }
            return Task.FromResult(_answers.Select(IPAddress.Parse).ToArray());
        }

        public void Dispose()
        {
        }
    }
}
