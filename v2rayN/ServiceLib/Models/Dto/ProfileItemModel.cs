namespace ServiceLib.Models.Dto;

[Serializable]
public partial class ProfileItemModel : ReactiveObject
{
    public bool IsActive { get; set; }
    public string IndexId { get; set; }
    public EConfigType ConfigType { get; set; }
    private string _remarks;
    public string Remarks
    {
        get => _remarks;
        set
        {
            this.RaiseAndSetIfChanged(ref _remarks, value);
            this.RaisePropertyChanged(nameof(CountryCode));
        }
    }
    public string Address { get; set; }
    public int Port { get; set; }
    public string Network { get; set; }
    public string StreamSecurity { get; set; }
    public string Subid { get; set; }
    public string SubRemarks { get; set; }
    public int Sort { get; set; }

    [Reactive]
    public partial int Delay { get; set; }

    public decimal Speed { get; set; }

    [Reactive]
    public partial string DelayVal { get; set; }

    [Reactive]
    public partial string SpeedVal { get; set; }

    private string _ipInfo;
    public string IpInfo
    {
        get => _ipInfo;
        set
        {
            this.RaiseAndSetIfChanged(ref _ipInfo, value);
            this.RaisePropertyChanged(nameof(CountryCode));
        }
    }

    [Reactive]
    public partial string TodayUp { get; set; }

    private string? _serverCountryCode;
    public string? ServerCountryCode
    {
        get => _serverCountryCode;
        set
        {
            this.RaiseAndSetIfChanged(ref _serverCountryCode, value);
            this.RaisePropertyChanged(nameof(CountryCode));
        }
    }

    // A measured exit country wins; an IP lookup must not overwrite IpInfo.
    public string? CountryCode => ProfileCountry.Resolve(IpInfo, null)
        ?? ProfileCountry.Normalize(ServerCountryCode)
        ?? ProfileCountry.Resolve(null, Remarks);

    [Reactive]
    public partial string TodayDown { get; set; }

    [Reactive]
    public partial string TotalUp { get; set; }

    [Reactive]
    public partial string TotalDown { get; set; }

    public string GetSummary()
    {
        var summary = $"[{ConfigType}] {Remarks}";
        if (!ConfigType.IsComplexType())
        {
            summary += $"({Address}:{Port})";
        }

        return summary;
    }
}
