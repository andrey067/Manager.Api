namespace Api.Options;

public record JwtOptions
{
    public string? Key { get; init; }
    public string? Login { get; init; }
    public string? Password { get; init; }
    public int HoursToExpire { get; init; } = 1;
}