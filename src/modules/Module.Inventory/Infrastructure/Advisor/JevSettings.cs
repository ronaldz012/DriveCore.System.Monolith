namespace Module.Inventory.Infrastructure.Advisor;

public sealed class JevSettings
{
    public const string SectionName = "Jev";
    public const string ApiKeyEnvVar = "TypeSafeApiKey";

    public string? ApiKey { get; set; }
    public string? Endpoint { get; set; }
    public string? Model { get; set; }
}
