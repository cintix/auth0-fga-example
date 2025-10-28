namespace Config;

public class ClientConfiguration
{
    public string ApiUrl { get; set; }
    public string StoreId { get; set; }
    public string AuthorizationModelId { get; set; }
    public Credentials Credentials { get; set; }
}

public class Credentials
{
    public CredentialsConfig Config { get; set; }
}

public class CredentialsConfig
{
    public string ApiTokenIssuer { get; set; }
    public string ApiAudience { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
}