namespace SixSeven.Domain.Config;

public class JwtOptions
{
    public string Secret { get; set; } = null!;  
    public int ExpiryMinutes { get; set; } = 60; 
    public string Issuer { get; set; } = "SixSeven";
    public string Audience { get; set; } = "SixSeven";
}