namespace Business.Configuration;

public sealed class ParameterConfigurationOptions
{
    public TimeSpan ReloadInterval { get; set; } = TimeSpan.FromSeconds(60);
    public bool ReloadOnChange { get; set; } = true;
}
