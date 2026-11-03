namespace Bitlink.Api.Configuration;

public class AuthServerOptions
{
    public const string SectionName = "AuthServer";

    public string AuthServerUrl { get; set; } = "http://localhost:4000";
    public string ResourceServerUrl { get; set; } = "http://localhost:5000";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = "http://localhost:5174/callback";
}

public class S3Options
{
    public const string SectionName = "S3";

    public string Endpoint { get; set; } = "http://localhost:9000";
    public string Region { get; set; } = "us-east-1";
    public string BucketName { get; set; } = "bitlink-bucket";
    public string AccessKey { get; set; } = "root";
    public string SecretKey { get; set; } = "rootpass";
}

public class CorsOptions
{
    public const string SectionName = "Cors";

    public string OriginUrl { get; set; } = "http://localhost:5174";
}
